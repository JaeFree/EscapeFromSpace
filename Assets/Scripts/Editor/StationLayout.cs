using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 정거장 맵의 초기 배치 (기획서 15장, 2026-11-18 사용자 결정: 약 140×100칸, 8구역).
    // 씬을 처음 만들 때(또는 옛 60×40 맵을 바꿀 때)만 쓰이고, 이후 수정은 Tile Palette와 Scene 뷰로 한다.
    // 좌표는 칸 번호이고 사각형은 양 끝 칸을 포함한다.
    public static class StationLayout
    {
        public const int Width = 140;
        public const int Height = 100;

        // 도킹 구역 가운데
        public const float SpawnX = 70f;
        public const float SpawnY = 9f;

        public struct Zone
        {
            public string Id;
            public string Name;
            public RectInt Area;

            public Zone(string id, string name, int x0, int y0, int x1, int y1)
            {
                Id = id;
                Name = name;
                Area = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            }
        }

        // 구역 (네임드 로봇의 탐색 범위, 방문 퀘스트, 전체 맵의 이름에 쓴다).
        // 2026-11-19: 구역마다 불시착한 우주선 한 척이다 (기획서 19.1). Id는 저장·퀘스트 때문에 그대로 둔다.
        public static readonly Zone[] Zones =
        {
            new Zone("Docking", "추락 지점", 58, 3, 81, 16),
            new Zone("Cargo", "화물선 잔해", 46, 27, 93, 55),
            new Zone("Quarters", "이민선", 4, 27, 37, 56),
            new Zone("Medbay", "병원선", 4, 67, 37, 95),
            new Zone("Lab", "연구선", 46, 67, 93, 95),
            new Zone("Security", "군 순찰선", 102, 67, 135, 95),
            new Zone("Engine", "채굴선", 102, 27, 127, 57),
            new Zone("Power", "연료 수송선", 100, 3, 135, 16),
        };

        // 방과 복도 (벽을 파낸다)
        private static readonly RectInt[] Rooms =
        {
            R(58, 3, 81, 16), R(4, 4, 14, 14), R(100, 3, 135, 16),
            R(6, 20, 135, 23),
            R(66, 17, 73, 19), R(8, 15, 11, 19), R(115, 17, 118, 19),
            R(46, 27, 93, 55), R(66, 24, 73, 26),
            R(4, 27, 37, 56), R(18, 24, 21, 26),
            R(102, 27, 127, 57), R(112, 24, 115, 26),
            R(38, 39, 45, 42), R(94, 39, 101, 42),
            R(131, 37, 136, 47), R(128, 40, 130, 43),
            R(6, 60, 135, 63),
            R(18, 57, 21, 59), R(68, 56, 71, 59), R(112, 58, 115, 59),
            R(4, 67, 37, 95), R(18, 64, 21, 66),
            R(46, 67, 93, 95), R(58, 64, 61, 66), R(78, 64, 81, 66),
            R(102, 67, 135, 95), R(116, 64, 119, 66),
            R(38, 80, 45, 83), R(94, 80, 101, 83),
        };

        // 엄폐물과 칸막이 (벽을 다시 세운다). 구역마다 분위기에 맞는 덩어리로 둔다 (그림은 18장 단계에서 입힌다).
        public static readonly RectInt[] Blocks =
        {
            // 도킹
            R(62, 8, 63, 9), R(76, 8, 77, 9),
            // 화물칸: 상자 더미와 긴 선반
            R(52, 32, 54, 34), R(60, 44, 62, 46), R(74, 32, 76, 33), R(84, 46, 86, 48), R(66, 39, 73, 40),
            R(52, 48, 53, 51), R(88, 30, 89, 33), R(78, 52, 80, 53),
            // 침실: 2층 침대 줄
            R(8, 32, 14, 33), R(8, 40, 14, 41), R(8, 48, 14, 49), R(24, 32, 30, 33), R(24, 40, 30, 41), R(24, 48, 30, 49),
            // 의무실: 진료 침대, 격리실 벽
            R(8, 72, 9, 75), R(14, 72, 15, 75), R(26, 72, 27, 75), R(32, 72, 33, 75), R(26, 86, 37, 86), R(26, 87, 26, 95),
            // 연구소: 실험대, 샘플 수조, 서버실 벽
            R(52, 72, 57, 73), R(52, 78, 57, 79), R(66, 72, 71, 73), R(66, 78, 71, 79), R(80, 72, 85, 73), R(80, 78, 85, 79),
            R(66, 88, 67, 89), R(74, 88, 75, 89), R(58, 84, 58, 95), R(46, 84, 58, 84),
            // 보안실: 감시 책상, 무기고 벽, 검문 게이트
            R(108, 72, 118, 73), R(124, 67, 124, 80), R(108, 84, 110, 85), R(118, 84, 120, 85), R(128, 88, 129, 89),
            // 기관실: 엔진 4기와 굵은 배관
            R(108, 32, 112, 36), R(118, 32, 122, 36), R(108, 46, 112, 50), R(118, 46, 122, 50), R(104, 41, 125, 41),
            // 동력 구역: 발전기
            R(104, 7, 107, 10), R(112, 7, 115, 10), R(122, 7, 125, 10), R(130, 7, 132, 12),
        };

        // 칸막이에 낸 문 (다시 파낸다)
        private static readonly RectInt[] Doors =
        {
            R(30, 86, 32, 86), R(52, 84, 54, 84), R(124, 73, 124, 75), R(110, 41, 112, 41), R(118, 41, 120, 41),
        };

        public static readonly Vector2[] NormalContainers =
        {
            V(60, 14), V(79, 4),
            V(48, 29), V(48, 53), V(57, 38), V(70, 48), V(82, 28), V(91, 53), V(91, 36), V(63, 29),
            V(5, 35), V(5, 51), V(22, 30), V(35, 45), V(35, 28),
            V(5, 69), V(20, 93), V(35, 69),
            V(103, 28), V(126, 56), V(115, 44), V(103, 52),
            V(101, 15), V(134, 4),
        };

        public static readonly Vector2[] ResearchContainers =
        {
            V(35, 93),
            V(47, 94), V(92, 69), V(62, 93),
            V(134, 69), V(104, 93),
        };

        // 동력 구역의 연구 컨테이너는 동력 셀이 나올 수 있는 표를 쓴다 (기획서 14.5).
        public static readonly Vector2[] PowerResearchContainers = { V(118, 4), V(127, 14) };

        public struct Patrol
        {
            public string Robot;
            public Vector2[] Points;

            public Patrol(string robot, params Vector2[] points)
            {
                Robot = robot;
                Points = points;
            }
        }

        // 구역별 일반 로봇. 저격 로봇은 움직이지 않고, 두 번째 점은 훑는 방향의 중심만 정한다.
        // 2026-11-19 사용자 결정: 구역마다 로봇 5기(네임드 무리 포함). 네임드와 호위(15.2)는 그대로 두고 일반 로봇을 줄였다.
        // 호위가 3기인 연구소·보안실은 1기, 나머지는 2기다.
        public static readonly (string zone, Patrol[] robots)[] Robots =
        {
            ("Cargo", new[]
            {
                new Patrol("MeleeRobot", V(50, 40), V(64, 36), V(64, 50)),
                new Patrol("PistolRobot", V(78, 40), V(90, 40), V(90, 50)),
            }),
            ("Quarters", new[]
            {
                new Patrol("MeleeRobot", V(6, 44), V(33, 44)),
                new Patrol("PistolRobot", V(33, 36), V(33, 53)),
            }),
            // 병원선과 연구선의 일반 적은 에일리언이다 (기획서 19.4, 2026-11-19 사용자 결정).
            ("Medbay", new[]
            {
                new Patrol("AlienStalker", V(10, 80), V(30, 80)),
                new Patrol("AlienSpitter", V(30, 90), V(34, 88)),
            }),
            ("Lab", new[]
            {
                new Patrol("AlienStalker", V(76, 75), V(90, 75)),
            }),
            ("Security", new[]
            {
                new Patrol("RifleRobot", V(112, 90), V(122, 90)),
            }),
            ("Engine", new[]
            {
                new Patrol("MeleeRobot", V(105, 30), V(105, 54)),
                new Patrol("RifleRobot", V(115, 30), V(115, 54)),
            }),
            ("Power", new[]
            {
                // 2026-11-19: 북쪽 출입구 앞(y 12)을 지나면 들어오자마자 맞아서 서쪽 벽을 따라 돌게 했다.
                new Patrol("RifleRobot", V(102, 4), V(102, 14)),
                new Patrol("ShotgunRobot", V(110, 4), V(120, 4)),
            }),
        };

        // 탈출 포드 3곳 (2026-11-18 사용자 결정: 모두 작동, 연구소 포드는 레버로 연다)
        public static readonly Vector2 WestPod = new Vector2(9.5f, 9.5f);
        public static readonly Vector2 EastPod = new Vector2(134f, 42.5f);
        public static readonly Vector2 LabPod = new Vector2(89.5f, 92.5f);
        public static readonly Vector2 LabLever = V(48, 92);

        public static bool[,] BuildWalls()
        {
            var walls = new bool[Width, Height];
            Set(walls, new RectInt(0, 0, Width, Height), true);
            foreach (RectInt room in Rooms)
            {
                Set(walls, room, false);
            }

            foreach (RectInt block in Blocks)
            {
                Set(walls, block, true);
            }

            foreach (RectInt door in Doors)
            {
                Set(walls, door, false);
            }

            return walls;
        }

        private static RectInt R(int x0, int y0, int x1, int y1)
        {
            return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        // 칸 중심 (칸 가운데에 놓아야 로봇 길찾기가 그 칸을 막힌 칸으로 읽는다)
        private static Vector2 V(int x, int y)
        {
            return new Vector2(x + 0.5f, y + 0.5f);
        }

        private static void Set(bool[,] walls, RectInt rect, bool value)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    if (x >= 0 && y >= 0 && x < Width && y < Height)
                    {
                        walls[x, y] = value;
                    }
                }
            }
        }
    }
}
