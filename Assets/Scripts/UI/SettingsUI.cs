using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 오른쪽 아래 톱니 버튼과 설정 창 (기획서 13.1): 음량 4갈래, 도움말, 게임 종료.
    // 다른 창보다 먼저 Esc를 읽어야, 같은 프레임에 다른 창이 닫힌 뒤 설정이 또 열리는 일이 없다.
    [DefaultExecutionOrder(-200)]
    public class SettingsUI : MonoBehaviour
    {
        private enum Page
        {
            Main,
            Help,
            QuitConfirm,
        }

        private static readonly string[] ChannelNames = { "전체 음량", "음악", "효과음", "배경 소리" };

        private const string HelpText =
            "목표: 정거장에서 물자를 챙겨 탈출 포드로 살아 나오고, 부품을 모아 장거리 탈출 포드를 고친다.\n\n" +
            "이동  WASD          구르기  Space\n" +
            "조준  마우스        사격  좌클릭 (저격총은 우클릭 조준)\n" +
            "재장전  R           도구칸 사용  1, 2, 3\n" +
            "가방  Tab           가방에서 아이템 회전  R\n" +
            "무기 교체  가방에서 드래그\n" +
            "묶음 나누기  가방에서 우클릭\n" +
            "열기·말 걸기  E (컨테이너는 길게)\n" +
            "탈출  포드 안에서 X 길게\n" +
            "전체 맵  M          설정  Esc";

        [SerializeField, Tooltip("톱니 버튼의 크기 (화면 기준 1920×1080)")] private float _buttonSize = 72f;

        private Canvas _canvas;
        private InventoryUI _inventory;
        private MapUI _map;
        private TechTreeUI _techTree;
        private MerchantUI _merchant;
        private ResultUI _result;
        private LobbyManager _lobby;
        private PlayerInputReader _input;
        private PlayerController _controller;

        private RectTransform _gear;
        private Image _gearIcon;
        private RectTransform _root;
        private RectTransform _mainPage;
        private RectTransform _helpPage;
        private RectTransform _quitPage;
        private Text _quitMessage;
        private readonly RectTransform[] _bars = new RectTransform[4];
        private readonly Image[] _fills = new Image[4];
        private readonly Text[] _values = new Text[4];
        private RectTransform _helpButton;
        private RectTransform _quitButton;
        private RectTransform _closeButton;
        private RectTransform _helpBack;
        private RectTransform _quitYes;
        private RectTransform _quitNo;
        private int _draggingBar = -1;
        private bool _isOpen;
        private bool _inputWasEnabled;
        private float _timeScaleBefore = 1f;
        private Page _page;

        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsOpen = false;
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _inventory = GetComponent<InventoryUI>();
            _map = GetComponent<MapUI>();
            _techTree = GetComponent<TechTreeUI>();
            _merchant = GetComponent<MerchantUI>();
            _result = GetComponent<ResultUI>();
            _lobby = FindFirstObjectByType<LobbyManager>();
            _input = FindFirstObjectByType<PlayerInputReader>();
            _controller = FindFirstObjectByType<PlayerController>();
            Build();
        }

        private void OnDestroy()
        {
            // 열린 채로 씬이 바뀌면 멈춘 시간과 꺼진 입력이 다음 씬으로 넘어가지 않게 한다.
            if (_isOpen)
            {
                Time.timeScale = 1f;
                IsOpen = false;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            Vector2 point = mouse != null ? mouse.position.ReadValue() : Vector2.zero;

            bool blocked = IsBlockedByOtherWindow();
            _gear.gameObject.SetActive(!blocked || _isOpen);

            if (!_isOpen)
            {
                if (blocked)
                {
                    return;
                }

                if (clicked && Contains(_gear, point))
                {
                    Open();
                    return;
                }

                if (escape)
                {
                    HandleEscapeWhileClosed();
                }

                return;
            }

            if (escape)
            {
                if (_page == Page.Main)
                {
                    Close();
                }
                else
                {
                    ShowPage(Page.Main);
                }

                return;
            }

            switch (_page)
            {
                case Page.Main:
                    UpdateMainPage(mouse, clicked, point);
                    break;
                case Page.Help:
                    if (clicked && Contains(_helpBack, point))
                    {
                        Sfx.Play(SfxId.UiCancel);
                        ShowPage(Page.Main);
                    }

                    break;
                case Page.QuitConfirm:
                    if (clicked && Contains(_quitYes, point))
                    {
                        Quit();
                    }
                    else if (clicked && Contains(_quitNo, point))
                    {
                        Sfx.Play(SfxId.UiCancel);
                        ShowPage(Page.Main);
                    }

                    break;
            }
        }

        // 스스로 Esc를 읽는 창(강화 트리, 출격 확인, 결과 화면, 수량 창)이 떠 있으면 설정은 끼어들지 않는다.
        private bool IsBlockedByOtherWindow()
        {
            return StoryScreenUI.IsShowing
                || (_techTree != null && _techTree.IsOpen)
                || (_merchant != null && _merchant.IsOpen)
                || (_result != null && _result.IsShown)
                || (_lobby != null && _lobby.IsConfirming)
                || (_inventory != null && _inventory.IsHandlingEscape)
                || (_controller != null && _controller.IsDead);
        }

        // Esc는 열린 창부터 닫고, 닫을 창이 없을 때 설정을 연다.
        private void HandleEscapeWhileClosed()
        {
            if (_inventory != null && _inventory.IsOpen)
            {
                _inventory.Close();
                return;
            }

            if (_map != null && _map.IsOpen)
            {
                _map.Close();
                return;
            }

            Open();
        }

        private void UpdateMainPage(Mouse mouse, bool clicked, Vector2 point)
        {
            bool held = mouse != null && mouse.leftButton.isPressed;
            if (!held)
            {
                _draggingBar = -1;
            }

            if (clicked)
            {
                for (int i = 0; i < _bars.Length; i++)
                {
                    if (Contains(_bars[i], point))
                    {
                        _draggingBar = i;
                    }
                }

                if (_draggingBar < 0)
                {
                    if (Contains(_helpButton, point))
                    {
                        Sfx.Play(SfxId.UiConfirm);
                        ShowPage(Page.Help);
                        return;
                    }

                    if (Contains(_quitButton, point))
                    {
                        Sfx.Play(SfxId.UiPrompt);
                        ShowPage(Page.QuitConfirm);
                        return;
                    }

                    if (Contains(_closeButton, point))
                    {
                        Close();
                        return;
                    }
                }
            }

            if (_draggingBar >= 0)
            {
                RectTransform bar = _bars[_draggingBar];
                RectTransformUtility.ScreenPointToLocalPointInRectangle(bar, point, UiBuilder.CameraOf(_canvas), out Vector2 local);
                float value = Mathf.Clamp01((local.x - bar.rect.xMin) / bar.rect.width);

                // 5% 단위로 끊어야 원하는 값에 맞추기 쉽다.
                value = Mathf.Round(value * 20f) / 20f;
                GameSettings.Set((VolumeChannel)_draggingBar, value);
                RefreshVolumes();
            }
        }

        private void Open()
        {
            if (_inventory != null && _inventory.IsOpen)
            {
                _inventory.Close();
            }

            if (_map != null)
            {
                _map.Close();
            }

            _isOpen = true;
            IsOpen = true;
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;

            // 멈춘 동안 Tab, M, E 같은 게임 입력이 들어가지 않게 플레이어 입력을 끈다.
            if (_input != null)
            {
                _inputWasEnabled = _input.enabled;
                _input.enabled = false;
            }

            Sfx.Play(SfxId.MapOpen);
            ShowPage(Page.Main);
            RefreshVolumes();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _gear.SetAsLastSibling();
        }

        private void Close()
        {
            _isOpen = false;
            IsOpen = false;
            _draggingBar = -1;
            Time.timeScale = _timeScaleBefore;
            if (_input != null)
            {
                _input.enabled = _inputWasEnabled;
            }

            Sfx.Play(SfxId.MapClose);
            _root.gameObject.SetActive(false);
        }

        private void ShowPage(Page page)
        {
            _page = page;
            _mainPage.gameObject.SetActive(page == Page.Main);
            _helpPage.gameObject.SetActive(page == Page.Help);
            _quitPage.gameObject.SetActive(page == Page.QuitConfirm);

            if (page == Page.QuitConfirm)
            {
                bool inRaid = GameSession.Data != null && GameSession.Data.InRaid;
                _quitMessage.text = inRaid
                    ? "게임을 끌까요?\n\n레이드 중에 끄면 이번 판은 사망으로 처리되어\n보관함을 뺀 장비와 가방을 잃습니다."
                    : "게임을 끌까요?";
            }
        }

        private void RefreshVolumes()
        {
            for (int i = 0; i < _fills.Length; i++)
            {
                float value = GameSettings.Get((VolumeChannel)i);
                _fills[i].fillAmount = value;
                _values[i].text = Mathf.RoundToInt(value * 100f).ToString();
            }
        }

        private void Quit()
        {
            Sfx.Play(SfxId.UiConfirm);
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool Contains(RectTransform rect, Vector2 point)
        {
            return rect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(rect, point, UiBuilder.CameraOf(_canvas));
        }

        private void Build()
        {
            _gear = UiBuilder.Rect("SettingsButton", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f),
                new Vector2(_buttonSize, _buttonSize));
            UiBuilder.Panel(_gear, UiTheme.PanelFill, UiTheme.PanelBorder);
            RectTransform icon = UiBuilder.Rect("Icon", _gear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(_buttonSize * 0.68f, _buttonSize * 0.68f));
            _gearIcon = UiBuilder.Image(icon, UiTheme.Accent, DrawGear());
            _gearIcon.preserveAspect = true;

            _root = UiBuilder.Dim("Settings", transform);
            RectTransform box = UiBuilder.Rect("Box", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(760f, 660f));
            UiBuilder.Window(box);

            RectTransform title = UiBuilder.Rect("Title", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f),
                new Vector2(700f, 70f));
            Text titleText = UiBuilder.Text("설정", title, 44, TextAnchor.MiddleCenter, UiTheme.Accent);
            titleText.fontStyle = FontStyle.Bold;

            BuildMainPage(box);
            BuildHelpPage(box);
            BuildQuitPage(box);

            _root.gameObject.SetActive(false);
        }

        private void BuildMainPage(RectTransform box)
        {
            _mainPage = CreatePage(box, "Main");

            for (int i = 0; i < ChannelNames.Length; i++)
            {
                float y = -120f - i * 78f;
                RectTransform label = UiBuilder.Rect("Label", _mainPage, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(60f, y),
                    new Vector2(170f, 50f));
                UiBuilder.Text(ChannelNames[i], label, 28, TextAnchor.MiddleLeft, UiTheme.Text);

                _fills[i] = UiBuilder.Bar("Volume", _mainPage, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(240f, y),
                    new Vector2(370f, 34f), UiTheme.Accent);
                _bars[i] = (RectTransform)_fills[i].transform.parent;

                RectTransform value = UiBuilder.Rect("Value", _mainPage, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(625f, y),
                    new Vector2(80f, 50f));
                _values[i] = UiBuilder.Text(string.Empty, value, 28, TextAnchor.MiddleRight, UiTheme.Text);
            }

            _helpButton = UiBuilder.Button("도움말", _mainPage, new Vector2(0.5f, 0f), new Vector2(-180f, 140f), new Vector2(300f, 70f), false);
            _quitButton = UiBuilder.Button("게임 종료", _mainPage, new Vector2(0.5f, 0f), new Vector2(180f, 140f), new Vector2(300f, 70f), false);
            _closeButton = UiBuilder.Button("돌아가기 (Esc)", _mainPage, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(360f, 70f), true);
        }

        private void BuildHelpPage(RectTransform box)
        {
            _helpPage = CreatePage(box, "Help");
            RectTransform body = UiBuilder.Rect("Body", _helpPage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f),
                new Vector2(660f, 440f));
            Text text = UiBuilder.Text(HelpText, body, 24, TextAnchor.UpperLeft, UiTheme.Text);
            text.lineSpacing = 1.25f;
            _helpBack = UiBuilder.Button("돌아가기 (Esc)", _helpPage, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(360f, 70f), true);
        }

        private void BuildQuitPage(RectTransform box)
        {
            _quitPage = CreatePage(box, "Quit");
            RectTransform body = UiBuilder.Rect("Body", _quitPage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f),
                new Vector2(660f, 300f));
            _quitMessage = UiBuilder.Text(string.Empty, body, 28, TextAnchor.MiddleCenter, UiTheme.Text);
            _quitMessage.lineSpacing = 1.2f;
            _quitYes = UiBuilder.Button("종료", _quitPage, new Vector2(0.5f, 0f), new Vector2(-170f, 60f), new Vector2(280f, 70f), true);
            _quitNo = UiBuilder.Button("취소", _quitPage, new Vector2(0.5f, 0f), new Vector2(170f, 60f), new Vector2(280f, 70f), false);
        }

        private static RectTransform CreatePage(RectTransform box, string name)
        {
            RectTransform page = UiBuilder.Rect(name, box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, box.sizeDelta);
            return page;
        }

        // 글꼴에 톱니 글자가 없을 수 있어 그림을 직접 그린다. 이빨 8개, 가운데 구멍.
        private static Sprite DrawGear()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float center = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float tooth = Mathf.Cos(angle * 8f) > 0.25f ? 1f : 0f;
                    float outer = Mathf.Lerp(23f, 30f, tooth);
                    float alpha = Mathf.Clamp01(outer - radius + 0.5f) * Mathf.Clamp01(radius - 9.5f + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
