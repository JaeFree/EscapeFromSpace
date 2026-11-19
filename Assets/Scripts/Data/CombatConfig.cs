using UnityEngine;

namespace EscapeFromSpace
{
    // 방어력 계수는 기획서 11.2에서 플레이하며 확정하기로 한 값이라 에셋에 둔다.
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "EscapeFromSpace/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [SerializeField, Tooltip("방어력 1당 피해 감소 비율 (0.04 = 4%)")] private float _armorReductionPerPoint;
        [SerializeField, Tooltip("이 값을 넘는 방어력은 계산에 넣지 않는다")] private int _maxArmor;

        [Header("반동 카메라 흔들림")]
        [SerializeField, Tooltip("칸/도. 무기 반동(도/발)에 곱해 흔들림 세기를 정한다")] private float _cameraShakePerRecoilDegree;
        [SerializeField, Tooltip("초")] private float _cameraShakeDuration;

        // 2026-11-19 사용자 결정: 은신과 엄폐 (기획서 19.3). 로봇이 보자마자 알아채지 않고 게이지가 차야 발견한다.
        [Header("은신: 발견 게이지 (기획서 19.3)")]
        [SerializeField, Tooltip("초. 이 거리 안에서 게이지가 가득 차는 시간")] private float _detectNearTime = 0.5f;
        [SerializeField, Tooltip("초. 시야 끝에서 게이지가 가득 차는 시간")] private float _detectFarTime = 2f;
        [SerializeField, Tooltip("칸")] private float _detectNearDistance = 3f;
        [SerializeField, Tooltip("살금 걷기 중 게이지가 차는 속도 배율")] private float _sneakDetectMultiplier = 0.5f;
        [SerializeField, Tooltip("엄폐 중 게이지가 차는 속도 배율")] private float _coverDetectMultiplier = 0.5f;
        [SerializeField, Tooltip("구르는 중 게이지가 차는 속도 배율")] private float _rollDetectMultiplier = 1.5f;
        [SerializeField, Tooltip("1초에 줄어드는 양 (가득 = 1)")] private float _detectDecayPerSecond = 0.4f;
        [SerializeField, Tooltip("이 값을 넘으면 의심 상태가 되어 본 곳을 살피러 간다")] private float _suspicionThreshold = 0.5f;
        [SerializeField, Tooltip("초. 의심한 곳에 가서 둘러보는 시간")] private float _suspicionLookTime = 3f;

        [Header("은신: 살금 걷기와 소리")]
        [SerializeField, Tooltip("살금 걷기(Ctrl) 이동 속도 배율")] private float _sneakSpeedRatio = 0.5f;
        [SerializeField, Tooltip("칸. 걸을 때 발소리가 들리는 반경")] private float _walkNoiseRadius = 4f;
        [SerializeField, Tooltip("칸. 구를 때 소리가 들리는 반경")] private float _rollNoiseRadius = 5f;
        [SerializeField, Tooltip("초. 걷는 동안 발소리를 내는 간격")] private float _footstepInterval = 0.5f;
        [SerializeField, Tooltip("칸. 총소리가 들리는 반경 (소음기가 없을 때)")] private float _gunNoiseRadius = 8f;
        [SerializeField, Tooltip("칸. 로봇 Hearing Radius가 이 값이면 소리 반경을 그대로 듣는다. 더 크면 더 멀리서 듣는다")] private float _referenceHearing = 8f;

        [Header("은신: 엄폐와 뒤에서 베기")]
        [SerializeField, Tooltip("칸. 적 쪽으로 이 거리 안에 벽·엄폐물이 있으면 엄폐 중")] private float _coverDistance = 1f;
        [SerializeField, Tooltip("엄폐 중인 플레이어를 쏘는 로봇의 퍼짐 배율")] private float _coverSpreadMultiplier = 3f;
        [SerializeField, Tooltip("알아채지 못한 적을 광선검으로 벨 때의 피해 배율")] private float _backstabMultiplier = 3f;

        public float DetectNearTime => _detectNearTime;
        public float DetectFarTime => _detectFarTime;
        public float DetectNearDistance => _detectNearDistance;
        public float SneakDetectMultiplier => _sneakDetectMultiplier;
        public float CoverDetectMultiplier => _coverDetectMultiplier;
        public float RollDetectMultiplier => _rollDetectMultiplier;
        public float DetectDecayPerSecond => _detectDecayPerSecond;
        public float SuspicionThreshold => _suspicionThreshold;
        public float SuspicionLookTime => _suspicionLookTime;
        public float SneakSpeedRatio => _sneakSpeedRatio;
        public float WalkNoiseRadius => _walkNoiseRadius;
        public float RollNoiseRadius => _rollNoiseRadius;
        public float FootstepInterval => _footstepInterval;
        public float GunNoiseRadius => _gunNoiseRadius;
        public float ReferenceHearing => _referenceHearing;
        public float CoverDistance => _coverDistance;
        public float CoverSpreadMultiplier => _coverSpreadMultiplier;
        public float BackstabMultiplier => _backstabMultiplier;

        public float CameraShakePerRecoilDegree => _cameraShakePerRecoilDegree;
        public float CameraShakeDuration => _cameraShakeDuration;
        public float ArmorReductionPerPoint => _armorReductionPerPoint;
        public int MaxArmor => _maxArmor;
    }
}
