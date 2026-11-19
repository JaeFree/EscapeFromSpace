using UnityEngine;
using UnityEngine.Serialization;

namespace EscapeFromSpace
{
    // 레이드 규칙의 수치 (기획서 7장). 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    [CreateAssetMenu(fileName = "RaidConfig", menuName = "EscapeFromSpace/Raid Config")]
    public class RaidConfig : ScriptableObject
    {
        [Header("산소")]
        [SerializeField, Tooltip("초. 헬멧 없이 가진 산소")] private float _baseOxygenSeconds;
        [SerializeField, Tooltip("산소가 0일 때 초당 HP 감소")] private float _suffocationDamagePerSecond;
        [SerializeField, Tooltip("초. 산소가 이만큼 남으면 게이지가 빨갛게 깜빡인다")] private float _oxygenWarningSeconds;

        [SerializeField, Tooltip("초. 산소가 얼마 안 남았을 때 경고음이 울리는 간격")] private float _oxygenWarningBeepInterval = 2.5f;
        [SerializeField, Tooltip("초. 질식 중에 경고음이 울리는 간격")] private float _suffocationBeepInterval = 0.8f;

        [Header("탈출 포드")]
        [FormerlySerializedAs("_escapeCountdown")]
        [SerializeField, Tooltip("초. 포드 안에서 탈출 키를 누르고 있어야 하는 시간")] private float _escapeHoldTime;
        [SerializeField, Tooltip("초. 레이드를 시작하고 이 시간이 지나야 포드가 열린다")] private float _podOpenDelay;
        [SerializeField, Tooltip("테스트용. 켜면 시작부터 열려 있다")] private bool _podsAlwaysOpen;

        // 2026-11-19 사용자 결정: 판마다 몇몇 배에 이변이 생긴다 (기획서 19.4). 비율은 합에 대한 몫이다.
        [Header("우주선 이변 (기획서 19.4)")]
        [SerializeField, Tooltip("이변 없음의 비율")] private float _noEventWeight = 40f;
        [SerializeField] private float _blackoutWeight = 25f;
        [SerializeField] private float _securityAlertWeight = 20f;
        [SerializeField] private float _alienInvasionWeight = 15f;
        [SerializeField, Tooltip("이변이 생기는 배의 수")] private int _eventShipCount = 2;
        [SerializeField, Tooltip("이변이 생길 수 있는 배 (구역 Id)")] private string[] _eventShipIds = { "Cargo", "Quarters", "Medbay", "Lab", "Security", "Engine", "Power" };
        [SerializeField, Tooltip("위 배들의 화면 이름 (같은 순서). 대기실의 출격 창에 쓴다")] private string[] _eventShipNames = { "화물선 잔해", "이민선", "병원선", "연구선", "군 순찰선", "채굴선", "연료 수송선" };
        [SerializeField, Tooltip("늘 에일리언이 있는 배. 에일리언 습격은 이 배들을 고르지 않는다")] private string[] _alienShipIds = { "Medbay", "Lab" };
        [SerializeField, Tooltip("이변이 생긴 배의 컨테이너에서 더 나오는 아이템 비율 (0.25 = 25%)")] private float _eventLootBonus = 0.25f;
        [SerializeField, Tooltip("칸. 정전된 배 안에서 헬멧 등으로 보이는 반경")] private float _blackoutVisionRadius = 6f;
        [SerializeField, Tooltip("정전된 배의 로봇 시야 거리 배율 (에일리언은 그대로)")] private float _blackoutSightMultiplier = 0.6f;
        [SerializeField, Tooltip("보안 경계인 배에 더 놓는 로봇 수")] private int _alertExtraRobots = 2;
        [SerializeField, Tooltip("보안 경계인 배의 로봇이 듣는 반경 배율")] private float _alertHearingMultiplier = 1.5f;
        [SerializeField, Tooltip("에일리언 습격 때 일반 로봇 대신 놓는 에일리언 (차례로 번갈아)")] private RobotData[] _invaders;

        public float NoEventWeight => _noEventWeight;
        public float BlackoutWeight => _blackoutWeight;
        public float SecurityAlertWeight => _securityAlertWeight;
        public float AlienInvasionWeight => _alienInvasionWeight;
        public int EventShipCount => _eventShipCount;
        public string[] EventShipIds => _eventShipIds;
        public string[] EventShipNames => _eventShipNames;
        public string[] AlienShipIds => _alienShipIds;
        public float EventLootBonus => _eventLootBonus;
        public float BlackoutVisionRadius => _blackoutVisionRadius;
        public float BlackoutSightMultiplier => _blackoutSightMultiplier;
        public int AlertExtraRobots => _alertExtraRobots;
        public float AlertHearingMultiplier => _alertHearingMultiplier;
        public RobotData[] Invaders => _invaders;

        public float BaseOxygenSeconds => _baseOxygenSeconds;
        public float SuffocationDamagePerSecond => _suffocationDamagePerSecond;
        public float OxygenWarningSeconds => _oxygenWarningSeconds;
        public float OxygenWarningBeepInterval => _oxygenWarningBeepInterval;
        public float SuffocationBeepInterval => _suffocationBeepInterval;
        public float EscapeHoldTime => _escapeHoldTime;
        public float PodOpenDelay => _podOpenDelay;
        public bool PodsAlwaysOpen => _podsAlwaysOpen;
    }
}
