using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // M키 전체 맵 (기획서 9.2). 가 본 곳만 보이고, 내 위치, 탈출 포드(비상구 표시), 구역 이름을 그린다.
    // 2026-11-19 사용자 요청: 화면 안에 다 들어오게 줄이고, 지금 있는 구역 이름을 보여 주고, 포드는 안 가 봤어도 보인다.
    public class MapUI : MonoBehaviour
    {
        // 화면 기준 높이 1080에서 위의 제목 줄과 아래 범례 줄이 차지하는 높이. 나머지에 맵이 들어간다.
        private const float HeaderHeight = 78f;
        private const float FooterHeight = 52f;
        private const float ScreenMargin = 40f;

        // 칸 하나를 점 하나로 그리고 Point 필터로 늘린다. 칸이 화면에서 10픽셀 안팎이라 선이나 무늬를 넣으면 오히려 뭉개진다.
        private static readonly Color32 UnknownColor = new Color32(9, 8, 16, 255);
        private static readonly Color32 CorridorColor = new Color32(62, 60, 80, 255);
        private static readonly Color32 WallEdgeColor = new Color32(120, 222, 206, 255);
        private static readonly Color32 WallInnerColor = new Color32(30, 70, 76, 255);
        private static readonly Color ExitColor = new Color(0.15f, 0.7f, 0.32f, 1f);
        private static readonly Color PlayerColor = new Color(0.3f, 0.78f, 1f, 1f);

        // 구역마다 바닥 색을 달리해서 경계가 한눈에 보이게 한다. 벽(청록)과 겹치지 않는 차분한 색이다.
        private static readonly Color32[] ZoneColors =
        {
            new Color32(92, 74, 120, 255),
            new Color32(116, 84, 62, 255),
            new Color32(62, 92, 120, 255),
            new Color32(110, 70, 90, 255),
            new Color32(76, 104, 72, 255),
            new Color32(120, 104, 60, 255),
            new Color32(70, 80, 126, 255),
            new Color32(118, 66, 66, 255),
        };

        [SerializeField] private MapExplorer _map;
        [SerializeField] private RaidManager _raid;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private EscapePod[] _pods;
        [SerializeField] private Sprite _circleSprite;

        // 크기는 1920×1080 화면 기준의 픽셀이다. Play를 다시 시작하면 반영된다.
        [SerializeField, Tooltip("맵 한 칸의 한 변의 최대 길이. 맵이 화면보다 크면 화면에 맞춰 이보다 작게 그린다")] private float _cellPixels = 22f;

        private readonly List<ZoneArea> _zones = new List<ZoneArea>();
        private readonly List<Text> _zoneLabels = new List<Text>();
        private RectTransform _root;
        private RectTransform _mapRect;
        private RectTransform _playerMark;
        private RectTransform _playerRing;
        private RectTransform _zoneFrame;
        private Image _zoneFrameBorder;
        private Text _whereText;
        private RectTransform[] _podIcons;
        private Texture2D _texture;
        private Color32[] _pixels;
        private int[] _cellZone;
        private bool[] _zoneKnown;
        private ZoneArea _currentZone;
        private int _drawnVersion = -1;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        // 설정 메뉴가 열릴 때, Esc로 닫을 때
        public void Close()
        {
            if (_isOpen)
            {
                _isOpen = false;
                Sfx.Play(SfxId.MapClose);
            }
        }

        // 맵의 크기는 MapExplorer의 Awake에서 정해진다. 그 뒤에 읽으려고 Start에서 만든다.
        private void Start()
        {
            Build();
        }

        private void Update()
        {
            if (_controller.IsDead || _raid.Outcome != RaidOutcome.InProgress)
            {
                _isOpen = false;
            }
            else if (_input.ToggleMapPressedThisFrame)
            {
                _isOpen = !_isOpen;
                Sfx.Play(_isOpen ? SfxId.MapOpen : SfxId.MapClose);
                if (_isOpen)
                {
                    _root.SetAsLastSibling();
                }
            }

            _root.gameObject.SetActive(_isOpen);
            if (!_isOpen)
            {
                return;
            }

            if (_drawnVersion != _map.Version)
            {
                Redraw();
            }

            Vector2 playerPosition = _controller.transform.position;
            Place(_playerMark, playerPosition);

            // 내 위치는 고리가 커졌다 작아져서 넓은 맵에서도 바로 찾을 수 있다.
            float ring = 1f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
            _playerRing.localScale = new Vector3(ring, ring, 1f);

            UpdateCurrentZone(ZoneArea.At(playerPosition));
            Color border = UiTheme.Accent;
            border.a = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f);
            _zoneFrameBorder.color = border;

            // 2026-11-19 사용자 결정: 포드는 가 보지 않았어도 늘 보인다. 열렸는지는 맵에서 알 수 없다.
            for (int i = 0; i < _pods.Length; i++)
            {
                Place(_podIcons[i], _pods[i].transform.position);
            }
        }

        private void UpdateCurrentZone(ZoneArea zone)
        {
            if (zone == _currentZone && _whereText.text.Length > 0)
            {
                return;
            }

            _currentZone = zone;
            _whereText.text = "현재 위치  <color=#FFBD4D><b>" + (zone != null ? zone.DisplayName : "복도") + "</b></color>";
            _zoneFrame.gameObject.SetActive(zone != null);
            if (zone != null)
            {
                Vector2 min = _map.Normalize(zone.Area.min);
                Vector2 max = _map.Normalize(zone.Area.max);
                Vector2 size = _mapRect.sizeDelta;
                _zoneFrame.anchoredPosition = new Vector2(min.x * size.x, min.y * size.y) - new Vector2(4f, 4f);
                _zoneFrame.sizeDelta = new Vector2((max.x - min.x) * size.x, (max.y - min.y) * size.y) + new Vector2(8f, 8f);
            }

            RefreshLabels();
        }

        // 구역 이름은 그 구역을 조금이라도 가 봤으면 보인다. 지금 있는 구역은 밝게 쓴다.
        private void RefreshLabels()
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                bool current = _zones[i] == _currentZone;
                _zoneLabels[i].enabled = _zoneKnown[i] || current;
                _zoneLabels[i].color = current ? UiTheme.Accent : UiTheme.Text;
                _zoneLabels[i].fontSize = current ? 26 : 21;
            }
        }

        private void Place(RectTransform mark, Vector2 world)
        {
            Vector2 ratio = _map.Normalize(world);
            mark.anchoredPosition = new Vector2(ratio.x * _mapRect.sizeDelta.x, ratio.y * _mapRect.sizeDelta.y);
        }

        private void Redraw()
        {
            _drawnVersion = _map.Version;
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    Color32 color;
                    if (!_map.IsExplored(x, y))
                    {
                        color = UnknownColor;
                    }
                    else if (_map.IsWall(x, y))
                    {
                        // 바닥과 맞닿은 벽만 밝게 그린다. 벽 속까지 밝으면 큰 청록 덩어리가 길을 가린다.
                        color = TouchesKnownFloor(x, y) ? WallEdgeColor : WallInnerColor;
                    }
                    else
                    {
                        int zone = _cellZone[y * _map.Width + x];
                        color = zone >= 0 ? ZoneColors[zone % ZoneColors.Length] : CorridorColor;
                        if (zone >= 0)
                        {
                            _zoneKnown[zone] = true;
                        }
                    }

                    _pixels[y * _map.Width + x] = color;
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply();
            RefreshLabels();
        }

        private bool TouchesKnownFloor(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < _map.Width && ny < _map.Height && _map.IsExplored(nx, ny) && !_map.IsWall(nx, ny))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 칸마다 어느 구역인지 한 번만 정해 둔다. 구역의 순서는 이름(Id) 순이라 판마다 색이 같다.
        private void CacheZones()
        {
            _zones.AddRange(ZoneArea.Zones);
            _zones.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            _zoneKnown = new bool[_zones.Count];
            _cellZone = new int[_map.Width * _map.Height];
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    Vector2 center = _map.CellCenter(x, y);
                    _cellZone[y * _map.Width + x] = _zones.FindIndex(zone => zone.Contains(center));
                }
            }
        }

        // 맵 전체가 화면 높이 1080 안에 들어가는 칸 크기. 화면이 좁으면 너비에도 맞춘다.
        private float FitCellPixels()
        {
            // 캔버스는 높이 1080에 맞춰 늘어나므로(Match = 높이) 너비만 화면 비율로 바뀐다.
            const float canvasHeight = 1080f;
            float canvasWidth = canvasHeight * Screen.width / Mathf.Max(1f, Screen.height);
            float byHeight = (canvasHeight - HeaderHeight - FooterHeight - ScreenMargin * 2f) / _map.Height;
            float byWidth = (canvasWidth - ScreenMargin * 2f - 40f) / _map.Width;
            return Mathf.Min(_cellPixels, byHeight, byWidth);
        }

        private void Build()
        {
            CacheZones();
            float cell = FitCellPixels();
            var mapSize = new Vector2(_map.Width * cell, _map.Height * cell);

            _root = UiBuilder.Rect("MapScreen", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                mapSize + new Vector2(40f, HeaderHeight + FooterHeight));
            UiBuilder.Window(_root);

            // 제목 줄: 왼쪽은 창 이름, 오른쪽은 지금 있는 구역
            RectTransform title = UiBuilder.Rect("Title", _root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f),
                new Vector2(mapSize.x, 54f));
            Text titleText = UiBuilder.Text("전체 맵 (M)", title, 26, TextAnchor.MiddleLeft, UiTheme.Accent);
            titleText.fontStyle = FontStyle.Bold;
            RectTransform where = UiBuilder.Rect("Where", _root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f),
                new Vector2(mapSize.x, 54f));
            _whereText = UiBuilder.Text(string.Empty, where, 30, TextAnchor.MiddleRight, UiTheme.Text);
            _whereText.supportRichText = true;

            _texture = new Texture2D(_map.Width, _map.Height, TextureFormat.RGBA32, false)
            {
                // 칸 경계가 번지지 않게 점을 그대로 늘린다.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _pixels = new Color32[_texture.width * _texture.height];

            RectTransform frame = UiBuilder.Rect("MapFrame", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, FooterHeight - 3f),
                mapSize + new Vector2(6f, 6f));
            UiBuilder.Image(frame, UiTheme.SlotBorder);

            _mapRect = UiBuilder.Rect("Map", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, FooterHeight), mapSize);
            var image = _mapRect.gameObject.AddComponent<RawImage>();
            image.texture = _texture;
            image.raycastTarget = false;

            // 지금 있는 구역의 테두리
            _zoneFrame = UiBuilder.Rect("CurrentZone", _mapRect, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            _zoneFrameBorder = UiBuilder.Image(_zoneFrame, UiTheme.Accent);
            _zoneFrameBorder.type = Image.Type.Sliced;
            _zoneFrameBorder.sprite = UiBuilder.BorderSprite;

            // 구역 이름은 구역 가운데에 쓴다. 바닥 그림 위에서도 읽히게 외곽선을 두른다.
            foreach (ZoneArea zone in _zones)
            {
                RectTransform label = UiBuilder.Rect("Label " + zone.Id, _mapRect, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(220f, 40f));
                Place(label, zone.Area.center);
                Text text = UiBuilder.Text(zone.DisplayName, label, 21, TextAnchor.MiddleCenter, UiTheme.Text);
                text.fontStyle = FontStyle.Bold;
                UiBuilder.Outline(text);
                text.enabled = false;
                _zoneLabels.Add(text);
            }

            _podIcons = new RectTransform[_pods.Length];
            for (int i = 0; i < _pods.Length; i++)
            {
                // 비상구 표시
                _podIcons[i] = UiBuilder.Rect("ExitIcon", _mapRect, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(54f, 28f));
                UiBuilder.Panel(_podIcons[i], ExitColor, Color.white);
                Text label = UiBuilder.Text("EXIT", _podIcons[i], 17, TextAnchor.MiddleCenter, Color.white);
                label.fontStyle = FontStyle.Bold;
            }

            _playerMark = UiBuilder.Rect("PlayerMark", _mapRect, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(16f, 16f));
            UiBuilder.Image(_playerMark, PlayerColor, _circleSprite);

            _playerRing = UiBuilder.Rect("Ring", _playerMark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(38f, 38f));
            UiBuilder.Image(_playerRing, PlayerColor, UiBuilder.RingSprite);

            RectTransform legend = UiBuilder.Rect("Legend", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f),
                new Vector2(mapSize.x, 36f));
            Text legendText = UiBuilder.Text("<color=#4DC7FF>●</color> 나      <color=#26B352>■</color> 귀환 비콘      "
                + "<color=#FFBD4D>□</color> 지금 있는 구역      <color=#2E2A44>■</color> 아직 안 가 본 곳", legend, 19,
                TextAnchor.MiddleCenter, UiTheme.TextDim);
            legendText.supportRichText = true;

            _root.gameObject.SetActive(false);
        }
    }
}
