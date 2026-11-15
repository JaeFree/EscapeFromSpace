using UnityEngine;

namespace EscapeFromSpace
{
    // 기획서의 플레이어 수치는 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    [CreateAssetMenu(fileName = "PlayerData", menuName = "EscapeFromSpace/Player Data")]
    public class PlayerData : ScriptableObject
    {
        [Header("이동")]
        [SerializeField, Tooltip("칸/초")] private float _moveSpeed;

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
        public float MaxHp => _maxHp;
        public float RollDistance => _rollDistance;
        public float RollDuration => _rollDuration;
        public float RollInvincibleDuration => _rollInvincibleDuration;
        public float RollCooldown => _rollCooldown;
        public float HitFlashDuration => _hitFlashDuration;
        public float HitStunDuration => _hitStunDuration;
        public int MaxBloodDecals => _maxBloodDecals;
    }
}
