using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 대기실에서 "가진 것"을 세고, 쓰고, 받는 곳. 상점·제작·퀘스트 납품·포드 수리가 같은 규칙을 쓴다.
    // 기획서 12.3·14.3: 창고, 가방, 보관함을 모두 세고 창고의 것부터 쓴다. 받은 물건은 창고로 들어간다.
    public class LobbyStock
    {
        private readonly InventoryGrid _storage;
        private readonly PlayerInventory _inventory;

        public LobbyStock(InventoryGrid storage, PlayerInventory inventory)
        {
            _storage = storage;
            _inventory = inventory;
        }

        public InventoryGrid Storage => _storage;

        public PlayerInventory Inventory => _inventory;

        private IEnumerable<InventoryGrid> Grids()
        {
            yield return _storage;
            yield return _inventory.Bag;
            yield return _inventory.SecureBox;
            if (_inventory.DogBag != null)
            {
                yield return _inventory.DogBag;
            }
        }

        public int Count(ItemData item)
        {
            int total = 0;
            foreach (InventoryGrid grid in Grids())
            {
                foreach (ItemStack stack in grid.Items)
                {
                    if (stack.Data == item)
                    {
                        total += stack.Count;
                    }
                }
            }

            return total;
        }

        public bool Has(IReadOnlyList<TechTree.Cost> costs)
        {
            foreach (TechTree.Cost cost in costs)
            {
                if (cost.Item != null && Count(cost.Item) < cost.Count)
                {
                    return false;
                }
            }

            return true;
        }

        public void Consume(IReadOnlyList<TechTree.Cost> costs)
        {
            foreach (TechTree.Cost cost in costs)
            {
                if (cost.Item != null)
                {
                    Consume(cost.Item, cost.Count);
                }
            }
        }

        public void Consume(ItemData item, int amount)
        {
            foreach (InventoryGrid grid in Grids())
            {
                foreach (ItemStack stack in new List<ItemStack>(grid.Items))
                {
                    if (amount <= 0)
                    {
                        return;
                    }

                    if (stack.Data != item)
                    {
                        continue;
                    }

                    int used = Mathf.Min(amount, stack.Count);
                    stack.Count -= used;
                    amount -= used;
                    if (stack.Count <= 0)
                    {
                        grid.Remove(stack);
                    }
                }
            }
        }

        // 창고에 넣는다. 같은 종류 묶음에 먼저 채우고, 남는 것은 빈자리에 새 묶음으로 놓는다.
        // 자리가 없으면 아무것도 바꾸지 않고 false를 돌려준다 (돈이나 재료를 쓰기 전에 확인하려고).
        public bool CanAddToStorage(ItemData item, int count)
        {
            return TryAddToStorage(item, count, false);
        }

        public bool AddToStorage(ItemData item, int count)
        {
            return TryAddToStorage(item, count, true);
        }

        private bool TryAddToStorage(ItemData item, int count, bool apply)
        {
            var topUps = new List<(ItemStack stack, int add)>();
            int left = count;
            if (item.MaxStack > 1)
            {
                foreach (ItemStack stack in _storage.Items)
                {
                    if (left <= 0)
                    {
                        break;
                    }

                    if (stack.Data == item && stack.Count < item.MaxStack)
                    {
                        int add = Mathf.Min(left, item.MaxStack - stack.Count);
                        topUps.Add((stack, add));
                        left -= add;
                    }
                }
            }

            var newStacks = new List<ItemStack>();
            while (left > 0)
            {
                int size = item.MaxStack > 1 ? Mathf.Min(left, item.MaxStack) : 1;
                var stack = new ItemStack(item, size);
                if (!_storage.TryAdd(stack))
                {
                    foreach (ItemStack placed in newStacks)
                    {
                        _storage.Remove(placed);
                    }

                    return false;
                }

                newStacks.Add(stack);
                left -= size;
            }

            if (!apply)
            {
                foreach (ItemStack placed in newStacks)
                {
                    _storage.Remove(placed);
                }

                return true;
            }

            foreach ((ItemStack stack, int add) in topUps)
            {
                stack.Count += add;
            }

            return true;
        }
    }
}
