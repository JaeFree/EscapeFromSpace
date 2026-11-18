using UnityEngine;

namespace EscapeFromSpace
{
    // 가방이나 루팅 칸에 놓인 아이템 하나 (탄약은 한 묶음).
    public class ItemStack
    {
        public ItemStack(ItemData data, int count = -1)
        {
            Data = data;
            Count = count > 0 ? count : data.DefaultCount;

            // 루팅하거나 처음 받은 무기는 탄창이 가득 찬 상태다.
            if (data.Weapon != null)
            {
                AmmoInMagazine = data.Weapon.MagazineSize;
                if (data.Weapon.AcceptsAttachments)
                {
                    Attachments = new ItemData[System.Enum.GetValues(typeof(AttachmentSlot)).Length];
                }
            }
        }

        // 부위별로 단 부착물 (기획서 17장). 부착물을 달 수 없는 무기와 다른 아이템은 null이다.
        public ItemData[] Attachments { get; }

        // 탄창에 든 탄의 종류. null이면 무기의 기본 탄약이다 (고출력 탄약, 기획서 16장).
        public ItemData LoadedAmmo { get; set; }

        // 부착물을 모두 곱한 값. 기획서 17장: 반동과 퍼짐은 기본값의 30% 아래로 줄지 않는다.
        public float RecoilMultiplier => Mathf.Max(0.3f, Product(a => a.RecoilMultiplier));
        public float SpreadMultiplier => Mathf.Max(0.3f, Product(a => a.SpreadMultiplier));
        public float AimSpreadMultiplier => Mathf.Max(0.3f, Product(a => a.AimSpreadMultiplier));

        // 이동 중 퍼짐 배율은 가장 낮은 것 하나만 쓴다 (손잡이는 한 부위뿐이다).
        public float MoveSpreadScale
        {
            get
            {
                float scale = 2f;
                ForEachAttachment(a =>
                {
                    if (a.MoveSpreadScale > 0f)
                    {
                        scale = Mathf.Min(scale, a.MoveSpreadScale);
                    }
                });
                return scale;
            }
        }

        public bool HasLaserSight
        {
            get
            {
                bool laser = false;
                ForEachAttachment(a => laser |= a.LaserSight);
                return laser;
            }
        }

        private float Product(System.Func<ItemData, float> value)
        {
            float result = 1f;
            ForEachAttachment(a => result *= value(a));
            return result;
        }

        private void ForEachAttachment(System.Action<ItemData> action)
        {
            if (Attachments == null)
            {
                return;
            }

            foreach (ItemData attachment in Attachments)
            {
                if (attachment != null)
                {
                    action(attachment);
                }
            }
        }

        public ItemData Data { get; }
        public int Count { get; set; }
        public bool Rotated { get; set; }

        // 무기를 바꿔 들어도 탄창에 남은 탄을 기억한다.
        public int AmmoInMagazine { get; set; }

        public int Width => Rotated ? Data.Height : Data.Width;
        public int Height => Rotated ? Data.Width : Data.Height;
        public bool IsStackable => Data.MaxStack > 1;
    }
}
