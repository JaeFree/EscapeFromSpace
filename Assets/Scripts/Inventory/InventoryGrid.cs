using System.Collections.Generic;

namespace EscapeFromSpace
{
    // 칸 배치 판정 (기획서 6.1). 가방, 보관함, 루팅 칸이 모두 이것을 쓴다.
    // 칸 좌표는 왼쪽 위가 (0, 0)이고 아래로 갈수록 y가 커진다. 방탄복의 추가 줄이 아래에 붙기 때문이다.
    public class InventoryGrid
    {
        private readonly List<ItemStack> _items = new List<ItemStack>();
        private readonly Dictionary<ItemStack, (int x, int y)> _positions = new Dictionary<ItemStack, (int x, int y)>();

        public InventoryGrid(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; private set; }
        public IReadOnlyList<ItemStack> Items => _items;

        public bool Contains(ItemStack stack)
        {
            return _positions.ContainsKey(stack);
        }

        public (int x, int y) PositionOf(ItemStack stack)
        {
            return _positions[stack];
        }

        public ItemStack GetAt(int x, int y)
        {
            foreach (ItemStack item in _items)
            {
                (int ix, int iy) = _positions[item];
                if (x >= ix && x < ix + item.Width && y >= iy && y < iy + item.Height)
                {
                    return item;
                }
            }

            return null;
        }

        // 밖으로 나가거나 다른 아이템과 겹치면 놓을 수 없다.
        public bool CanPlace(ItemStack stack, int x, int y)
        {
            if (x < 0 || y < 0 || x + stack.Width > Width || y + stack.Height > Height)
            {
                return false;
            }

            foreach (ItemStack item in _items)
            {
                if (item == stack)
                {
                    continue;
                }

                (int ix, int iy) = _positions[item];
                bool apart = x + stack.Width <= ix || ix + item.Width <= x || y + stack.Height <= iy || iy + item.Height <= y;
                if (!apart)
                {
                    return false;
                }
            }

            return true;
        }

        public void Place(ItemStack stack, int x, int y)
        {
            if (!_positions.ContainsKey(stack))
            {
                _items.Add(stack);
            }

            _positions[stack] = (x, y);
        }

        public void Remove(ItemStack stack)
        {
            if (_positions.Remove(stack))
            {
                _items.Remove(stack);
            }
        }

        // 위에서부터 훑어 처음 들어가는 자리에 놓는다. 그대로 안 들어가면 돌려서도 본다.
        public bool TryAdd(ItemStack stack)
        {
            for (int turn = 0; turn < 2; turn++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (CanPlace(stack, x, y))
                        {
                            Place(stack, x, y);
                            return true;
                        }
                    }
                }

                if (stack.Data.Width == stack.Data.Height)
                {
                    break;
                }

                stack.Rotated = !stack.Rotated;
            }

            return false;
        }

        // 줄 수를 바꾼다 (방탄복의 추가 줄). 사라질 줄에 아이템이 걸쳐 있으면 줄일 수 없다.
        public bool CanSetHeight(int height)
        {
            foreach (ItemStack item in _items)
            {
                if (_positions[item].y + item.Height > height)
                {
                    return false;
                }
            }

            return true;
        }

        public void SetHeight(int height)
        {
            Height = height;
        }
    }
}
