using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 게임 시작 화면: 우주선 무덤 그림 위의 제목과 메뉴 (시작, 설정, 종료).
    // 마우스로 누르거나 위아래 키로 고르고 Enter로 정한다. 시작하면 대기실로 간다.
    public class TitleUI : MonoBehaviour
    {
        private static readonly string[] Labels = { "시작", "설정", "게임 종료" };

        [SerializeField] private string _lobbySceneName = "Lobby";
        [SerializeField, Tooltip("초. 시작을 누른 뒤 화면이 어두워지는 시간")] private float _fadeOutTime = 0.6f;

        private Canvas _canvas;
        private SettingsUI _settings;
        private StoryStage _stage;
        private readonly RectTransform[] _buttons = new RectTransform[3];
        private readonly Image[] _fills = new Image[3];
        private Text _title;
        private Image _fade;
        private int _selected;
        private float _time;
        private float _leaving = -1f;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _settings = GetComponent<SettingsUI>();
            Time.timeScale = 1f;
            Build();
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            _stage.Tick(_time);
            _title.color = Color.Lerp(UiTheme.Text, UiTheme.Accent, 0.5f + Mathf.Sin(_time * 1.4f) * 0.25f);

            if (_leaving >= 0f)
            {
                _leaving += Time.unscaledDeltaTime;
                _fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(_leaving / _fadeOutTime));
                if (_leaving >= _fadeOutTime)
                {
                    SceneManager.LoadScene(_lobbySceneName);
                }

                return;
            }

            // 처음 열 때 검은 화면에서 밝아진다.
            _fade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(_time / 1.2f));
            if (SettingsUI.IsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                {
                    Select((_selected + _buttons.Length - 1) % _buttons.Length);
                }
                else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                {
                    Select((_selected + 1) % _buttons.Length);
                }
                else if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    Choose(_selected);
                    return;
                }
            }

            if (mouse == null)
            {
                return;
            }

            Vector2 point = mouse.position.ReadValue();
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_buttons[i], point, UiBuilder.CameraOf(_canvas)))
                {
                    continue;
                }

                if (mouse.delta.ReadValue().sqrMagnitude > 0f && _selected != i)
                {
                    Select(i);
                }

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Choose(i);
                }

                break;
            }
        }

        private void Select(int index)
        {
            _selected = index;
            Sfx.Play(SfxId.ItemPick);
            Refresh();
        }

        private void Choose(int index)
        {
            Select(index);
            switch (index)
            {
                case 0:
                    Sfx.Play(SfxId.UiConfirm);
                    _leaving = 0f;
                    break;
                case 1:
                    _settings.Open();
                    break;
                default:
                    Application.Quit();
                    break;
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool on = i == _selected;
                _fills[i].color = on ? UiTheme.ButtonFill : new Color(0.08f, 0.07f, 0.12f, 0.82f);
                _buttons[i].localScale = Vector3.one * (on ? 1.04f : 1f);
            }
        }

        private void Build()
        {
            // 설정 창(톱니 버튼)이 먼저 만들어지므로, 시작 화면은 그 뒤에 깔리도록 맨 앞 형제로 둔다.
            RectTransform layer = UiBuilder.Stretch("Title", (RectTransform)transform);
            layer.SetAsFirstSibling();
            RectTransform back = UiBuilder.Rect("TitleBack", layer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f));
            UiBuilder.Image(back, Color.black);
            _stage = StoryArt.Build(StoryShot.Graveyard, back);
            _stage.Root.anchoredPosition = new Vector2(220f, 0f);

            // 글자가 그림 위에서 읽히도록 왼쪽을 어둡게 덮는다.
            RectTransform shade = UiBuilder.Rect("Shade", layer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(900f, 1200f));
            UiBuilder.Image(shade, new Color(0f, 0f, 0.02f, 0.62f));

            RectTransform title = UiBuilder.Rect("Title", layer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 250f), new Vector2(760f, 150f));
            _title = UiBuilder.Text("ESCAPE", title, 128, TextAnchor.MiddleLeft, UiTheme.Text);
            _title.fontStyle = FontStyle.Bold;
            UiBuilder.Outline(_title);

            RectTransform sub = UiBuilder.Rect("Sub", layer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(118f, 150f), new Vector2(760f, 70f));
            Text subText = UiBuilder.Text("from Space", sub, 56, TextAnchor.MiddleLeft, UiTheme.Accent);
            subText.fontStyle = FontStyle.Italic;
            UiBuilder.Outline(subText);

            RectTransform tag = UiBuilder.Rect("Tagline", layer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(122f, 90f), new Vector2(760f, 40f));
            UiBuilder.Text("우주선 무덤에서 살아 나가라", tag, 24, TextAnchor.MiddleLeft, UiTheme.TextDim);

            // 저장이 있으면 이어서 하고, 없으면 처음부터 시작한다 (프롤로그는 대기실에서 나온다).
            bool hasSave = File.Exists(SaveSystem.FilePath);
            for (int i = 0; i < _buttons.Length; i++)
            {
                string label = i == 0 ? (hasSave ? "이어하기" : "새로 시작") : Labels[i];
                RectTransform button = UiBuilder.Rect(label, layer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(118f, -40f - i * 92f), new Vector2(360f, 74f));
                _fills[i] = UiBuilder.Panel(button, Color.clear, UiTheme.SlotBorder);
                Text text = UiBuilder.Text(label, button, 30, TextAnchor.MiddleCenter, UiTheme.Text);
                text.fontStyle = FontStyle.Bold;
                _buttons[i] = button;
            }

            RectTransform help = UiBuilder.Rect("Help", layer, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(118f, 40f), new Vector2(900f, 36f));
            UiBuilder.Text("↑↓ / W S  고르기    Enter  정하기", help, 20, TextAnchor.MiddleLeft, UiTheme.TextDim);

            RectTransform fade = UiBuilder.Rect("Fade", layer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f));
            _fade = UiBuilder.Image(fade, Color.black);
            Refresh();
        }
    }
}
