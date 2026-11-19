using UnityEngine;

namespace EscapeFromSpace
{
    // 더블클릭으로 옮기기 (2026-11-19 사용자 요청): 컨테이너·창고·잔해의 아이템은 가방으로,
    // 가방·안전 상자의 아이템은 열려 있는 컨테이너·창고로 보낸다. 같은 묶음이 있으면 먼저 채우고, 빈자리를 찾아 놓는다.
    public partial class InventoryUI
    {
        private const float DoubleClickTime = 0.3f;

        private ItemStack _lastClicked;
        private float _lastClickTime = -1f;

        // 짧은 사이에 같은 아이템을 두 번 누르면 집어 들지 않고 옮긴다.
        private bool TryQuickMove(DropTarget target)
        {
            if (target.Kind != TargetKind.Grid)
            {
                _lastClicked = null;
                return false;
            }

            ItemStack stack = target.View.Grid.GetAt(target.X, target.Y);
            bool isDouble = stack != null && stack == _lastClicked && Time.unscaledTime - _lastClickTime <= DoubleClickTime;
            _lastClicked = stack;
            _lastClickTime = Time.unscaledTime;
            if (!isDouble)
            {
                return false;
            }

            _lastClicked = null;
            InventoryGrid destination = target.View == _lootView ? _inventory.Bag : _lootView.Grid;
            if (destination == null || destination == target.View.Grid)
            {
                // 옮길 곳이 없는 더블클릭은 보통 클릭처럼 집어 든다.
                return false;
            }

            if (MoveInto(stack, target.View.Grid, destination))
            {
                Sfx.Play(SfxId.ItemPlace);
            }
            else
            {
                Sfx.Play(SfxId.ItemRefuse);
            }

            _inventory.NotifyChanged();
            _dirty = true;
            return true;
        }

        // 다 옮기면 true. 일부만 같은 묶음에 합쳐졌으면 남은 것은 원래 자리에 그대로 있다.
        private static bool MoveInto(ItemStack stack, InventoryGrid from, InventoryGrid to)
        {
            if (stack.IsStackable)
            {
                foreach (ItemStack other in to.Items)
                {
                    if (other.Data == stack.Data && other.Count < other.Data.MaxStack)
                    {
                        int moved = Mathf.Min(other.Data.MaxStack - other.Count, stack.Count);
                        other.Count += moved;
                        stack.Count -= moved;
                        if (stack.Count <= 0)
                        {
                            from.Remove(stack);
                            return true;
                        }
                    }
                }
            }

            (int x, int y) = from.PositionOf(stack);
            bool rotated = stack.Rotated;
            from.Remove(stack);
            if (to.TryAdd(stack))
            {
                return true;
            }

            stack.Rotated = rotated;
            from.Place(stack, x, y);
            return false;
        }
    }
}
