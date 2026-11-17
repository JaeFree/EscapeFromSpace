using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // M키 전체 맵 (기획서 9.2). 가 본 곳만 보이고, 내 위치와 탈출 포드(비상구 표시)를 그린다.
    public class MapUI : MonoBehaviour
    {
        // 맵 한 칸을 가로세로 이만큼의 점으로 그린다. 칸 안에 테두리와 무늬를 넣으려면 점이 여러 개 필요하다.
        private const int CellTexels = 8;

        // 벽은 게임 화면의 벽과 같은 청록, 바닥과 안 가 본 곳은 창의 남보라에 맞춘다.
        private static readonly Color32 UnknownColor = new Color32(12, 10, 20, 255);
        private static readonly Color32 UnknownDotColor = new Color32(30, 26, 46, 255);
        private static readonly Color32 FloorColor = new Color32(44, 40, 62, 255);
        private static readonly Color32 FloorLineColor = new Color32(33, 30, 49, 255);
        private static readonly Color32 WallColor = new Color32(52, 142, 140, 255);
        private static readonly Color32 WallLightColor = new Color32(130, 226, 214, 255);
        private static readonly Color32 WallDarkColor = new Color32(26, 84, 90, 255);
        private static readonly Color ExitColor = new Color(0.15f, 0.65f, 0.3f, 1f);
        private static readonly Color PlayerColor = new Color(0.3f, 0.75f, 1f, 1f);

        [SerializeField] private MapExplorer _map;
        [SerializeField] private RaidManager _raid;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private EscapePod[] _pods;
        [SerializeField] private Sprite _circleSprite;

        // 크기는 1920×1080 화면 기준의 픽셀이다. Play를 다시 시작하면 반영된다.
        [SerializeField, Tooltip("맵 한 칸의 한 변")] private float _cellPixels = 22f;

        private RectTransform _root;
        private RectTransform _mapRect;
        private RectTransform _playerMark;
        private RectTransform _playerRing;
        private RectTransform[] _podIcons;
        private Texture2D _texture;
        private Color32[] _pixels;
        private int _drawnVersion = -1;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

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

            Place(_playerMark, _controller.transform.position);

            // 내 위치는 고리가 커졌다 작아져서 넓은 맵에서도 바로 찾을 수 있다.
            float ring = 1f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f);
            _playerRing.localScale = new Vector3(ring, ring, 1f);

            // 탈출 포드도 가 본 곳에 있을 때만 보인다. 어느 쪽이 작동하는지는 맵에서 알 수 없다.
            for (int i = 0; i < _pods.Length; i++)
            {
                bool known = _map.IsExploredAt(_pods[i].transform.position);
                _podIcons[i].gameObject.SetActive(known);
                if (known)
                {
                    Place(_podIcons[i], _pods[i].transform.position);
                }
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
                    if (!_map.IsExplored(x, y))
                    {
                        DrawUnknown(x, y);
                    }
                    else if (_map.IsWall(x, y))
                    {
                        DrawWall(x, y);
                    }
                    else
                    {
                        DrawFloor(x, y);
                    }
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply();
        }

        // 안 가 본 곳: 어두운 바탕에 점 하나. 빈 화면이 아니라 "아직 모르는 칸"으로 보인다.
        private void DrawUnknown(int x, int y)
        {
            int middle = CellTexels / 2;
            for (int v = 0; v < CellTexels; v++)
            {
                for (int u = 0; u < CellTexels; u++)
                {
                    bool dot = (u == middle - 1 || u == middle) && (v == middle - 1 || v == middle);
                    SetTexel(x, y, u, v, dot ? UnknownDotColor : UnknownColor);
                }
            }
        }

        // 바닥: 왼쪽과 아래에 옅은 선을 넣어 타일처럼 칸이 보이게 한다.
        private void DrawFloor(int x, int y)
        {
            for (int v = 0; v < CellTexels; v++)
            {
                for (int u = 0; u < CellTexels; u++)
                {
                    SetTexel(x, y, u, v, u == 0 || v == 0 ? FloorLineColor : FloorColor);
                }
            }
        }

        // 벽: 바닥과 맞닿은 쪽에만 테두리를 그린다. 위와 왼쪽은 밝게, 아래와 오른쪽은 어둡게 해서 도드라져 보인다.
        // 이어진 벽 사이에는 선이 없어서 한 덩어리로 보인다.
        private void DrawWall(int x, int y)
        {
            bool left = IsKnownFloor(x - 1, y);
            bool right = IsKnownFloor(x + 1, y);
            bool down = IsKnownFloor(x, y - 1);
            bool up = IsKnownFloor(x, y + 1);
            int last = CellTexels - 1;

            for (int v = 0; v < CellTexels; v++)
            {
                for (int u = 0; u < CellTexels; u++)
                {
                    Color32 color = WallColor;
                    if ((up && v == last) || (left && u == 0))
                    {
                        color = WallLightColor;
                    }
                    else if ((down && v == 0) || (right && u == last))
                    {
                        color = WallDarkColor;
                    }

                    SetTexel(x, y, u, v, color);
                }
            }
        }

        private bool IsKnownFloor(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _map.Width && y < _map.Height && _map.IsExplored(x, y) && !_map.IsWall(x, y);
        }

        private void SetTexel(int x, int y, int u, int v, Color32 color)
        {
            _pixels[(y * CellTexels + v) * _map.Width * CellTexels + x * CellTexels + u] = color;
        }

        private void Build()
        {
            var mapSize = new Vector2(_map.Width * _cellPixels, _map.Height * _cellPixels);

            _root = UiBuilder.Rect("MapScreen", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                mapSize + new Vector2(40f, 90f));
            UiBuilder.Window(_root);

            RectTransform title = UiBuilder.Rect("Title", _root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f),
                new Vector2(mapSize.x, 46f));
            Text titleText = UiBuilder.Text("전체 맵 (M)", title, 26, TextAnchor.MiddleCenter, UiTheme.Accent);
            titleText.fontStyle = FontStyle.Bold;

            _texture = new Texture2D(_map.Width * CellTexels, _map.Height * CellTexels, TextureFormat.RGBA32, false)
            {
                // 화면의 칸 크기가 점의 정수배가 아니어도 선 굵기가 들쭉날쭉하지 않게 부드럽게 늘린다.
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _pixels = new Color32[_texture.width * _texture.height];

            RectTransform frame = UiBuilder.Rect("MapFrame", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 17f),
                mapSize + new Vector2(6f, 6f));
            UiBuilder.Image(frame, UiTheme.SlotBorder);

            _mapRect = UiBuilder.Rect("Map", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), mapSize);
            var image = _mapRect.gameObject.AddComponent<RawImage>();
            image.texture = _texture;
            image.raycastTarget = false;

            _podIcons = new RectTransform[_pods.Length];
            for (int i = 0; i < _pods.Length; i++)
            {
                // 비상구 표시
                _podIcons[i] = UiBuilder.Rect("ExitIcon", _mapRect, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(64f, 34f));
                UiBuilder.Panel(_podIcons[i], ExitColor, Color.white);
                Text label = UiBuilder.Text("EXIT", _podIcons[i], 20, TextAnchor.MiddleCenter, Color.white);
                label.fontStyle = FontStyle.Bold;
            }

            _playerMark = UiBuilder.Rect("PlayerMark", _mapRect, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(16f, 16f));
            UiBuilder.Image(_playerMark, PlayerColor, _circleSprite);

            _playerRing = UiBuilder.Rect("Ring", _playerMark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(34f, 34f));
            UiBuilder.Image(_playerRing, PlayerColor, UiBuilder.RingSprite);

            _root.gameObject.SetActive(false);
        }
    }
}
