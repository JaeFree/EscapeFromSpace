using System;
using UnityEngine;

namespace EscapeFromSpace
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private CombatConfig _config;

        // 방어구가 생기면(11/17) 장착한 장비에서 받아 온다. 그때까지는 공식을 확인하려고 직접 넣는다.
        [SerializeField] private int _armor;

        // HUD가 생기기 전까지 Inspector에서 현재 HP를 보려고 노출한다.
        [SerializeField] private float _currentHp;

        public float MaxHp { get; private set; }
        public float CurrentHp => _currentHp;
        public bool IsDead { get; private set; }
        public bool IsInvincible { get; set; }

        // 실제로 들어간 피해와 맞은 위치
        public event Action<float, Vector2> Damaged;
        public event Action Died;

        // 최대 HP는 대상마다 출처가 달라서(플레이어는 PlayerData) 주인이 넘겨준다.
        public void Initialize(float maxHp)
        {
            MaxHp = maxHp;
            _currentHp = maxHp;
            IsDead = false;
        }

        public void TakeDamage(float attackDamage, Vector2 hitPoint)
        {
            if (IsDead || IsInvincible)
            {
                return;
            }

            float damage = DamageCalculator.Calculate(attackDamage, _armor, _config);
            _currentHp = Mathf.Max(0f, _currentHp - damage);
            Damaged?.Invoke(damage, hitPoint);

            if (_currentHp <= 0f)
            {
                IsDead = true;
                Died?.Invoke();
            }
        }
    }
}
