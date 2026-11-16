using UnityEngine;

namespace EscapeFromSpace
{
    // 우주복 산소 (기획서 7장). 레이드 중 계속 줄고, 0이 되면 질식 피해를 입는다.
    public class PlayerOxygen : MonoBehaviour
    {
        [SerializeField] private RaidConfig _config;
        [SerializeField] private Health _health;
        [SerializeField] private PlayerInventory _inventory;

        private float _helmetBonus;

        // 남은 산소(초)
        public float Current { get; private set; }
        public float Max => _config.BaseOxygenSeconds + _helmetBonus;
        public bool IsSuffocating => Current <= 0f;
        public bool IsLow => Current <= _config.OxygenWarningSeconds;

        // 대기실에서는 줄지 않는다. 헬멧을 바꿔 끼면 늘어난 양은 그대로 보인다.
        public bool IsDraining { get; set; } = true;

        private void OnEnable()
        {
            _inventory.Changed += OnInventoryChanged;
        }

        private void OnDisable()
        {
            _inventory.Changed -= OnInventoryChanged;
        }

        // 가방은 PlayerInventory의 Awake에서 채워진다. 그 뒤에 읽으려고 Start에서 한다.
        private void Start()
        {
            _helmetBonus = HelmetBonus();
            Current = Max;
        }

        private void Update()
        {
            if (_health.IsDead || !IsDraining)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - Time.deltaTime);
            if (Current <= 0f)
            {
                _health.TakeRawDamage(_config.SuffocationDamagePerSecond * Time.deltaTime);
            }
        }

        // 산소 캔. 조금이라도 차면 질식은 바로 멈춘다.
        public void Add(float seconds)
        {
            Current = Mathf.Min(Max, Current + seconds);
        }

        // 헬멧을 입으면 그 헬멧의 추가분만큼 늘고, 벗으면 그만큼 준다.
        private void OnInventoryChanged()
        {
            float bonus = HelmetBonus();
            if (Mathf.Approximately(bonus, _helmetBonus))
            {
                return;
            }

            Current = Mathf.Max(0f, Current + bonus - _helmetBonus);
            _helmetBonus = bonus;
        }

        private float HelmetBonus()
        {
            ItemStack helmet = _inventory.GetEquipped(EquipSlot.Helmet);
            return helmet != null ? helmet.Data.ExtraOxygenSeconds : 0f;
        }
    }
}
