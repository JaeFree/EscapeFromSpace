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

        // 대기실을 거치지 않고 레이드 씬을 바로 Play할 때만 쓰는 테스트용 장비.
        // 대기실에서 들어오면 거기서 고른 것을 Restore로 받는다.
        [Header("씬을 바로 Play할 때의 장비 (테스트용)")]
        [SerializeField] private ItemData _startingWeapon;
        [SerializeField] private StartingItem[] _startingItems;

        private readonly ItemStack[] _equipped = new ItemStack[3];
        private bool _restored;

        public InventoryGrid Bag { get; private set; }

        // 여기 넣은 아이템은 죽어도 잃지 않는다.
        public InventoryGrid SecureBox { get; private set; }

        // 로봇 개의 가방 (기획서 19.5). 개가 없으면 null. 무게에 들지 않고 죽어도 남는다.
        public InventoryGrid DogBag { get; private set; }

        public bool HasDog => DogBag != null;

        [SerializeField, Tooltip("로봇 개 그림 (위에서 본 모습, 앞이 아래)")] private Sprite _dogSprite;

        // 개 가방의 크기. 최대 3칸이고 늘지 않는다 (2026-11-19 사용자 결정: 죽었을 때 손실 부담이 남아야 한다).
        private const int DogBagWidth = 3;
        private const int DogBagHeight = 1;

        // 무엇이든 바뀌면 알린다. 화면이 다시 그리는 데 쓴다.
        public event Action Changed;

        private void Awake()
        {
            EnsureCreated();
            if (_restored)
            {
                return;
            }

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

        // 저장해 둔 장비로 채운다. 다른 오브젝트의 Awake에서 부르므로 이쪽 Awake보다 먼저일 수 있다.
        public void Restore(SaveData data, ItemDatabase items)
        {
            // 이쪽 Awake가 먼저 돌았으면 테스트용 장비가 이미 들어 있다. 비우고 저장된 것만 넣는다.
            Bag = new InventoryGrid(BagWidth, BaseBagHeight);
            SecureBox = new InventoryGrid(Mathf.Max(1, data.SecureWidth), Mathf.Max(1, data.SecureHeight));
            DogBag = data.HasDog ? new InventoryGrid(DogBagWidth, DogBagHeight) : null;
            Array.Clear(_equipped, 0, _equipped.Length);
            _restored = true;

            foreach (SavedItem saved in data.Equipped)
            {
                ItemStack stack = SaveConverter.ToStack(saved, items);
                if (stack != null && saved.X >= 0 && saved.X < _equipped.Length && Accepts((EquipSlot)saved.X, stack))
                {
                    _equipped[saved.X] = stack;
                }
            }

            // 방탄복의 추가 줄이 먼저 생겨야 그 줄에 있던 아이템이 제자리에 들어간다.
            ApplyEquipment();
            SaveConverter.Fill(Bag, data.Bag, items);
            SaveConverter.Fill(SecureBox, data.Secure, items);
            if (DogBag != null)
            {
                SaveConverter.Fill(DogBag, data.DogBag, items);
                RobotDog.Ensure(transform, _dogSprite);
            }

            NotifyChanged();
        }

        // 퀘스트 보상으로 보관함을 키운다. 들어 있던 것은 새 칸에 다시 놓는다.
        public void ResizeSecureBox(int width, int height)
        {
            if (width <= SecureBox.Width && height <= SecureBox.Height)
            {
                return;
            }

            var bigger = new InventoryGrid(Mathf.Max(width, SecureBox.Width), Mathf.Max(height, SecureBox.Height));
            foreach (ItemStack stack in new System.Collections.Generic.List<ItemStack>(SecureBox.Items))
            {
                (int x, int y) = SecureBox.PositionOf(stack);
                SecureBox.Remove(stack);
                if (bigger.CanPlace(stack, x, y))
                {
                    bigger.Place(stack, x, y);
                }
                else
                {
                    bigger.TryAdd(stack);
                }
            }

            SecureBox = bigger;
            NotifyChanged();
        }

        // 로봇 개가 합류한다 (퀘스트 21).
        public void EnableDog()
        {
            if (DogBag == null)
            {
                DogBag = new InventoryGrid(DogBagWidth, DogBagHeight);
                RobotDog.Ensure(transform, _dogSprite);
                NotifyChanged();
            }
        }

        // 지금 가진 것을 저장 내용에 담는다.
        public void Capture(SaveData data)
        {
            data.Equipped.Clear();
            for (int i = 0; i < _equipped.Length; i++)
            {
                if (_equipped[i] != null)
                {
                    data.Equipped.Add(SaveConverter.ToSaved(_equipped[i], i, 0));
                }
            }

            SaveConverter.Capture(Bag, data.Bag);
            SaveConverter.Capture(SecureBox, data.Secure);
            data.SecureWidth = SecureBox.Width;
            data.SecureHeight = SecureBox.Height;
            data.HasDog = DogBag != null;
            if (DogBag != null)
            {
                SaveConverter.Capture(DogBag, data.DogBag);
            }
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

        // 장착한 것, 가방, 보관함을 모두 더한 무게 (기획서 6.4).
        public float TotalWeight
        {
            get
            {
                float total = WeightIn(Bag) + WeightIn(SecureBox);
                foreach (ItemStack stack in _equipped)
                {
                    total += WeightOf(stack);
                }

                return total;
            }
        }

        // 가진 것의 가치 합계. 죽으면 보관함과 개 가방은 남으므로 잃은 가치를 셀 때는 뺀다.
        public int TotalValue(bool includeSecureBox)
        {
            float total = ValueIn(Bag) + (includeSecureBox ? ValueIn(SecureBox) + (DogBag != null ? ValueIn(DogBag) : 0f) : 0f);
            foreach (ItemStack stack in _equipped)
            {
                total += ValueOf(stack);
            }

            return Mathf.RoundToInt(total);
        }

        // 가방에 넣는다. 같은 종류 묶음에 먼저 채우고 남는 것은 빈자리에 놓는다. 다 못 넣으면 아무것도 바꾸지 않는다.
        public bool TryAddToBag(ItemData item, int count)
        {
            var topUps = new System.Collections.Generic.List<(ItemStack stack, int add)>();
            int left = count;
            foreach (ItemStack stack in Bag.Items)
            {
                if (left > 0 && stack.Data == item && stack.Count < item.MaxStack)
                {
                    int add = Mathf.Min(left, item.MaxStack - stack.Count);
                    topUps.Add((stack, add));
                    left -= add;
                }
            }

            ItemStack rest = null;
            if (left > 0)
            {
                rest = new ItemStack(item, left);
                if (!Bag.TryAdd(rest))
                {
                    return false;
                }
            }

            foreach ((ItemStack stack, int add) in topUps)
            {
                stack.Count += add;
            }

            NotifyChanged();
            return true;
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

        private void EnsureCreated()
        {
            if (Bag == null)
            {
                Bag = new InventoryGrid(BagWidth, BaseBagHeight);
                SecureBox = new InventoryGrid(1, 1);
            }
        }

        // 장착한 헬멧과 방탄복을 방어력과 가방 줄 수에 반영한다. 헬멧의 산소는 PlayerOxygen이 읽는다.
        private void ApplyEquipment()
        {
            ItemStack helmet = GetEquipped(EquipSlot.Helmet);
            ItemStack vest = GetEquipped(EquipSlot.Vest);

            int armor = (helmet != null ? helmet.Data.Armor : 0) + (vest != null ? vest.Data.Armor : 0);
            _health.SetArmor(armor);

            Bag.SetHeight(BaseBagHeight + (vest != null ? vest.Data.ExtraBagRows : 0));
        }

        // 탄약의 무게와 가치는 기본 수량 한 묶음 기준이라 발 수에 비례한다. 탄창에 든 탄은 세지 않는다.
        private static float WeightOf(ItemStack stack)
        {
            return stack == null ? 0f : stack.Data.Weight * StackScale(stack) + AttachmentSum(stack, a => a.Weight);
        }

        public static float ValueOf(ItemStack stack)
        {
            return stack == null ? 0f : stack.Data.Value * StackScale(stack) + AttachmentSum(stack, a => a.Value);
        }

        // 무기에 단 부착물도 무게와 가치에 들어간다 (기획서 17장).
        private static float AttachmentSum(ItemStack stack, System.Func<ItemData, float> value)
        {
            float total = 0f;
            if (stack.Attachments != null)
            {
                foreach (ItemData attachment in stack.Attachments)
                {
                    if (attachment != null)
                    {
                        total += value(attachment);
                    }
                }
            }

            return total;
        }

        private static float StackScale(ItemStack stack)
        {
            return stack.IsStackable ? stack.Count / (float)stack.Data.DefaultCount : 1f;
        }

        private static float WeightIn(InventoryGrid grid)
        {
            float total = 0f;
            foreach (ItemStack item in grid.Items)
            {
                total += WeightOf(item);
            }

            return total;
        }

        public static float ValueIn(InventoryGrid grid)
        {
            float total = 0f;
            foreach (ItemStack item in grid.Items)
            {
                total += ValueOf(item);
            }

            return total;
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
