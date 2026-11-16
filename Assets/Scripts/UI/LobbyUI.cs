using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 대기실 화면의 글자: 창고와 포탈의 이름, 보유 총 가치와 지금 든 무게 (기획서 7장).
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private LobbyManager _lobby;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerData _playerData;
        [SerializeField, Tooltip("칸. 이름을 대상보다 이만큼 위에 띄운다")] private float _labelOffset = 2.1f;
        [SerializeField] private int _fontSize = 26;

        private Canvas _canvas;
        private Camera _camera;
        private RectTransform _storageLabel;
        private RectTransform _portalLabel;
        private Text _summary;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _camera = Camera.main;

            _storageLabel = CreateLabel("창고 (E)");
            _portalLabel = CreateLabel("레이드 포탈");

            RectTransform summary = UiBuilder.Rect("Summary", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -24f),
                new Vector2(520f, 90f));
            _summary = UiBuilder.Text(string.Empty, summary, _fontSize, TextAnchor.UpperRight, Color.white);
            _summary.gameObject.AddComponent<Outline>().effectColor = Color.black;
        }

        private void Update()
        {
            Place(_storageLabel, _lobby.Storage.transform.position);
            Place(_portalLabel, _lobby.Portal.position);
            _summary.text = $"보유 총 가치  {_lobby.TotalValue}\n든 무게  {_inventory.TotalWeight:0.0} / {_playerData.MaxWeight:0}kg";
        }

        private void Place(RectTransform label, Vector3 world)
        {
            UiBuilder.MoveToScreenPoint(_canvas, label, _camera.WorldToScreenPoint(world + Vector3.up * _labelOffset));
        }

        private RectTransform CreateLabel(string content)
        {
            RectTransform label = UiBuilder.Rect("Label", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(260f, 44f));
            Text text = UiBuilder.Text(content, label, _fontSize, TextAnchor.MiddleCenter, Color.white);
            text.gameObject.AddComponent<Outline>().effectColor = Color.black;

            // 가방 화면과 창고 칸이 이름 위에 그려지도록 맨 뒤에 둔다.
            label.SetAsFirstSibling();
            return label;
        }
    }
}
