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

        // 무기에 단 부착물의 아이템 이름 (부위 순서, 빈 부위는 빈 글자). 탄창에 든 탄 종류 (빈 글자면 기본 탄약).
        public List<string> Attachments = new List<string>();
        public string LoadedAmmo = string.Empty;
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

        // 레벨과 테크 트리 (기획서 12장). 죽어도 남는다. Xp는 지금 레벨에서 쌓은 경험치다.
        public int Level = 1;
        public int Xp;
        public int TechPoints;
        public List<string> UnlockedTech = new List<string>();

        // 상인과 퀘스트 (기획서 14·16장). 죽어도 남는다.
        public int Credits;
        public List<string> CompletedQuests = new List<string>();

        // 진행 중인 퀘스트의 센 횟수 (QuestIds[i]의 횟수가 QuestCounts[i]). JsonUtility가 사전을 저장하지 못해 두 목록으로 둔다.
        public List<string> QuestIds = new List<string>();
        public List<int> QuestCounts = new List<int>();

        // 한 번이라도 부순 네임드의 Id (네임드 모두 처치 퀘스트)
        public List<string> KilledNamed = new List<string>();

        // 장거리 탈출 포드에 설치한 부품 (한 개마다 한 줄, 기획서 14.5)
        public List<string> InstalledParts = new List<string>();

        public bool SeenPrologue;
        public bool SeenEnding;
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
                Attachments = SaveAttachments(stack),
                LoadedAmmo = stack.LoadedAmmo != null ? stack.LoadedAmmo.name : string.Empty,
            };
        }

        private static List<string> SaveAttachments(ItemStack stack)
        {
            var list = new List<string>();
            if (stack.Attachments != null)
            {
                foreach (ItemData attachment in stack.Attachments)
                {
                    list.Add(attachment != null ? attachment.name : string.Empty);
                }
            }

            return list;
        }

        private static void LoadAttachments(ItemStack stack, SavedItem saved, ItemDatabase items)
        {
            if (!string.IsNullOrEmpty(saved.LoadedAmmo))
            {
                stack.LoadedAmmo = items.Find(saved.LoadedAmmo);
            }

            if (stack.Attachments == null || saved.Attachments == null)
            {
                return;
            }

            for (int i = 0; i < saved.Attachments.Count && i < stack.Attachments.Length; i++)
            {
                if (!string.IsNullOrEmpty(saved.Attachments[i]))
                {
                    stack.Attachments[i] = items.Find(saved.Attachments[i]);
                }
            }
        }

        public static ItemStack ToStack(SavedItem saved, ItemDatabase items)
        {
            ItemData data = items.Find(saved.Id);
            if (data == null)
            {
                return null;
            }

            var stack = new ItemStack(data, saved.Count)
            {
                AmmoInMagazine = saved.AmmoInMagazine,
                Rotated = saved.Rotated,
            };
            LoadAttachments(stack, saved, items);
            return stack;
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
