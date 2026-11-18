using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 프롤로그와 엔딩의 글 화면 (기획서 14.1). 빈 줄로 나눈 장을 하나씩 보여 주고, 클릭이나 Enter·Space로 넘긴다.
    // 보여 주는 동안 게임을 멈추고 플레이어 입력을 끈다.
    [DefaultExecutionOrder(-300)]
    public class StoryScreenUI : MonoBehaviour
    {
        [SerializeField, Tooltip("초. 한 장의 글자가 다 나타나는 시간")] private float _fadeTime = 0.6f;

        private readonly List<string> _pages = new List<string>();
        private Canvas _canvas;
        private PlayerInputReader _input;
        private RectTransform _root;
        private Text _text;
        private Text _hint;
        private Action _onFinished;
        private int _page;
        private float _shownTime;
        private bool _inputWasEnabled;

        public static bool IsShowing { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsShowing = false;
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

        // story는 빈 줄로 장을 나눈다. credits가 있으면 마지막 장으로 붙인다.
        public void Show(string story, string credits, Action onFinished)
        {
            _pages.Clear();
            foreach (string page in (story ?? string.Empty).Replace("\r", string.Empty).Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                _pages.Add(page.Trim());
            }

            if (!string.IsNullOrEmpty(credits))
            {
                _pages.Add(credits.Trim());
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
            ShowPage();
        }

        private void Update()
        {
            if (!IsShowing)
            {
                return;
            }

            _shownTime += Time.unscaledDeltaTime;
            _text.color = new Color(1f, 0.95f, 0.86f, Mathf.Clamp01(_shownTime / _fadeTime));

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            bool next = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame
                    || keyboard.escapeKey.wasPressedThisFrame));
            if (!next)
            {
                return;
            }

            // 글이 다 나타나기 전에 누르면 먼저 다 보여 준다.
            if (_shownTime < _fadeTime)
            {
                _shownTime = _fadeTime;
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

        private void ShowPage()
        {
            _text.text = _pages[_page];
            _shownTime = 0f;
            _hint.text = _page < _pages.Count - 1 ? "클릭 / Enter  다음" : "클릭 / Enter  계속";
        }

        private void Finish()
        {
            IsShowing = false;
            Time.timeScale = 1f;
            if (_input != null)
            {
                _input.enabled = _inputWasEnabled;
            }

            _root.gameObject.SetActive(false);
            Sfx.Play(SfxId.UiConfirm);
            Action finished = _onFinished;
            _onFinished = null;
            finished?.Invoke();
        }

        private void Build()
        {
            _root = UiBuilder.Rect("Story", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f));
            UiBuilder.Image(_root, new Color(0.01f, 0.01f, 0.03f, 0.97f));

            RectTransform body = UiBuilder.Rect("Body", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1200f, 620f));
            _text = UiBuilder.Text(string.Empty, body, 34, TextAnchor.MiddleCenter, UiTheme.Text);
            _text.lineSpacing = 1.45f;

            RectTransform hint = UiBuilder.Rect("Hint", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -380f), new Vector2(600f, 50f));
            _hint = UiBuilder.Text(string.Empty, hint, 24, TextAnchor.MiddleCenter, UiTheme.TextDim);
            _root.gameObject.SetActive(false);
        }
    }
}
