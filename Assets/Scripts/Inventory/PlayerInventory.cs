using System;
using UnityEngine;

namespace EscapeFromSpace
{
    public enum EquipSlot
    {
        Helmet,
        Vest,
        Weapon,
    }

    // 플레이어가 가진 것 전부: 장비 칸 3개, 가방, 보관함 (기획서 6.1).
    public class PlayerInventory : MonoBehaviour
    {
        private const int BagWidth = 3;
        private const int BaseBagHeight = 4;

        [Serializable]
        private class StartingItem
        {
            [SerializeField] private ItemData _item;
            [SerializeField] private int _count = 1;

            public ItemData Item => _item;
            public int Count => _count;
        }

        [SerializeField] private Health _health;

        // 대기실이 생기기 전까지 쓰는 임시 시작 장비. 대기실을 만들면 거기서 고른 것을 들고 들어오게 바꾼다.
        [Header("시작 장비 (임시)")]
        [SerializeField] private ItemData _startingWeapon;
        [SerializeField] private StartingItem[] _startingItems;

        private readonly ItemStack[] _equipped = new ItemStack[3];

        public InventoryGrid Bag { get; private set; }

        // 여기 넣은 아이템은 죽어도 잃지 않는다 (사망 처리는 11/18).
        public InventoryGrid SecureBox { get; private set; }

        // 무엇이든 바뀌면 알린다. 화면이 다시 그리는 데 쓴다.
        public event Action Changed;

        private void Awake()
        {
            Bag = new InventoryGrid(BagWidth, BaseBagHeight);
            SecureBox = new InventoryGrid(1, 1);

            if (_startingWeapon != null)
            {
                _equipped[(int)EquipSlot.Weapon] = new ItemStack(_startingWeapon);
            }

            if (_startingItems != null)
            {
                foreach (StartingItem starting in _startingItems)
                {
                    if (starting.Item != null)
                    {
                        Bag.TryAdd(new ItemStack(starting.Item, starting.Count));
                    }
                }
            }
        }

        private void Start()
        {
            ApplyEquipment();
        }

        public ItemStack GetEquipped(EquipSlot slot)
        {
            return _equipped[(int)slot];
        }

        // 그 칸에 들어갈 수 있는 종류인지
        public static bool Accepts(EquipSlot slot, ItemStack stack)
        {
            switch (slot)
            {
                case EquipSlot.Helmet:
                    return stack.Data.Category == ItemCategory.Helmet;
                case EquipSlot.Vest:
                    return stack.Data.Category == ItemCategory.Vest;
                default:
                    return stack.Data.Category == ItemCategory.Weapon;
            }
        }

        // 방탄복을 벗거나 바꿀 때, 사라질 추가 줄에 아이템이 있으면 안 된다. null은 벗는 경우.
        public bool CanSetVest(ItemStack vest)
        {
            int rows = vest != null ? vest.Data.ExtraBagRows : 0;
            return Bag.CanSetHeight(BaseBagHeight + rows);
        }

        public void SetEquipped(EquipSlot slot, ItemStack stack)
        {
            _equipped[(int)slot] = stack;
            ApplyEquipment();
            NotifyChanged();
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        // 가방과 보관함에 있는 그 탄약의 합계. 재장전의 예비탄으로 쓴다.
        public int CountAmmo(ItemData ammo)
        {
            if (ammo == null)
            {
                return 0;
            }

            return CountIn(Bag, ammo) + CountIn(SecureBox, ammo);
        }

        // 발 수가 적은 묶음부터 쓴다. 다 쓴 묶음은 칸에서 사라진다.
        public void ConsumeAmmo(ItemData ammo, int amount)
        {
            while (amount > 0)
            {
                InventoryGrid grid = Bag;
                ItemStack smallest = FindSmallest(Bag, ammo);
                ItemStack inSecureBox = FindSmallest(SecureBox, ammo);
                if (smallest == null || (inSecureBox != null && inSecureBox.Count < smallest.Count))
                {
                    smallest = inSecureBox;
                    grid = SecureBox;
                }

                if (smallest == null)
                {
                    break;
                }

                int used = Mathf.Min(amount, smallest.Count);
                smallest.Count -= used;
                amount -= used;
                if (smallest.Count <= 0)
                {
                    grid.Remove(smallest);
                }
            }

            NotifyChanged();
        }

        // 장착한 헬멧과 방탄복을 방어력과 가방 줄 수에 반영한다. 헬멧의 산소는 산소 시스템(11/18)에서 읽는다.
        private void ApplyEquipment()
        {
            ItemStack helmet = GetEquipped(EquipSlot.Helmet);
            ItemStack vest = GetEquipped(EquipSlot.Vest);

            int armor = (helmet != null ? helmet.Data.Armor : 0) + (vest != null ? vest.Data.Armor : 0);
            _health.SetArmor(armor);

            Bag.SetHeight(BaseBagHeight + (vest != null ? vest.Data.ExtraBagRows : 0));
        }

        private static int CountIn(InventoryGrid grid, ItemData data)
        {
            int total = 0;
            foreach (ItemStack item in grid.Items)
            {
                if (item.Data == data)
                {
                    total += item.Count;
                }
            }

            return total;
        }

        private static ItemStack FindSmallest(InventoryGrid grid, ItemData data)
        {
            ItemStack smallest = null;
            foreach (ItemStack item in grid.Items)
            {
                if (item.Data == data && (smallest == null || item.Count < smallest.Count))
                {
                    smallest = item;
                }
            }

            return smallest;
        }
    }
}
