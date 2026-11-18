using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 대기실 화면의 글자: 창고·포탈·사격장의 이름, 보유 총 가치 (기획서 7장), 출격 확인 창. 든 무게는 HUD의 게이지가 보여 준다.
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private LobbyManager _lobby;
        [SerializeField] private Transform _shootingRange;
        [SerializeField, Tooltip("칸. 이름을 대상보다 이만큼 위에 띄운다")] private float _labelOffset = 2.1f;
        [SerializeField] private int _fontSize = 26;

        private Canvas _canvas;
        private Camera _camera;
        private RectTransform _storageLabel;
        private RectTransform _portalLabel;
        private RectTransform _rangeLabel;
        private Text _summary;
        private RectTransform _confirm;
        private RectTransform _confirmButton;
        private RectTransform _cancelButton;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _camera = Camera.main;

            _storageLabel = CreateLabel("창고 (E)");
            _portalLabel = CreateLabel("레이드 포탈");
            _rangeLabel = CreateLabel("사격장");

            RectTransform summary = UiBuilder.Rect("Summary", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -24f),
                new Vector2(600f, 56f));
            UiBuilder.Panel(summary, UiTheme.SlotFill, UiTheme.SlotBorder);
            _summary = UiBuilder.Text(string.Empty, summary, _fontSize, TextAnchor.MiddleCenter, UiTheme.Accent);
            _summary.fontStyle = FontStyle.Bold;

            BuildConfirm();
        }

        private void Update()
        {
            Place(_storageLabel, _lobby.Storage.transform.position);
            Place(_portalLabel, _lobby.Portal.position);
            _rangeLabel.gameObject.SetActive(_shootingRange != null);
            if (_shootingRange != null)
            {
                Place(_rangeLabel, _shootingRange.position);
            }

            int credits = GameSession.Data != null ? GameSession.Data.Credits : 0;
            _summary.text = $"보유 총 가치  {_lobby.TotalValue}     크레딧  {credits:N0}";

            UpdateConfirm();
        }

        // 출격 확인 창. 버튼을 누르거나 Enter(확인) / Esc(취소)로 고른다.
        private void UpdateConfirm()
        {
            bool show = _lobby.IsConfirming;
            if (show && !_confirm.gameObject.activeSelf)
            {
                _confirm.SetAsLastSibling();
            }

            _confirm.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            Vector2 point = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            Camera uiCamera = UiBuilder.CameraOf(_canvas);

            if ((keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                || (clicked && RectTransformUtility.RectangleContainsScreenPoint(_confirmButton, point, uiCamera)))
            {
                _lobby.ConfirmLaunch();
            }
            else if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (clicked && RectTransformUtility.RectangleContainsScreenPoint(_cancelButton, point, uiCamera)))
            {
                _lobby.CancelLaunch();
            }
        }

        private void Place(RectTransform label, Vector3 world)
        {
            UiBuilder.MoveToScreenPoint(_canvas, label, _camera.WorldToScreenPoint(world + Vector3.up * _labelOffset));
        }

        private RectTransform CreateLabel(string content)
        {
            // 맵 위에 뜨는 이름표. 바닥이나 벽의 색과 섞이지 않게 틀을 두른다.
            RectTransform label = UiBuilder.Rect("Label", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero,
                new Vector2(content.Length * _fontSize + 56f, _fontSize + 22f));
            UiBuilder.Panel(label, UiTheme.SlotFill, UiTheme.SlotBorder);
            UiBuilder.Text(content, label, _fontSize, TextAnchor.MiddleCenter, UiTheme.Text);

            // 가방 화면과 창고 칸이 이름 위에 그려지도록 맨 뒤에 둔다.
            label.SetAsFirstSibling();
            return label;
        }

        private void BuildConfirm()
        {
            _confirm = UiBuilder.Dim("LaunchConfirm", transform);

            RectTransform box = UiBuilder.Rect("Box", _confirm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(640f, 300f));
            UiBuilder.Window(box);

            RectTransform message = UiBuilder.Rect("Message", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f),
                new Vector2(600f, 90f));
            Text text = UiBuilder.Text("출격하시겠습니까?", message, 42, TextAnchor.MiddleCenter, UiTheme.Text);
            text.fontStyle = FontStyle.Bold;

            var buttonSize = new Vector2(260f, 76f);
            _confirmButton = UiBuilder.Button("확인 (Enter)", box, new Vector2(0.5f, 0f), new Vector2(-150f, 40f), buttonSize, true);
            _cancelButton = UiBuilder.Button("취소 (Esc)", box, new Vector2(0.5f, 0f), new Vector2(150f, 40f), buttonSize, false);
            _confirm.gameObject.SetActive(false);
        }
    }
}
