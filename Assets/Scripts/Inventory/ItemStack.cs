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
