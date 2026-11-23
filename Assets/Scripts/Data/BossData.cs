using System;
using UnityEngine;

namespace EscapeFromSpace
{
    // 네임드 패턴의 종류 (기획서 21장). 네임드마다 일반 1개, 특수 1개를 고른다.
    public enum BossPatternKind
    {
        CrateThrow,
        Charge,
        FanShot,
        Summon,
        GasPool,
        Quarantine,
        SpinSlash,
        WeldDash,
        Discharge,
        ChainLightning,
        Spiral,
        PiercingLaser,
        TripleSnipe,
        Artillery,
    }

    // 패턴 하나의 수치. 종류마다 쓰는 칸이 다르다 (안 쓰는 칸은 무시).
    [Serializable]
    public class BossPattern
    {
        [SerializeField] private string _displayName;
        [SerializeField] private BossPatternKind _kind;
        [SerializeField, Tooltip("초. 이 패턴을 다시 쓰기까지 (내부 쿨)")] private float _cooldown = 6f;
        [SerializeField, Tooltip("칸. 플레이어가 이 거리 안에 있을 때만 쓴다")] private float _useDistance = 10f;
        [SerializeField, Tooltip("초. 예고를 보여 주는 시간")] private float _windup = 0.8f;
        [SerializeField, Tooltip("한 번 맞을 때의 피해. 장판·번개는 초당 피해")] private float _damage = 20f;
        [SerializeField, Tooltip("칸. 폭발·장판·고리의 반경, 빔·돌진의 판정 반폭")] private float _radius = 1.5f;
        [SerializeField, Tooltip("두 번째 피해. 용접 돌진의 불 장판(초당), 격리 봉쇄의 산탄 한 발")] private float _secondaryDamage = 10f;
        [SerializeField, Tooltip("칸. 돌진·빔·번개의 길이, 탄의 사거리, 포격이 흩어지는 반경")] private float _length = 8f;
        [SerializeField, Tooltip("발 수, 줄 수, 폭발 수, 호출 수 등")] private int _count = 1;
        [SerializeField, Tooltip("도. 부채꼴 전체 각, 레이저 줄 사이 각")] private float _angle = 40f;
        [SerializeField, Tooltip("칸/초 또는 도/초. 돌진·탄의 속도, 번개·나선의 회전 속도")] private float _speed = 10f;
        [SerializeField, Tooltip("초. 연속 공격 사이 간격")] private float _interval = 0.3f;
        [SerializeField, Tooltip("초. 장판·고리·번개·나선이 이어지는 시간, 돌진 뒤 기절 시간")] private float _duration = 4f;
        [SerializeField, Tooltip("되풀이 횟수. 3연속 저격의 발 수, 격리 봉쇄의 산탄 횟수")] private int _repeat = 1;
        [SerializeField, Tooltip("판마다 쓸 수 있는 횟수. 0이면 제한 없음")] private int _maxUses;

        public string DisplayName => _displayName;
        public BossPatternKind Kind => _kind;
        public float Cooldown => _cooldown;
        public float UseDistance => _useDistance;
        public float Windup => _windup;
        public float Damage => _damage;
        public float Radius => _radius;
        public float SecondaryDamage => _secondaryDamage;
        public float Length => _length;
        public int Count => _count;
        public float Angle => _angle;
        public float Speed => _speed;
        public float Interval => _interval;
        public float Duration => _duration;
        public int Repeat => _repeat;
        public int MaxUses => _maxUses;
    }

    // 네임드 한 기의 패턴 묶음 (기획서 21장). RobotData의 Boss 칸에 넣는다.
    [CreateAssetMenu(fileName = "Boss", menuName = "EscapeFromSpace/Boss Data")]
    public class BossData : ScriptableObject
    {
        [SerializeField] private BossPattern _normal = new BossPattern();
        [SerializeField] private BossPattern _special = new BossPattern();
        [SerializeField, Tooltip("초. 패턴 하나가 끝나고 다음 패턴까지 최소 간격 (그 사이에는 기본 공격)")] private float _minGap = 2.5f;
        [SerializeField, Tooltip("초. 싸움을 시작하고 첫 일반·특수 패턴까지")] private float _firstNormalDelay = 2f;
        [SerializeField] private float _firstSpecialDelay = 7f;

        [Header("마지막 발악 (상위 3기)")]
        [SerializeField] private bool _hasLastStand;
        [SerializeField, Range(0f, 1f), Tooltip("HP가 이 비율 아래로 내려가면 시작")] private float _lastStandHpRatio = 0.3f;
        [SerializeField, Tooltip("발악 때 빌려 쓰는 패턴의 주인 (이들의 특수 패턴)")] private BossData[] _lastStandSources = new BossData[0];
        [SerializeField, Tooltip("초. 빌려 쓴 패턴의 내부 쿨 (원래 쿨보다 짧으면 이 값)")] private float _borrowedCooldown = 20f;

        [Header("호출 (비상 경보)")]
        [SerializeField] private Robot _summonPrefab;
        [SerializeField] private RobotData _summonData;

        public BossPattern Normal => _normal;
        public BossPattern Special => _special;
        public float MinGap => _minGap;
        public float FirstNormalDelay => _firstNormalDelay;
        public float FirstSpecialDelay => _firstSpecialDelay;
        public bool HasLastStand => _hasLastStand;
        public float LastStandHpRatio => _lastStandHpRatio;
        public BossData[] LastStandSources => _lastStandSources;
        public float BorrowedCooldown => _borrowedCooldown;
        public Robot SummonPrefab => _summonPrefab;
        public RobotData SummonData => _summonData;
    }
}
