using UnityEngine;

namespace EscapeFromSpace
{
    // 기획서의 플레이어 수치는 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    [CreateAssetMenu(fileName = "PlayerData", menuName = "EscapeFromSpace/Player Data")]
    public class PlayerData : ScriptableObject
    {
        [Header("이동")]
        [SerializeField, Tooltip("칸/초")] private float _moveSpeed;

        [SerializeField, Tooltip("재장전 중 이동 속도에 곱하는 값. 0.7이면 30% 느려진다")] private float _reloadMoveSpeedMultiplier;

        [Header("무게")]
        [SerializeField, Tooltip("kg")] private float _maxWeight;
        [SerializeField, Tooltip("최대 무게 대비 이 비율부터 느려진다 (0.5 = 50%)")] private float _slowStartRatio;
        [SerializeField, Tooltip("느려지기 시작할 때의 감소량 (0.2 = 20% 감소)")] private float _slowAtStart;
        [SerializeField, Tooltip("이 비율만큼 더 찰 때마다 추가로 느려진다 (0.1 = 10%마다)")] private float _slowStepRatio;
        [SerializeField, Tooltip("한 단계마다의 추가 감소량 (0.1 = 10%)")] private float _slowPerStep;
        [SerializeField, Tooltip("이동 속도 하한 (0.2 = 기본의 20%)")] private float _minSpeedRatio;
        [SerializeField, Tooltip("최대 무게 대비 이 비율을 넘으면 HP가 준다 (1.5 = 150%)")] private float _overloadRatio;
        [SerializeField, Tooltip("과적일 때 초당 HP 감소")] private float _overloadDamagePerSecond;

        [Header("체력")]
        [SerializeField] private float _maxHp;

        [Header("구르기")]
        [SerializeField, Tooltip("칸")] private float _rollDistance;
        [SerializeField, Tooltip("초. 이 시간 동안 구르기 거리를 이동한다")] private float _rollDuration;
        [SerializeField, Tooltip("초")] private float _rollInvincibleDuration;
        [SerializeField, Tooltip("초")] private float _rollCooldown;

        [Header("피격")]
        [SerializeField, Tooltip("초")] private float _hitFlashDuration;
        [SerializeField, Tooltip("초")] private float _hitStunDuration;
        [SerializeField, Tooltip("이 개수를 넘으면 오래된 혈흔부터 지운다")] private int _maxBloodDecals;

        public float MoveSpeed => _moveSpeed;
        public float ReloadMoveSpeedMultiplier => _reloadMoveSpeedMultiplier;
        public float MaxWeight => _maxWeight;
        public float OverloadRatio => _overloadRatio;
        public float OverloadDamagePerSecond => _overloadDamagePerSecond;
        public float MaxHp => _maxHp;

        // 무게에 따른 이동 속도 배율 (기획서 6.4의 표).
        public float WeightSpeedMultiplier(float weight)
        {
            if (_maxWeight <= 0f || _slowStepRatio <= 0f)
            {
                return 1f;
            }

            float ratio = weight / _maxWeight;
            if (ratio < _slowStartRatio)
            {
                return 1f;
            }

            // 부동소수 오차로 경계값(예: 정확히 60%)이 한 단계 아래로 떨어지지 않게 조금 더한다.
            int steps = Mathf.FloorToInt((ratio - _slowStartRatio) / _slowStepRatio + 0.0001f);
            return Mathf.Max(_minSpeedRatio, 1f - _slowAtStart - steps * _slowPerStep);
        }

        public float RollDistance => _rollDistance;
        public float RollDuration => _rollDuration;
        public float RollInvincibleDuration => _rollInvincibleDuration;
        public float RollCooldown => _rollCooldown;
        public float HitFlashDuration => _hitFlashDuration;
        public float HitStunDuration => _hitStunDuration;
        public int MaxBloodDecals => _maxBloodDecals;
    }
}
