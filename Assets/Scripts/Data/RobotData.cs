using UnityEngine;

namespace EscapeFromSpace
{
    public enum RobotAttackKind
    {
        Melee,
        Gun,
        Shotgun,
        Sniper,
    }

    // 기획서 5.3 로봇 표의 한 줄. 수치는 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    // 플레이어의 무기 에셋과 따로 둔다. 플레이어 무기를 조정해도 로봇이 바뀌지 않게 하려는 것이다.
    [CreateAssetMenu(fileName = "RobotData", menuName = "EscapeFromSpace/Robot Data")]
    public class RobotData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField, Tooltip("단색 도형 단계에서 종류를 구분하는 색")] private Color _color = Color.white;
        [SerializeField] private float _maxHp;

        [Header("공격")]
        [SerializeField] private RobotAttackKind _attackKind;
        [SerializeField, Tooltip("한 발(펠릿 하나)의 피해")] private float _damage;
        [SerializeField] private int _pelletCount;
        [SerializeField, Tooltip("도. 총은 ±이 값, 샷건은 부채꼴 전체 각, 근접은 공격 범위 각도")] private float _spreadAngle;
        [SerializeField, Tooltip("칸")] private float _range;

        [Header("사격 패턴")]
        [SerializeField, Tooltip("초. 쏘기 전에 충전하거나 조준선을 보여 주는 시간")] private float _windupTime;
        [SerializeField, Tooltip("초. 충전의 마지막 이 시간 동안은 쏠 방향이 고정된다")] private float _aimLockTime;
        [SerializeField, Tooltip("한 번에 이어서 쏘는 발 수")] private int _burstCount;
        [SerializeField, Tooltip("초. 한 발을 쏜 뒤 다음 발까지")] private float _burstInterval;
        [SerializeField, Tooltip("초. 다 쏜 뒤 쉬는 시간")] private float _restTime;

        [Header("이동")]
        [SerializeField, Tooltip("칸/초. 0이면 제자리에서 좌우로 훑는다")] private float _patrolSpeed;
        [SerializeField, Tooltip("칸/초. 0이면 추격할 때도 움직이지 않는다")] private float _chaseSpeed;
        [SerializeField, Tooltip("도/초")] private float _turnSpeed;
        [SerializeField, Tooltip("도. 움직이지 않는 로봇이 좌우로 훑는 각도 (±)")] private float _sweepAngle;
        [SerializeField, Tooltip("도/초")] private float _sweepSpeed;

        [Header("감지")]
        [SerializeField, Tooltip("칸")] private float _sightDistance;
        [SerializeField, Tooltip("도. 전체 시야각")] private float _sightAngle;
        [SerializeField, Tooltip("칸. 총소리를 듣는 반경")] private float _hearingRadius;
        [SerializeField, Tooltip("초. 발견한 뒤 멈춰서 바라보는 시간")] private float _alertDuration;
        [SerializeField, Tooltip("초. 추격하다 이 시간 동안 못 찾으면 원래 자리로 돌아간다")] private float _chaseTimeout;

        public string DisplayName => _displayName;
        public Color Color => _color;
        public float MaxHp => _maxHp;
        public RobotAttackKind AttackKind => _attackKind;
        public float Damage => _damage;
        public int PelletCount => _pelletCount;
        public float SpreadAngle => _spreadAngle;
        public float Range => _range;
        public float WindupTime => _windupTime;
        public float AimLockTime => _aimLockTime;
        public int BurstCount => _burstCount;
        public float BurstInterval => _burstInterval;
        public float RestTime => _restTime;
        public float PatrolSpeed => _patrolSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float TurnSpeed => _turnSpeed;
        public float SweepAngle => _sweepAngle;
        public float SweepSpeed => _sweepSpeed;
        public float SightDistance => _sightDistance;
        public float SightAngle => _sightAngle;
        public float HearingRadius => _hearingRadius;
        public float AlertDuration => _alertDuration;
        public float ChaseTimeout => _chaseTimeout;
    }
}
