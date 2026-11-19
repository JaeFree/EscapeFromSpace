namespace EscapeFromSpace.EditorTools
{
    // 대기실 방의 초기 배치. 씬을 처음 만들 때만 쓰이고, 이후 수정은 Tile Palette와 Scene 뷰로 한다.
    public static class LobbyLayout
    {
        public const int Width = 24;
        public const int Height = 14;

        // 방 가운데. 카메라가 방 전체(창고와 포탈)를 한 화면에 담는다.
        public const float SpawnX = 12f;
        public const float SpawnY = 7f;

        // 왼쪽 위 구석
        public const float StorageX = 2f;
        public const float StorageY = 12f;

        // 위쪽 벽 가운데에 붙인다
        public const float PortalX = 12f;
        public const float PortalY = 12f;

        // 사격장: 왼쪽 아래 구석의 서서 쏘는 구역(4×4칸)과 그 오른쪽으로 뻗은 사로.
        public const float RangeX = 3f;
        public const float RangeY = 3f;

        // 사로의 위쪽 칸막이 벽(줄 번호)과 그 벽이 시작하는 칸, 사로가 끝나는 벽의 칸
        public const int LaneWallY = 5;
        public const int LaneWallStartX = 6;
        public const int LaneEndX = 16;

        public static bool[,] BuildWalls()
        {
            var walls = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    walls[x, y] = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                }
            }

            return walls;
        }
    }
}
