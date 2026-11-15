using UnityEngine;

namespace EscapeFromSpace
{
    public static class DamageCalculator
    {
        // 받는 피해 = 공격 피해 × (1 − 방어력 × 계수).
        // 고정값을 빼는 방식은 약한 공격이 0이 되므로 쓰지 않는다 (기획서 4.1).
        public static float Calculate(float attackDamage, int armor, CombatConfig config)
        {
            int effectiveArmor = Mathf.Clamp(armor, 0, config.MaxArmor);
            return attackDamage * (1f - effectiveArmor * config.ArmorReductionPerPoint);
        }
    }
}
