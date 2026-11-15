namespace EscapeFromSpace.EditorTools
{
    // 회색 박스 맵의 벽 배치. 씬을 처음 만들 때만 쓰이고, 이후 수정은 Tile Palette로 한다.
    public static class StationLayout
    {
        public const int Width = 60;
        public const int Height = 40;

        // 도킹 구역 중앙
        public const float SpawnX = 30f;
        public const float SpawnY = 4.5f;

        public static bool[,] BuildWalls()
        {
            var walls = new bool[Width, Height];

            // 외벽
            Fill(walls, 0, 0, Width - 1, 0);
            Fill(walls, 0, Height - 1, Width - 1, Height - 1);
            Fill(walls, 0, 0, 0, Height - 1);
            Fill(walls, Width - 1, 0, Width - 1, Height - 1);

            // 도킹 구역(남쪽): x 20~39, y 1~7. 양옆은 막는다.
            Fill(walls, 1, 1, 19, 7);
            Fill(walls, 40, 1, 58, 7);

            // 도킹 구역 ↔ 화물칸 격벽과 문
            Fill(walls, 1, 8, 58, 8);
            Clear(walls, 28, 8, 31, 8);

            // 서쪽 탈출 포드 방: x 1~6, y 14~20
            Fill(walls, 1, 9, 7, 13);
            Fill(walls, 1, 21, 7, 24);
            Fill(walls, 7, 14, 7, 20);
            Clear(walls, 7, 16, 7, 18);

            // 동쪽 탈출 포드 방: x 53~58, y 14~20
            Fill(walls, 52, 9, 58, 13);
            Fill(walls, 52, 21, 58, 24);
            Fill(walls, 52, 14, 52, 20);
            Clear(walls, 52, 16, 52, 18);

            // 화물칸(중앙, x 8~51, y 9~24) 안의 격벽과 화물
            Fill(walls, 20, 9, 20, 14);
            Fill(walls, 39, 19, 39, 24);
            Fill(walls, 14, 12, 15, 13);
            Fill(walls, 26, 11, 27, 12);
            Fill(walls, 30, 15, 31, 16);
            Fill(walls, 45, 12, 46, 13);
            Fill(walls, 22, 18, 23, 19);
            Fill(walls, 18, 21, 19, 22);
            Fill(walls, 34, 21, 35, 22);
            Fill(walls, 44, 18, 45, 19);
            Fill(walls, 11, 17, 12, 18);

            // 화물칸 ↔ 연구 구역 격벽과 문 2개
            Fill(walls, 1, 25, 58, 25);
            Clear(walls, 13, 25, 16, 25);
            Clear(walls, 43, 25, 46, 25);

            // 연구 구역(북쪽, x 1~58, y 26~38) 안의 격벽과 엄폐물
            Fill(walls, 29, 30, 30, 38);
            Fill(walls, 5, 32, 12, 32);
            Fill(walls, 47, 32, 54, 32);
            Fill(walls, 10, 28, 12, 29);
            Fill(walls, 20, 33, 21, 35);
            Fill(walls, 38, 33, 39, 35);
            Fill(walls, 22, 28, 23, 29);
            Fill(walls, 36, 28, 37, 29);
            Fill(walls, 50, 35, 51, 36);
            Fill(walls, 7, 35, 8, 36);

            return walls;
        }

        private static void Fill(bool[,] walls, int x0, int y0, int x1, int y1)
        {
            Set(walls, x0, y0, x1, y1, true);
        }

        private static void Clear(bool[,] walls, int x0, int y0, int x1, int y1)
        {
            Set(walls, x0, y0, x1, y1, false);
        }

        private static void Set(bool[,] walls, int x0, int y0, int x1, int y1, bool value)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    walls[x, y] = value;
                }
            }
        }
    }
}
