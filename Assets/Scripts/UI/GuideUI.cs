using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 첫 출격 안내 (기획서 20.5). 첫 퀘스트를 끝내기 전까지 화면 아래 가운데에 "지금 할 일" 한 줄을 보여 준다.
    // 처음 하는 사람이 대기실의 여러 화면 사이에서 길을 잃지 않고 첫 판을 끝내게 하려는 것이다.
    public class GuideUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField, Tooltip("레이드 씬에서만 넣는다. 비어 있으면 대기실 안내를 보여 준다")] private RaidManager _raid;
        [SerializeField, Tooltip("이 퀘스트를 끝내면 안내를 그만둔다")] private string _questId = "Q01";
        [SerializeField, Tooltip("전체 맵에 '추천'을 붙일 구역 Id")] private string _recommendedZoneId = "Cargo";
        [SerializeField, Tooltip("이 퀘스트를 맡기는 상인의 화면 이름")] private string _questGiverName = "제작소의 오토";
        [SerializeField] private int _fontSize = 24;

        private RectTransform _root;
        private Text _text;

        // 안내 중이면 추천 구역 Id, 아니면 null. 전체 맵이 읽는다.
        public static string RecommendedZoneId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            RecommendedZoneId = null;
        }

        private bool IsActive => GameSession.Data != null && !GameSession.Data.CompletedQuests.Contains(_questId);

        private void Awake()
        {
            // 화면 위쪽은 보유 가치 판, 퀘스트 목록, 이변·수색 알림, 맵 위 이름표가 차지해서 아래 가운데의 무기칸·진행 바 위에 둔다.
            _root = UiBuilder.Rect("Guide", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(820f, 54f));
            UiBuilder.Panel(_root, new Color(0.075f, 0.06f, 0.11f, 0.88f), UiTheme.Accent);
            _text = UiBuilder.Text(string.Empty, _root, _fontSize, TextAnchor.MiddleCenter, UiTheme.Text);
            _text.fontStyle = FontStyle.Bold;
            _root.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            RecommendedZoneId = null;
        }

        private void Update()
        {
            bool active = IsActive && _inventory != null && (_raid == null || _raid.Outcome == RaidOutcome.InProgress);
            RecommendedZoneId = active ? _recommendedZoneId : null;
            _root.gameObject.SetActive(active);
            if (active)
            {
                _text.text = "지금 할 일  <color=#FFBD4D>" + CurrentStep() + "</color>";
            }
        }

        private string CurrentStep()
        {
            if (_raid != null)
            {
                return _inventory.Bag.Items.Count == 0
                    ? "가까운 화물선 잔해에서 컨테이너를 열어(E 길게) 물자를 챙기세요"
                    : "M 지도에서 귀환 비콘을 찾아 X를 길게 눌러 탈출하세요";
            }

            // 대기실: 이미 한 번 탈출했으면 퀘스트를 끝내러 상인에게 간다.
            QuestLog log = QuestLog.Current;
            if (log != null && log.GetCount(_questId) > 0)
            {
                return $"{_questGiverName}에게 가서 E로 말을 걸고 첫 퀘스트 보상을 받으세요";
            }

            return _inventory.GetEquipped(EquipSlot.Weapon) == null
                ? "창고(왼쪽 위)에서 무기를 꺼내 무기칸에 장착하세요 (Tab)"
                : "출격 해치로 가서 출격하세요";
        }
    }
}
