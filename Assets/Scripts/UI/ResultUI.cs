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

        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _button;
        private Text _title;
        private Text _body;
        private bool _shown;

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

            int seconds = Mathf.FloorToInt(_raid.ElapsedTime);
            string time = $"걸린 시간  {seconds / 60}:{seconds % 60:00}";

            if (_raid.Outcome == RaidOutcome.Escaped)
            {
                _title.text = "탈출 성공";
                _title.color = new Color(0.4f, 1f, 0.55f);
                _body.text = $"{time}\n가지고 나온 가치  {_raid.CarriedValue}";
            }
            else
            {
                string secured = _raid.SecuredItem != null ? _raid.SecuredItem.Data.DisplayName : "없음";
                _title.text = "사망";
                _title.color = new Color(1f, 0.35f, 0.35f);
                _body.text = $"{time}\n잃은 가치  {_raid.CarriedValue}\n보관함으로 지킨 것  {secured}";
            }
        }

        // 대기실 씬이 생기기 전까지는 같은 씬을 다시 불러 처음부터 시작한다 (임시).
        private void Leave()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Build()
        {
            _root = UiBuilder.Rect("ResultScreen", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(4000f, 4000f));
            UiBuilder.Image(_root, new Color(0f, 0f, 0f, 0.7f));

            RectTransform box = UiBuilder.Rect("Box", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(640f, 440f));
            UiBuilder.Image(box, new Color(0.08f, 0.09f, 0.12f, 0.98f));

            RectTransform title = UiBuilder.Rect("Title", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                new Vector2(600f, 90f));
            _title = UiBuilder.Text(string.Empty, title, 60, TextAnchor.MiddleCenter, Color.white);
            _title.fontStyle = FontStyle.Bold;

            RectTransform body = UiBuilder.Rect("Body", box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
                new Vector2(560f, 170f));
            _body = UiBuilder.Text(string.Empty, body, 30, TextAnchor.MiddleCenter, Color.white);
            _body.lineSpacing = 1.3f;

            _button = UiBuilder.Rect("Button", box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f),
                new Vector2(320f, 76f));
            UiBuilder.Image(_button, new Color(0.25f, 0.5f, 0.9f));
            UiBuilder.Text("다시 시작 (Enter)", _button, 28, TextAnchor.MiddleCenter, Color.white);

            _root.gameObject.SetActive(false);
        }
    }
}
