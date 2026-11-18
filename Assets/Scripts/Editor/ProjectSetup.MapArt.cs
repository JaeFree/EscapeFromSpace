using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace.EditorTools
{
    // 픽셀아트 맵 (기획서 18장, 2026-11-18 사용자 결정: CC0 픽셀아트, ScreenShots 분위기 참고).
    // 옛 단색 타일로 칠한 맵만 새 타일로 다시 칠한다. 한 번 칠한 뒤에는 Tile Palette로 고친 것을 건드리지 않는다.
    public static partial class ProjectSetup
    {
        private const string MapArtFolder = "Assets/Art/Pixel/Map";
        private const string PixelTileFolder = "Assets/Tiles/Pixel";

        private static readonly string[] FloorNames = { "Floor_Grid", "Floor_Plate", "Floor_Smooth", "Floor_Navy", "Floor_Arrow" };
        private static readonly string[] WallNames = { "Wall_TL", "Wall_T", "Wall_TR", "Wall_L", "Wall_C", "Wall_R", "Wall_BL", "Wall_B", "Wall_BR", "Wall_Face" };

        // 16×16 점 그림 소품 (무료 팩에 없는 침대 두 가지)
        private static readonly string[] BunkArt =
        {
            "kkkkkkkkkkkkkkkk",
            "kmmmmmmmmmmmmmmk",
            "kmllllkddddddddk",
            "kmllllkdaaaaaadk",
            "kmllllkdaaaaaadk",
            "kmllllkddddddddk",
            "kmmmmmmmmmmmmmmk",
            "kkkkkkkkkkkkkkkk",
            "kmmmmmmmmmmmmmmk",
            "kmllllkddddddddk",
            "kmllllkdaaaaaadk",
            "kmllllkdaaaaaadk",
            "kmllllkddddddddk",
            "kmmmmmmmmmmmmmmk",
            "kkkkkkkkkkkkkkkk",
        };

        private static readonly string[] MedBedArt =
        {
            "..kkkkkkkkkkkk..",
            "..kllllllllllk..",
            "..kllllrrlllllk.",
            "..klllrrrrllllk.",
            "..kllllrrlllllk.",
            "..kllllllllllk..",
            "..kmmmmmmmmmmk..",
            "..kaaaaaaaaaak..",
            "..kaaaaaaaaaak..",
            "..kaaaaaaaaaak..",
            "..kaaaaaaaaaak..",
            "..kaaaaaaaaaak..",
            "..kmmmmmmmmmmk..",
            "..kkkkkkkkkkkk..",
            "...kk......kk...",
        };

        private static Tile LoadPixelTile(string name) => AssetDatabase.LoadAssetAtPath<Tile>($"{PixelTileFolder}/{name}.asset");

        private static void EnsurePixelTiles()
        {
            Directory.CreateDirectory(PixelTileFolder);
            foreach (string name in FloorNames)
            {
                EnsurePixelTile(name, Tile.ColliderType.None);
            }

            foreach (string name in WallNames)
            {
                EnsurePixelTile(name, Tile.ColliderType.Grid);
            }
        }

        private static void EnsurePixelTile(string name, Tile.ColliderType collider)
        {
            string path = $"{PixelTileFolder}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Tile>(path) != null)
            {
                return;
            }

            Sprite sprite = EnsureTileSprite($"{MapArtFolder}/{name}.png");
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = collider;
            AssetDatabase.CreateAsset(tile, path);
        }

        // 타일은 48점이 1칸. 칸 사이에 틈이 보이지 않게 점 필터와 꽉 찬 메시로 읽는다.
        private static Sprite EnsureTileSprite(string path)
        {
            const string marker = "EscapeFromSpace:pixeltile";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.userData != marker)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 48f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.userData = marker;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 벽 타일맵에 아직 옛 단색 벽 타일이 있으면 새로 칠할 때다.
        private static bool NeedsRepaint(Tilemap walls)
        {
            var oldWall = AssetDatabase.LoadAssetAtPath<Tile>(WallTilePath);
            foreach (Vector3Int cell in walls.cellBounds.allPositionsWithin)
            {
                if (walls.GetTile(cell) == oldWall)
                {
                    return true;
                }
            }

            return false;
        }

        // 바닥은 floorFor가 정한 타일과 색으로, 벽은 둘레의 바닥 방향을 보고 9조각 중 하나로 칠한다.
        private static void RepaintMap(System.Func<int, int, (string tile, Color tint)> floorFor)
        {
            GameObject grid = GameObject.Find("Grid");
            Tilemap floor = grid.transform.Find("Floor").GetComponent<Tilemap>();
            Tilemap walls = grid.transform.Find("Walls").GetComponent<Tilemap>();
            walls.CompressBounds();
            floor.CompressBounds();
            BoundsInt bounds = floor.cellBounds;

            var wallSet = new HashSet<Vector2Int>();
            foreach (Vector3Int cell in walls.cellBounds.allPositionsWithin)
            {
                if (walls.GetTile(cell) != null)
                {
                    wallSet.Add(new Vector2Int(cell.x, cell.y));
                }
            }

            // 맵 밖은 벽으로 본다 (바깥 테두리에 선이 그어지지 않게).
            bool IsWall(int x, int y) => wallSet.Contains(new Vector2Int(x, y)) || x < bounds.xMin || y < bounds.yMin || x >= bounds.xMax || y >= bounds.yMax;

            var tiles = new Dictionary<string, Tile>();
            Tile Get(string name)
            {
                if (!tiles.TryGetValue(name, out Tile tile))
                {
                    tile = LoadPixelTile(name);
                    tiles[name] = tile;
                }

                return tile;
            }

            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                (string tileName, Color tint) = floorFor(cell.x, cell.y);
                floor.SetTile(cell, Get(tileName));
                floor.SetTileFlags(cell, TileFlags.None);
                floor.SetColor(cell, tint);
            }

            foreach (Vector2Int cell in wallSet)
            {
                bool up = IsWall(cell.x, cell.y + 1);
                bool down = IsWall(cell.x, cell.y - 1);
                bool left = IsWall(cell.x - 1, cell.y);
                bool right = IsWall(cell.x + 1, cell.y);

                string name;
                if (!down)
                {
                    // 아래가 바닥이면 위에서 비스듬히 보이는 벽면을 그린다.
                    name = "Wall_Face";
                }
                else if (!up)
                {
                    name = !left ? "Wall_TL" : !right ? "Wall_TR" : "Wall_T";
                }
                else if (!left)
                {
                    name = "Wall_L";
                }
                else if (!right)
                {
                    name = "Wall_R";
                }
                else
                {
                    name = "Wall_C";
                }

                walls.SetTile(new Vector3Int(cell.x, cell.y, 0), Get(name));
            }

            EditorUtility.SetDirty(floor);
            EditorUtility.SetDirty(walls);
        }

        // ---------- 정거장 ----------

        private static (string, Color) StationFloor(int x, int y)
        {
            var position = new Vector2(x + 0.5f, y + 0.5f);
            foreach (StationLayout.Zone zone in StationLayout.Zones)
            {
                if (zone.Area.Contains(new Vector2Int(x, y)))
                {
                    switch (zone.Id)
                    {
                        case "Docking": return ("Floor_Plate", new Color(0.85f, 0.88f, 0.95f));
                        case "Cargo": return ("Floor_Grid", new Color(0.92f, 0.88f, 0.78f));
                        case "Quarters": return ("Floor_Navy", new Color(1f, 0.92f, 0.85f));
                        case "Medbay": return ("Floor_Smooth", new Color(0.9f, 1f, 0.97f));
                        case "Lab": return ("Floor_Grid", new Color(0.85f, 0.95f, 1f));
                        case "Security": return ("Floor_Navy", new Color(0.9f, 0.92f, 1f));
                        case "Engine": return ("Floor_Plate", new Color(1f, 0.88f, 0.8f));
                        case "Power": return ("Floor_Navy", new Color(1f, 0.95f, 0.8f));
                    }
                }
            }

            // 복도와 포드 방
            return ("Floor_Smooth", new Color(0.75f, 0.78f, 0.82f));
        }

        private static void EnsureStationArt()
        {
            EnsurePixelTiles();
            Tilemap walls = GameObject.Find("Grid").transform.Find("Walls").GetComponent<Tilemap>();
            if (!NeedsRepaint(walls))
            {
                return;
            }

            RepaintMap(StationFloor);
            EnsureWallColliders();
            BuildStationProps();
            BuildLights(StationLights());
        }

        // 엄폐물 덩어리마다 그 구역에 맞는 소품을 얹는다. 판정은 아래의 벽 타일이 하고 소품은 그림뿐이다.
        private static void BuildStationProps()
        {
            GameObject old = GameObject.Find("Props");
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var parent = new GameObject("Props").transform;
            foreach (RectInt block in StationLayout.Blocks)
            {
                string zone = ZoneIdAt(block.center);
                switch (zone)
                {
                    case "Docking":
                        FillProps(parent, block, "Prop_Crate", 1, 1);
                        break;
                    case "Cargo":
                        FillProps(parent, block, block.width >= 2 ? "Prop_CrateWide" : "Prop_Crate", block.width >= 2 ? 2 : 1, 1);
                        break;
                    case "Quarters":
                        FillProps(parent, block, PointProp("Prop_Bunk", BunkArt, new Color32(80, 120, 190, 255)), 1, 1);
                        break;
                    case "Medbay":
                        if (block.width > 1 && block.height > 1)
                        {
                            FillProps(parent, block, PointProp("Prop_MedBed", MedBedArt, new Color32(120, 200, 190, 255)), 1, 2);
                        }

                        break;
                    case "Lab":
                        if (block.width > 1 && block.height > 1 && block.width == block.height)
                        {
                            FillProps(parent, block, "Prop_Generator", 2, 2);
                        }
                        else if (block.width > 1 && block.height > 1)
                        {
                            FillProps(parent, block, "Prop_ConsoleText", 1, 2);
                        }
                        else if (block.width > 1 || block.height > 1)
                        {
                            FillProps(parent, block, "Prop_Server", 1, 1);
                        }

                        break;
                    case "Security":
                        if (block.width > 1 && block.height > 1)
                        {
                            FillProps(parent, block, "Prop_Console", 1, 2);
                        }
                        else
                        {
                            FillProps(parent, block, "Prop_Locker", 1, 1);
                        }

                        break;
                    case "Engine":
                        if (block.height > 1)
                        {
                            FillProps(parent, block, "Prop_Generator", 2, 2);
                        }

                        break;
                    case "Power":
                        if (block.height > 4)
                        {
                            FillProps(parent, block, "Prop_PipeColumn", 3, 6);
                        }
                        else
                        {
                            FillProps(parent, block, "Prop_GeneratorOpen", 2, 2);
                        }

                        break;
                }
            }
        }

        private static string ZoneIdAt(Vector2 cell)
        {
            foreach (StationLayout.Zone zone in StationLayout.Zones)
            {
                if (zone.Area.Contains(Vector2Int.FloorToInt(cell)))
                {
                    return zone.Id;
                }
            }

            return null;
        }

        private static string PointProp(string name, string[] art, Color32 accent)
        {
            EnsurePointArt($"{MapArtFolder}/{name}.png", art, accent);
            return name;
        }

        // 덩어리를 footprint(칸) 크기의 칸으로 나눠 소품을 하나씩 놓는다. 그림은 비율을 지키며 그 칸에 맞춘다.
        private static void FillProps(Transform parent, RectInt block, string spriteName, int footW, int footH)
        {
            Sprite sprite = EnsurePixelSprite($"{MapArtFolder}/{spriteName}.png");
            int countX = Mathf.Max(1, block.width / footW);
            int countY = Mathf.Max(1, block.height / footH);
            float cellW = block.width / (float)countX;
            float cellH = block.height / (float)countY;
            Vector2 size = sprite.bounds.size;
            float scale = Mathf.Min(cellW / size.x, cellH / size.y) * 0.95f;

            for (int ix = 0; ix < countX; ix++)
            {
                for (int iy = 0; iy < countY; iy++)
                {
                    var prop = new GameObject(spriteName);
                    prop.transform.SetParent(parent, false);
                    prop.transform.position = new Vector3(block.xMin + (ix + 0.5f) * cellW, block.yMin + (iy + 0.5f) * cellH, 0f);
                    prop.transform.localScale = new Vector3(scale, scale, 1f);
                    var renderer = prop.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = -4;
                }
            }
        }

        // 구역마다 천장등 (분위기 색). 방 안의 바닥 칸에 10칸 간격으로 둔다.
        private static List<(Vector2 position, Color color)> StationLights()
        {
            var lights = new List<(Vector2, Color)>();
            foreach (StationLayout.Zone zone in StationLayout.Zones)
            {
                Color color = zone.Id switch
                {
                    "Cargo" => new Color(1f, 0.85f, 0.6f),
                    "Quarters" => new Color(1f, 0.8f, 0.6f),
                    "Medbay" => new Color(0.75f, 1f, 0.95f),
                    "Lab" => new Color(0.6f, 0.85f, 1f),
                    "Security" => new Color(1f, 0.55f, 0.5f),
                    "Engine" => new Color(1f, 0.65f, 0.4f),
                    "Power" => new Color(0.9f, 0.6f, 1f),
                    _ => new Color(0.85f, 0.9f, 1f),
                };

                for (float x = zone.Area.xMin + 5f; x < zone.Area.xMax; x += 10f)
                {
                    for (float y = zone.Area.yMin + 5f; y < zone.Area.yMax; y += 10f)
                    {
                        lights.Add((new Vector2(x, y), color));
                    }
                }
            }

            // 복도에는 흐린 흰빛을 듬성듬성
            for (float x = 10f; x < StationLayout.Width; x += 14f)
            {
                lights.Add((new Vector2(x, 22f), new Color(0.8f, 0.85f, 1f)));
                lights.Add((new Vector2(x, 62f), new Color(0.8f, 0.85f, 1f)));
            }

            return lights;
        }

        // 전체를 어둡게 하고 천장등 둘레만 밝힌다 (ScreenShots의 어두운 정거장 분위기).
        private static void BuildLights(List<(Vector2 position, Color color)> lights)
        {
            GameObject global = GameObject.Find("Global Light 2D");
            if (global != null)
            {
                global.GetComponent<Light2D>().intensity = 0.45f;
            }

            GameObject old = GameObject.Find("Lights");
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var parent = new GameObject("Lights").transform;
            foreach ((Vector2 position, Color color) in lights)
            {
                var lightObject = new GameObject("CeilingLight");
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.position = position;
                var light = lightObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = color;
                light.intensity = 0.85f;
                light.pointLightInnerRadius = 1.5f;
                light.pointLightOuterRadius = 8f;
                light.falloffIntensity = 0.6f;
            }
        }

        // ---------- 컨테이너 그림 ----------

        // 컨테이너와 창고를 같은 픽셀아트 소품으로 바꾼다. 아직 처음 넣은 Kenney 그림일 때만.
        private static void EnsurePixelContainers()
        {
            ReplacePrefabSprite(ContainerPrefabPath, "crate_gray", "Prop_Crate", 0.9f);
            ReplacePrefabSprite(ResearchContainerPrefabPath, "crate_red", "Prop_Server", 0.95f);
            ReplacePrefabSprite(StoragePrefabPath, "locker_gray", "Prop_Locker", 1.9f);
        }

        // 프리팹의 크기(충돌체)는 그대로 두고 그림만 바꾼다. 소품 그림을 복사해 이 프리팹에 맞는 크기(칸)로 읽힌다.
        private static void ReplacePrefabSprite(string prefabPath, string oldSprite, string newSprite, float size)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            var renderer = root.GetComponent<SpriteRenderer>();
            string current = renderer.sprite != null ? AssetDatabase.GetAssetPath(renderer.sprite) : string.Empty;
            if (current.EndsWith("/" + oldSprite + ".png"))
            {
                string source = $"{MapArtFolder}/{newSprite}.png";
                string copy = $"{MapArtFolder}/{newSprite}_{Path.GetFileNameWithoutExtension(prefabPath)}.png";
                if (!File.Exists(copy))
                {
                    AssetDatabase.CopyAsset(source, copy);
                }

                var importer = (TextureImporter)AssetImporter.GetAtPath(copy);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = Mathf.Max(width, height) / size;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.userData = "EscapeFromSpace:pixel";
                importer.SaveAndReimport();

                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(copy);
                renderer.color = Color.white;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
