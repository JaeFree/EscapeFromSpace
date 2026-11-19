using System;
using UnityEngine;

namespace EscapeFromSpace
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private CombatConfig _config;

        // 플레이어는 장착한 헬멧과 방탄복의 합을 PlayerInventory가 넣어 준다. 로봇은 0이다.
        [SerializeField] private int _armor;

        // 장비와 상관없이 더해지는 방어력 (플레이어의 강화). 장비를 바꿔도 남아야 해서 따로 둔다.
        private int _bonusArmor;

        // HUD가 생기기 전까지 Inspector에서 현재 HP를 보려고 노출한다.
        [SerializeField] private float _currentHp;

        // 은신 수치(발견 게이지, 엄폐)도 이 설정에 있다. 로봇과 플레이어가 같은 에셋을 쓴다.
        public CombatConfig Config => _config;

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

        // 질식이나 과적처럼 몸 안에서 깎이는 피해. 방어력과 구르기 무적을 무시하고,
        // 피격 효과(번쩍임, 혈흔)와 피격으로 취소되는 것들(루팅)을 건드리지 않는다.
        public void TakeRawDamage(float damage)
        {
            if (IsDead)
            {
                return;
            }

            _currentHp = Mathf.Max(0f, _currentHp - damage);
            if (_currentHp <= 0f)
            {
                IsDead = true;
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsDead)
            {
                _currentHp = Mathf.Min(MaxHp, _currentHp + amount);
            }
        }

        public void SetArmor(int armor)
        {
            _armor = armor;
        }

        public void SetBonusArmor(int armor)
        {
            _bonusArmor = armor;
        }

        // 최대 HP를 바꾼다 (플레이어의 강화). 늘어난 만큼 현재 HP도 같이 늘어난다.
        public void SetMaxHp(float maxHp)
        {
            if (!IsDead)
            {
                _currentHp = Mathf.Clamp(_currentHp + maxHp - MaxHp, 1f, maxHp);
            }

            MaxHp = maxHp;
        }

        public void TakeDamage(float attackDamage, Vector2 hitPoint)
        {
            if (IsDead || IsInvincible)
            {
                return;
            }

            float damage = DamageCalculator.Calculate(attackDamage, _armor + _bonusArmor, _config);
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
