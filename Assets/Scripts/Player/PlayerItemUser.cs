using UnityEngine;

namespace EscapeFromSpace
{
    // 도구칸 1, 2, 3 (기획서 9.3, 9.4). 가방과 보관함에 있는 의약품과 산소 캔을 키로 쓴다.
    public class PlayerItemUser : MonoBehaviour
    {
        public const int SlotCount = 3;

        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerOxygen _oxygen;
        [SerializeField] private Health _health;
        [SerializeField, Tooltip("도구칸 1, 2, 3에 놓는 아이템")] private ItemData[] _slots = new ItemData[SlotCount];

        private ItemData _using;
        private float _useTimeLeft;

        // 지금 쓰고 있는 아이템. 없으면 null.
        public ItemData Using => _using;
        public float UseProgress => _using != null && _using.UseTime > 0f ? 1f - _useTimeLeft / _using.UseTime : 0f;
        public float UseTimeLeft => _using != null ? Mathf.Max(0f, _useTimeLeft) : 0f;

        public ItemData GetSlot(int index)
        {
            return index < _slots.Length ? _slots[index] : null;
        }

        public int CountInSlot(int index)
        {
            return _inventory.CountAmmo(GetSlot(index));
        }

        private void Update()
        {
            if (_controller.IsDead)
            {
                _using = null;
                return;
            }

            if (_using != null)
            {
                UpdateUse();
                return;
            }

            for (int i = 0; i < SlotCount; i++)
            {
                if (_input.ToolPressedThisFrame(i))
                {
                    TryUse(GetSlot(i));
                    break;
                }
            }
        }

        private void TryUse(ItemData item)
        {
            if (item == null || _inventory.CountAmmo(item) <= 0)
            {
                return;
            }

            // HP가 가득이면 의약품을 낭비하지 않게 막는다.
            if (item.HealAmount > 0f && _health.CurrentHp >= _health.MaxHp)
            {
                return;
            }

            if (item.UseTime <= 0f)
            {
                Apply(item);
                return;
            }

            _using = item;
            _useTimeLeft = item.UseTime;
        }

        private void UpdateUse()
        {
            // 쓰는 중에 사격하거나 구르면 취소된다. 맞는 것으로는 취소되지 않는다.
            bool firing = _input.FireHeld && !_controller.IsBagOpen;
            if (firing || _roll.IsRolling || _inventory.CountAmmo(_using) <= 0)
            {
                _using = null;
                return;
            }

            _useTimeLeft -= Time.deltaTime;
            if (_useTimeLeft <= 0f)
            {
                ItemData item = _using;
                _using = null;
                Apply(item);
            }
        }

        private void Apply(ItemData item)
        {
            if (item.HealAmount > 0f)
            {
                _health.Heal(item.HealAmount);
            }

            if (item.OxygenSeconds > 0f)
            {
                _oxygen.Add(item.OxygenSeconds);
            }

            _inventory.ConsumeAmmo(item, 1);
        }
    }
}
