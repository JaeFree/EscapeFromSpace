using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 프롤로그와 엔딩의 스토리 장면 (기획서 14.1). 장마다 그림 한 장면과 아래 자막을 보여 준다.
    // 클릭·Enter·Space로 넘기고, 오른쪽 위 건너뛰기 버튼이나 Esc로 끝까지 넘긴다.
    // 보여 주는 동안 게임을 멈추고 플레이어 입력을 끈다.
    [DefaultExecutionOrder(-300)]
    public class StoryScreenUI : MonoBehaviour
    {
        [SerializeField, Tooltip("초당 나타나는 글자 수")] private float _typeSpeed = 30f;
        [SerializeField, Tooltip("초. 장이 바뀔 때 검은 화면에서 밝아지는 시간")] private float _fadeTime = 0.6f;

        private readonly List<string> _pages = new List<string>();
        private readonly List<StoryShot> _shots = new List<StoryShot>();
        private Canvas _canvas;
        private PlayerInputReader _input;
        private RectTransform _root;
        private RectTransform _stageParent;
        private RectTransform _skip;
        private Image _skipFill;
        private Text _caption;
        private Text _center;
        private Text _hint;
        private Image _fade;
        private StoryStage _stage;
        private Action _onFinished;
        private int _page;
        private float _shownTime;
        private bool _inputWasEnabled;
        private static float _closedTime = -1f;

        // 초. 장면을 닫은 직후 이만큼은 같은 키·클릭이 다른 창에 닿지 않게 막는다.
        private const float CloseGuard = 0.15f;

        public static bool IsShowing { get; private set; }

        // 장면을 닫은 직후에도 같은 클릭·Esc가 다른 창(설정, 상인)을 건드리지 않게 막는다.
        public static bool IsBlocking => IsShowing || (_closedTime >= 0f && Time.unscaledTime - _closedTime < CloseGuard);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsShowing = false;
            _closedTime = -1f;
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _input = FindFirstObjectByType<PlayerInputReader>();
            Build();
        }

        private void OnDestroy()
        {
            if (IsShowing)
            {
                IsShowing = false;
                Time.timeScale = 1f;
            }
        }

        // story는 빈 줄로 장을 나눈다. shots[i]가 i번째 장의 그림이고, 모자라면 별 하늘이다.
        // credits가 있으면 별 하늘 위 가운데 글로 마지막 장에 붙인다.
        public void Show(string story, string credits, IReadOnlyList<StoryShot> shots, Action onFinished)
        {
            _pages.Clear();
            _shots.Clear();
            foreach (string page in (story ?? string.Empty).Replace("\r", string.Empty).Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                _shots.Add(shots != null && _pages.Count < shots.Count ? shots[_pages.Count] : StoryShot.Stars);
                _pages.Add(page.Trim());
            }

            if (!string.IsNullOrEmpty(credits))
            {
                _pages.Add(credits.Trim());
                _shots.Add(StoryShot.Stars);
            }

            if (_pages.Count == 0)
            {
                onFinished?.Invoke();
                return;
            }

            _onFinished = onFinished;
            _page = 0;
            IsShowing = true;
            Time.timeScale = 0f;
            if (_input != null)
            {
                _inputWasEnabled = _input.enabled;
                _input.enabled = false;
            }

            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _skip.gameObject.SetActive(true);
            _skip.SetAsLastSibling();
            ShowPage();
        }

        private void Update()
        {
            if (!IsShowing)
            {
                return;
            }

            _shownTime += Time.unscaledDeltaTime;
            _stage?.Tick(_shownTime);
            _fade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(_shownTime / _fadeTime));

            string page = _pages[_page];
            int shown = Mathf.Min(page.Length, Mathf.FloorToInt(Mathf.Max(0f, _shownTime - _fadeTime * 0.5f) * _typeSpeed));
            bool typed = shown >= page.Length;
            Text target = IsCreditsPage ? _center : _caption;
            target.text = typed ? page : page.Substring(0, shown);
            _hint.enabled = typed;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Vector2 point = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            bool overSkip = mouse != null && RectTransformUtility.RectangleContainsScreenPoint(_skip, point, UiBuilder.CameraOf(_canvas));
            _skipFill.color = overSkip ? UiTheme.ButtonFill : new Color(0.1f, 0.09f, 0.14f, 0.85f);
            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;

            if ((clicked && overSkip) || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                Finish();
                return;
            }

            bool next = clicked || (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
            if (!next)
            {
                return;
            }

            // 글이 다 나타나기 전에 누르면 먼저 다 보여 준다.
            if (!typed)
            {
                _shownTime = Mathf.Max(_shownTime, _fadeTime * 0.5f + page.Length / Mathf.Max(1f, _typeSpeed) + 0.01f);
                return;
            }

            _page++;
            if (_page < _pages.Count)
            {
                Sfx.Play(SfxId.ItemPick);
                ShowPage();
                return;
            }

            Finish();
        }

        private bool IsCreditsPage => _shots[_page] == StoryShot.Stars;

        private void ShowPage()
        {
            _stage?.Destroy();
            _stage = StoryArt.Build(_shots[_page], _stageParent);
            _shownTime = 0f;
            _caption.text = string.Empty;
            _center.text = string.Empty;
            _hint.text = _page < _pages.Count - 1 ? "클릭 / Enter  다음" : "클릭 / Enter  계속";
            _fade.color = Color.black;
        }

        private void Finish()
        {
            IsShowing = false;
            _closedTime = Time.unscaledTime;
            Time.timeScale = 1f;
            if (_input != null)
            {
                _input.enabled = _inputWasEnabled;
            }

            _stage?.Destroy();
            _stage = null;
            _root.gameObject.SetActive(false);
            _skip.gameObject.SetActive(false);
            Sfx.Play(SfxId.UiConfirm);
            Action finished = _onFinished;
            _onFinished = null;
            finished?.Invoke();
        }

        private void Build()
        {
            _root = UiBuilder.Rect("Story", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f));
            UiBuilder.Image(_root, Color.black).raycastTarget = true;

            // 장면은 1920×1080에 맞춰 둔다. 화면이 더 넓으면 양옆은 검다. 위아래 띠에 가리는 만큼 조금 올린다.
            _stageParent = UiBuilder.Rect("Stage", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1920f, 1080f));
            _stageParent.gameObject.AddComponent<RectMask2D>();

            // 영화처럼 위아래를 검은 띠로 가린다. 자막은 아래 띠에 쓴다.
            RectTransform top = UiBuilder.Rect("TopBar", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 430f), new Vector2(4000f, 2000f));
            UiBuilder.Image(top, Color.black);
            RectTransform bottom = UiBuilder.Rect("BottomBar", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(4000f, 2000f));
            UiBuilder.Image(bottom, Color.black);

            RectTransform caption = UiBuilder.Rect("Caption", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -342f), new Vector2(1500f, 150f));
            _caption = UiBuilder.Text(string.Empty, caption, 30, TextAnchor.UpperCenter, UiTheme.Text);
            _caption.lineSpacing = 1.15f;

            RectTransform center = UiBuilder.Rect("Center", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1400f, 700f));
            _center = UiBuilder.Text(string.Empty, center, 34, TextAnchor.MiddleCenter, UiTheme.Text);
            _center.lineSpacing = 1.4f;
            UiBuilder.Outline(_center);

            RectTransform hint = UiBuilder.Rect("Hint", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -515f), new Vector2(600f, 36f));
            _hint = UiBuilder.Text(string.Empty, hint, 20, TextAnchor.MiddleCenter, UiTheme.TextDim);

            RectTransform fade = UiBuilder.Rect("Fade", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1920f, 760f));
            _fade = UiBuilder.Image(fade, Color.black);

            // 건너뛰기는 화면 오른쪽 위 구석에 붙인다. 화면 비율이 달라도 구석에 있게 캔버스에 바로 붙이고 장면과 함께 켜고 끈다.
            _skip = UiBuilder.Rect("StorySkip", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -28f), new Vector2(210f, 56f));
            _skipFill = UiBuilder.Panel(_skip, new Color(0.1f, 0.09f, 0.14f, 0.85f), UiTheme.SlotBorder);
            Text skipText = UiBuilder.Text("건너뛰기  >>", _skip, 24, TextAnchor.MiddleCenter, UiTheme.Text);
            skipText.fontStyle = FontStyle.Bold;

            _root.gameObject.SetActive(false);
            _skip.gameObject.SetActive(false);
        }
    }
}
