using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 레이드가 끝났을 때의 결과 화면 (기획서 7장). 뜨는 동안 게임을 멈춘다.
    public class ResultUI : MonoBehaviour
    {
        [SerializeField] private RaidManager _raid;
        [SerializeField, Tooltip("결과 화면에서 돌아갈 대기실 씬의 이름")] private string _lobbySceneName = "Lobby";

        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _button;
        private Text _title;
        private Text _body;
        private Text _buttonText;
        private RectTransform _xpRoot;
        private Image _xpBar;
        private Text _xpLevel;
        private Text _xpCount;
        private bool _shown;

        public bool IsShown => _shown;

        // 경험치 바 연출: 들어올 때의 값에서 시작해 얻은 만큼 채운다.
        private int _animLevel;
        private float _animXp;
        private float _animRemaining;
        private float _animDelay;
        private float _levelPulse;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();

            // 앞 판에서 멈춘 채로 씬을 다시 불렀을 수 있다.
            Time.timeScale = 1f;
            Build();
        }

        private void Update()
        {
            if (!_shown)
            {
                if (_raid.Outcome != RaidOutcome.InProgress)
                {
                    Show();
                }

                return;
            }

            AnimateXp(Time.unscaledDeltaTime);

            // 게임이 멈춰 있어도 입력은 들어온다. 버튼을 누르거나 Enter를 누르면 넘어간다.
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame
                && RectTransformUtility.RectangleContainsScreenPoint(_button, mouse.position.ReadValue(), UiBuilder.CameraOf(_canvas));
            bool entered = keyboard != null && keyboard.enterKey.wasPressedThisFrame;

            if (clicked || entered)
            {
                Leave();
            }
        }

        private void Show()
        {
            _shown = true;
            Time.timeScale = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();

            _buttonText.text = GameSession.RaidFromLobby ? "대기실로 (Enter)" : "다시 시작 (Enter)";

            int seconds = Mathf.FloorToInt(_raid.ElapsedTime);
            string time = $"걸린 시간  {seconds / 60}:{seconds % 60:00}";
            PlayerProgress progress = PlayerProgress.Current;
            string xp = progress != null ? $"\n경험치  +{_raid.XpGained}" : string.Empty;
            StartXpAnimation(progress);

            if (_raid.Outcome == RaidOutcome.Escaped)
            {
                _title.text = "탈출 성공";
                _title.color = new Color(0.4f, 1f, 0.55f);
                string blueprints = _raid.NewBlueprints.Count > 0 ? $"\n새 설계도  {string.Join(", ", _raid.NewBlueprints)}" : string.Empty;
                _body.text = $"{time}\n가지고 나온 가치  {_raid.CarriedValue}{xp}{blueprints}";
            }
            else
            {
                string secured = _raid.SecuredItem != null ? _raid.SecuredItem.Data.DisplayName : "없음";
                _title.text = "사망";
                _title.color = new Color(1f, 0.35f, 0.35f);
                _body.text = $"{time}\n잃은 가치  {_raid.CarriedValue}\n보관함으로 지킨 것  {secured}{xp}";
            }
        }

        private void StartXpAnimation(PlayerProgress progress)
        {
            _xpRoot.gameObject.SetActive(progress != null);
            if (progress == null)
            {
                return;
            }

            _animLevel = progress.StartLevel;
            _animXp = progress.StartXp;
            _animRemaining = _raid.XpGained;
            _animDelay = 0.5f;
            DrawXp(progress.Tree);
        }

        // 바가 한 번 차는 데 약 1초. 얻은 경험치가 많아 레벨이 여러 번 올라도 한 칸씩 보여 준다.
        private void AnimateXp(float deltaTime)
        {
            PlayerProgress progress = PlayerProgress.Current;
            if (progress == null)
            {
                return;
            }

            TechTree tree = progress.Tree;
            if (_levelPulse > 0f)
            {
                _levelPulse = Mathf.Max(0f, _levelPulse - deltaTime);
                _xpLevel.rectTransform.localScale = Vector3.one * (1f + _levelPulse * 1.2f);
            }

            if (_animDelay > 0f)
            {
                _animDelay -= deltaTime;
                return;
            }

            if (_animRemaining <= 0f || _animLevel >= tree.MaxLevel)
            {
                return;
            }

            float need = tree.XpToNext(_animLevel);
            float step = Mathf.Min(_animRemaining, need * deltaTime);
            _animXp += step;
            _animRemaining -= step;
            if (_animXp >= need - 0.001f)
            {
                _animXp = 0f;
                _animLevel++;
                _levelPulse = 0.25f;
                Sfx.Play(SfxId.LevelUp);
            }

            DrawXp(tree);
        }

        private void DrawXp(TechTree tree)
        {
            bool max = _animLevel >= tree.MaxLevel;
            int need = tree.XpToNext(_animLevel);
            _xpBar.fillAmount = max ? 1f : _animXp / need;
            _xpLevel.text = $"Lv. {_animLevel}";
            _xpCount.text = max ? "MAX" : $"{Mathf.FloorToInt(_animXp)} / {need}";
        }

        // 대기실에서 들어온 판이면 대기실로 돌아간다. 레이드 씬을 바로 Play했으면 같은 씬을 다시 불러 처음부터 한다.
        private void Leave()
        {
            Sfx.Play(SfxId.UiConfirm);
            Time.timeScale = 1f;
            if (GameSession.RaidFromLobby)
            {
                SceneManager.LoadScene(_lobbySceneName);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void Build()
        {
            _root = UiBuilder.Dim("ResultScreen", transform);

            RectTransform box = UiBuilder.Rect("Box", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(640f, 520f));
            UiBuilder.Window(box);

            RectTransform title = UiBuilder.Rect("Title", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                new Vector2(600f, 90f));
            _title = UiBuilder.Text(string.Empty, title, 60, TextAnchor.MiddleCenter, Color.white);
            _title.fontStyle = FontStyle.Bold;

            RectTransform body = UiBuilder.Rect("Body", box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f),
                new Vector2(560f, 200f));
            _body = UiBuilder.Text(string.Empty, body, 30, TextAnchor.MiddleCenter, UiTheme.Text);
            _body.lineSpacing = 1.3f;

            // 본문 아래: 레벨과 경험치 바
            _xpRoot = UiBuilder.Rect("Xp", box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 136f), new Vector2(560f, 44f));
            RectTransform level = UiBuilder.Rect("Level", _xpRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                new Vector2(110f, 44f));
            _xpLevel = UiBuilder.Text(string.Empty, level, 30, TextAnchor.MiddleCenter, UiTheme.Accent);
            _xpLevel.fontStyle = FontStyle.Bold;
            _xpBar = UiBuilder.Bar("Bar", _xpRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 0f),
                new Vector2(440f, 26f), new Color(0.55f, 0.85f, 1f));
            RectTransform count = UiBuilder.Rect("Count", _xpRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 0f),
                new Vector2(440f, 26f));
            _xpCount = UiBuilder.Text(string.Empty, count, 20, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Outline(_xpCount);

            _button = UiBuilder.Button("Button", box, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(320f, 76f), true);
            _buttonText = _button.GetComponentInChildren<Text>();

            _root.gameObject.SetActive(false);
        }
    }
}
