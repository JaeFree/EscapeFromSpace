using System;
using System.Collections.Generic;

namespace EscapeFromSpace
{
    // JsonUtility는 필드만 저장하므로 저장 파일의 모양을 그대로 필드로 둔다.
    [Serializable]
    public class SavedItem
    {
        public string Id;
        public int Count;
        public int AmmoInMagazine;
        public bool Rotated;

        // 격자 안의 칸. 장비 칸에서는 X가 칸 번호(헬멧 0, 방탄복 1, 무기 2)다.
        public int X;
        public int Y;
    }

    // 저장 파일 하나의 내용 (기획서 7장 대기실 창고, 9.2 전체 맵).
    [Serializable]
    public class SaveData
    {
        // 출격할 때 켜고 레이드가 끝나면 끈다. 켜진 채로 대기실에 들어오면 레이드 중에 게임을 끈 것이다.
        public bool InRaid;

        public List<SavedItem> Storage = new List<SavedItem>();
        public List<SavedItem> Equipped = new List<SavedItem>();
        public List<SavedItem> Bag = new List<SavedItem>();
        public List<SavedItem> Secure = new List<SavedItem>();

        // 전체 맵에서 가 본 칸. 칸마다 '1'(가 봄) 또는 '0'을 줄 순서대로 이어 붙인다.
        public int MapWidth;
        public int MapHeight;
        public string Explored = string.Empty;
    }

    // 실행 중의 아이템(ItemStack, InventoryGrid)과 저장 파일의 모양을 서로 바꾼다.
    public static class SaveConverter
    {
        public static SavedItem ToSaved(ItemStack stack, int x, int y)
        {
            return new SavedItem
            {
                Id = stack.Data.name,
                Count = stack.Count,
                AmmoInMagazine = stack.AmmoInMagazine,
                Rotated = stack.Rotated,
                X = x,
                Y = y,
            };
        }

        public static ItemStack ToStack(SavedItem saved, ItemDatabase items)
        {
            ItemData data = items.Find(saved.Id);
            if (data == null)
            {
                return null;
            }

            return new ItemStack(data, saved.Count)
            {
                AmmoInMagazine = saved.AmmoInMagazine,
                Rotated = saved.Rotated,
            };
        }

        public static void Capture(InventoryGrid grid, List<SavedItem> target)
        {
            target.Clear();
            foreach (ItemStack stack in grid.Items)
            {
                (int x, int y) = grid.PositionOf(stack);
                target.Add(ToSaved(stack, x, y));
            }
        }

        public static void Fill(InventoryGrid grid, List<SavedItem> source, ItemDatabase items)
        {
            foreach (SavedItem saved in source)
            {
                ItemStack stack = ToStack(saved, items);
                if (stack == null)
                {
                    continue;
                }

                // 저장한 뒤에 칸 크기가 바뀌었으면 원래 자리에 안 들어갈 수 있다. 그때는 빈자리를 찾는다.
                if (grid.CanPlace(stack, saved.X, saved.Y))
                {
                    grid.Place(stack, saved.X, saved.Y);
                }
                else
                {
                    grid.TryAdd(stack);
                }
            }
        }
    }
}
