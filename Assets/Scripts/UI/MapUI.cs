using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // M키 전체 맵 (기획서 9.2). 가 본 곳만 보이고, 내 위치와 탈출 포드(비상구 표시)를 그린다.
    public class MapUI : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.07f, 0.94f);
        private static readonly Color UnknownColor = new Color(0.02f, 0.02f, 0.03f, 1f);
        private static readonly Color FloorColor = new Color(0.2f, 0.22f, 0.27f, 1f);
        private static readonly Color WallColor = new Color(0.62f, 0.64f, 0.68f, 1f);
        private static readonly Color ExitColor = new Color(0.15f, 0.65f, 0.3f, 1f);

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
                    Color color = !_map.IsExplored(x, y) ? UnknownColor : _map.IsWall(x, y) ? WallColor : FloorColor;
                    _pixels[y * _map.Width + x] = color;
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply();
        }

        private void Build()
        {
            var mapSize = new Vector2(_map.Width * _cellPixels, _map.Height * _cellPixels);

            _root = UiBuilder.Rect("MapScreen", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                mapSize + new Vector2(40f, 90f));
            UiBuilder.Image(_root, PanelColor);

            RectTransform title = UiBuilder.Rect("Title", _root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f),
                new Vector2(mapSize.x, 46f));
            UiBuilder.Text("전체 맵 (M)", title, 26, TextAnchor.MiddleCenter, Color.white);

            // 칸 하나가 픽셀 하나인 그림을 늘려서 그린다.
            _texture = new Texture2D(_map.Width, _map.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _pixels = new Color32[_map.Width * _map.Height];

            _mapRect = UiBuilder.Rect("Map", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), mapSize);
            var image = _mapRect.gameObject.AddComponent<RawImage>();
            image.texture = _texture;
            image.raycastTarget = false;

            _podIcons = new RectTransform[_pods.Length];
            for (int i = 0; i < _pods.Length; i++)
            {
                // 비상구 표시. 단색 도형 단계라 초록 판에 글자로 그린다.
                _podIcons[i] = UiBuilder.Rect("ExitIcon", _mapRect, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(64f, 34f));
                UiBuilder.Image(_podIcons[i], ExitColor);
                Text label = UiBuilder.Text("EXIT", _podIcons[i], 20, TextAnchor.MiddleCenter, Color.white);
                label.fontStyle = FontStyle.Bold;
            }

            _playerMark = UiBuilder.Rect("PlayerMark", _mapRect, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(18f, 18f));
            UiBuilder.Image(_playerMark, new Color(0.3f, 0.75f, 1f), _circleSprite);

            _root.gameObject.SetActive(false);
        }
    }
}
