using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace.EditorTools
{
    // 대기실 격납고 (기획서 18장): 원래 방(창고·포탈·사격장·강화 단말기) 오른쪽에 격납고를 붙인다.
    // 격납고 가운데에 장거리 탈출 포드, 벽을 따라 상인 5명의 자리를 둔다.
    public static partial class ProjectSetup
    {
        // 원래 방은 x 0~23, 격납고는 x 23~43 (안쪽 24~42), y 0~19 (안쪽 1~18). 둘 사이 벽(x 23)의 y 2~11을 튼다.
        private const int HangarRight = 43;
        private const int HangarTop = 19;
        private const int HangarDoorBottom = 2;
        private const int HangarDoorTop = 11;

        public static readonly Vector2 LongPodPosition = new Vector2(33.5f, 10.5f);

        private static readonly (string id, Vector2 position)[] HangarNpcs =
        {
            ("Weapons", new Vector2(26.5f, 17.2f)),
            ("Armor", new Vector2(30.5f, 17.2f)),
            ("Medical", new Vector2(38.5f, 17.2f)),
            ("Workshop", new Vector2(33.5f, 5.2f)),
            ("BlackMarket", new Vector2(41.6f, 3.5f)),
        };

        private static void EnsureHangar(Tile oldWallTile, Tile oldFloorTile)
        {
            GameObject grid = GameObject.Find("Grid");
            Tilemap walls = grid.transform.Find("Walls").GetComponent<Tilemap>();
            Tilemap floor = grid.transform.Find("Floor").GetComponent<Tilemap>();
            walls.CompressBounds();
            if (walls.cellBounds.xMax > HangarRight)
            {
                return;
            }

            // 옛 단색 타일로 넓힌다. 바로 뒤에 RepaintMap이 픽셀아트로 다시 칠한다.
            for (int x = LobbyLayout.Width - 1; x <= HangarRight; x++)
            {
                for (int y = 0; y <= HangarTop; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    floor.SetTile(cell, oldFloorTile);
                    bool border = x == HangarRight || y == 0 || y == HangarTop;
                    bool divider = x == LobbyLayout.Width - 1 && (y < HangarDoorBottom || y > HangarDoorTop);
                    walls.SetTile(cell, border || divider ? oldWallTile : null);
                }
            }

            // 원래 방 위쪽(y 13~19, x 0~22)은 벽으로 채운다 (L자 바깥이 비어 보이지 않게).
            for (int x = 0; x < LobbyLayout.Width - 1; x++)
            {
                for (int y = LobbyLayout.Height; y <= HangarTop; y++)
                {
                    floor.SetTile(new Vector3Int(x, y, 0), oldFloorTile);
                    walls.SetTile(new Vector3Int(x, y, 0), oldWallTile);
                }
            }

            // 상인을 격납고 벽을 따라 옮긴다 (처음 세운 임시 자리에 있을 때만).
            foreach ((string id, Vector2 position) in HangarNpcs)
            {
                foreach (Npc npc in Object.FindObjectsByType<Npc>(FindObjectsSortMode.None))
                {
                    if (npc.Merchant != null && npc.Merchant.Id == id && npc.transform.position.x < LobbyLayout.Width)
                    {
                        npc.transform.position = position;
                    }
                }
            }
        }

        private static (string, Color) LobbyFloor(int x, int y)
        {
            if (x >= LobbyLayout.Width - 1)
            {
                return ("Floor_Grid", new Color(0.95f, 0.92f, 0.85f));
            }

            if (y < LobbyLayout.LaneWallY && x <= LobbyLayout.LaneEndX)
            {
                return ("Floor_Navy", new Color(1f, 0.95f, 0.85f));
            }

            return ("Floor_Plate", new Color(0.92f, 0.9f, 0.88f));
        }

        private static void EnsureLobbyArt()
        {
            EnsurePixelTiles();
            Tilemap walls = GameObject.Find("Grid").transform.Find("Walls").GetComponent<Tilemap>();
            if (!NeedsRepaint(walls))
            {
                return;
            }

            RepaintMap(LobbyFloor);
            EnsureWallColliders();
            BuildLobbyProps();

            var lights = new List<(Vector2, Color)>
            {
                (new Vector2(6f, 9f), new Color(1f, 0.85f, 0.65f)),
                (new Vector2(14f, 9f), new Color(1f, 0.85f, 0.65f)),
                (new Vector2(6f, 3f), new Color(1f, 0.9f, 0.6f)),
                (new Vector2(13f, 3f), new Color(1f, 0.9f, 0.6f)),
                (new Vector2(28f, 15f), new Color(1f, 0.8f, 0.6f)),
                (new Vector2(38f, 15f), new Color(0.75f, 1f, 0.9f)),
                (new Vector2(33.5f, 10.5f), new Color(0.7f, 0.85f, 1f)),
                (new Vector2(28f, 5f), new Color(1f, 0.85f, 0.6f)),
                (new Vector2(39f, 5f), new Color(0.95f, 0.6f, 1f)),
            };
            BuildLights(lights);
        }

        // 격납고의 소품: 상인 자리 뒤의 가판대·작업대, 자판기, 화물 상자, 발전기. 지나갈 수 없게 충돌체를 둔다.
        private static void BuildLobbyProps()
        {
            GameObject old = GameObject.Find("Props");
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var parent = new GameObject("Props").transform;
            int wallLayer = LayerMask.NameToLayer("Wall");
            (string sprite, Vector2 position, float size)[] props =
            {
                ("Prop_CrateWide", new Vector2(26.5f, 18.1f), 1.8f),
                ("Prop_Locker", new Vector2(30.5f, 18.2f), 1.1f),
                ("Prop_Vending", new Vector2(36.6f, 17.9f), 1.6f),
                ("Prop_MedBedProp", new Vector2(40.2f, 17.6f), 1.4f),
                ("Prop_ConsoleText", new Vector2(31.5f, 4.6f), 1.6f),
                ("Prop_Console", new Vector2(35.5f, 4.6f), 1.6f),
                ("Prop_Crate", new Vector2(42.1f, 1.9f), 0.9f),
                ("Prop_Crate", new Vector2(42.1f, 5.2f), 0.9f),
                ("Prop_Generator", new Vector2(25.2f, 2.2f), 1.8f),
                ("Prop_Server", new Vector2(42.1f, 9.5f), 0.95f),
                ("Prop_Server", new Vector2(42.1f, 10.5f), 0.95f),
                ("Prop_Server", new Vector2(42.1f, 11.5f), 0.95f),
            };

            PointProp("Prop_MedBedProp", MedBedArt, new Color32(120, 200, 190, 255));
            foreach ((string spriteName, Vector2 position, float size) in props)
            {
                Sprite sprite = EnsurePixelSprite($"{MapArtFolder}/{spriteName}.png");
                var prop = new GameObject(spriteName);
                prop.transform.SetParent(parent, false);
                prop.transform.position = position;
                Vector2 bounds = sprite.bounds.size;
                float scale = size / Mathf.Max(bounds.x, bounds.y);
                prop.transform.localScale = new Vector3(scale, scale, 1f);
                prop.layer = wallLayer;
                var renderer = prop.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = -3;
                prop.AddComponent<BoxCollider2D>();
            }
        }
    }
}
