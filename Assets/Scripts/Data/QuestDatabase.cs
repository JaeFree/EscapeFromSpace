using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    public enum QuestKind
    {
        // 레이드에서 탈출한 횟수
        Escape,

        // 로봇 아무거나 처치
        KillAny,

        // 지정한 종류의 일반 로봇 처치
        KillRobot,

        // 지정한 네임드 처치
        KillNamed,

        // 네임드를 모두 한 번 이상 처치
        AllNamed,

        // 아이템을 건넨다
        Deliver,

        // 구역에 들어간 뒤 그 판에 탈출
        Visit,

        // 장거리 탈출 포드 수리 완료 (엔딩)
        RepairPod,
    }

    // 퀘스트 목록 (기획서 14.3·14.4). 이름·대사·조건·보상은 초기값이라 에셋의 Inspector에서 고친다.
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "EscapeFromSpace/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        [Serializable]
        public class Quest
        {
            [SerializeField, Tooltip("저장 파일과 상인의 해금 칸에 적히는 이름. 정한 뒤에는 바꾸지 않는다")] private string _id;
            [SerializeField] private string _title;
            [SerializeField, Tooltip("이 퀘스트를 주는 상인의 Id")] private string _npc;
            [SerializeField] private int _chapter = 1;
            [SerializeField, TextArea(3, 8), Tooltip("상인이 퀘스트를 줄 때 하는 말")] private string _story;
            [SerializeField, TextArea(2, 5), Tooltip("완료했을 때 하는 말")] private string _doneLine;
            [SerializeField, Tooltip("같은 상인의 앞 퀘스트 말고도 먼저 끝내야 하는 퀘스트")] private string _requires;

            [Header("조건")]
            [SerializeField] private QuestKind _kind;
            [SerializeField, Tooltip("처치·탈출 횟수")] private int _count = 1;
            [SerializeField, Tooltip("KillRobot: 이 로봇")] private RobotData _robot;
            [SerializeField, Tooltip("KillNamed: 네임드 Id")] private string _namedId;
            [SerializeField, Tooltip("Visit: 구역 Id")] private string _zoneId;
            [SerializeField, Tooltip("Deliver: 건넬 아이템")] private List<TechTree.Cost> _deliver = new List<TechTree.Cost>();

            [Header("보상")]
            [SerializeField] private int _credits;
            [SerializeField] private List<TechTree.Cost> _rewardItems = new List<TechTree.Cost>();
            [SerializeField, Tooltip("보상 설명에 덧붙이는 말 (상점 해금 등)")] private string _rewardNote;
            [SerializeField, Tooltip("완료하면 보관함을 이 크기로 늘린다. 0이면 그대로 (기획서 19.5)")] private Vector2Int _secureBoxSize;
            [SerializeField, Tooltip("완료하면 로봇 개가 합류한다 (기획서 19.5)")] private bool _unlocksDog;

            public string Id => _id;
            public string Title => _title;
            public string Npc => _npc;
            public int Chapter => _chapter;
            public string Story => _story;
            public string DoneLine => _doneLine;
            public string Requires => _requires;
            public QuestKind Kind => _kind;
            public int Count => _count;
            public RobotData Robot => _robot;
            public string NamedId => _namedId;
            public string ZoneId => _zoneId;
            public IReadOnlyList<TechTree.Cost> Deliver => _deliver;
            public int Credits => _credits;
            public IReadOnlyList<TechTree.Cost> RewardItems => _rewardItems;
            public Vector2Int SecureBoxSize => _secureBoxSize;
            public bool UnlocksDog => _unlocksDog;
            public string RewardNote => _rewardNote;
        }

        [SerializeField] private List<Quest> _quests = new List<Quest>();
        [SerializeField, Tooltip("네임드 퀘스트(AllNamed)가 세는 네임드 Id 목록")] private List<string> _allNamed = new List<string>();
        [SerializeField, Tooltip("장거리 탈출 포드를 고치는 데 필요한 부품 (기획서 14.5)")] private List<TechTree.Cost> _podParts = new List<TechTree.Cost>();
        [SerializeField, TextArea(4, 12), Tooltip("처음 시작할 때 보여 주는 글. 빈 줄로 장을 나눈다")] private string _prologue;
        [SerializeField, TextArea(4, 12), Tooltip("포드를 고쳐 떠날 때 보여 주는 글. 빈 줄로 장을 나눈다")] private string _ending;
        [SerializeField, TextArea(4, 12)] private string _credits;
        [SerializeField, Tooltip("프롤로그 장마다 보여 줄 그림. 장보다 적으면 나머지는 별 하늘")]
        private StoryShot[] _prologueShots = { StoryShot.Planet, StoryShot.Attack, StoryShot.Crash, StoryShot.Shelter };
        [SerializeField, Tooltip("엔딩 장마다 보여 줄 그림. 만든 사람 장은 별 하늘")]
        private StoryShot[] _endingShots = { StoryShot.Launch, StoryShot.Liftoff, StoryShot.Escape, StoryShot.Farewell };

        public IReadOnlyList<Quest> Quests => _quests;
        public IReadOnlyList<string> AllNamed => _allNamed;
        public IReadOnlyList<TechTree.Cost> PodParts => _podParts;
        public string Prologue => _prologue;
        public string Ending => _ending;
        public string CreditsText => _credits;
        public IReadOnlyList<StoryShot> PrologueShots => _prologueShots;
        public IReadOnlyList<StoryShot> EndingShots => _endingShots;

        public Quest Find(string id)
        {
            foreach (Quest quest in _quests)
            {
                if (quest.Id == id)
                {
                    return quest;
                }
            }

            return null;
        }
    }
}
