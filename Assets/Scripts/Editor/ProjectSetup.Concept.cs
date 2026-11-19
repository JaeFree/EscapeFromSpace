using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace.EditorTools
{
    // 2026-11-19 사용자 결정: 정거장 대신 행성의 "우주선 무덤"에 불시착한 이야기로 바꾼다 (기획서 19.1).
    // 구역·퀘스트·아이템의 Id는 저장 파일 때문에 그대로 두고, 화면에 보이는 이름과 글, 배 사이 길의 바닥만 바꾼다.
    public static partial class ProjectSetup
    {
        private const string LongPodName = "도약 엔진";
        private const string LongPodGreeting = "회수선 뒤쪽의 도약 엔진.\n\n부품 여섯 종을 모두 설치하면 이 행성을 떠날 수 있다.";
        private const string SecurityKeyName = "방어망 해제 코드";
        private const string SandTileName = "Floor_Sand";

        private static readonly Dictionary<string, string> MerchantGreetings = new Dictionary<string, string>
        {
            ["Weapons"] = "순찰선에서 싸우던 병사였지. 우리 배를 떨어뜨린 센티널 로봇들은 누구보다 잘 알아.\n\n" +
                "녀석들은 재장전할 때와 연사 사이가 비어. 그 틈을 노려.\n\n살 게 있으면 골라 봐. 외상은 없어.",
            ["Armor"] = "화물선에서 일하던 기술자야. 우주복이라면 눈 감고도 고쳐.\n\n" +
                "바깥 공기는 독이야. 구멍 난 걸 입고 나가면 그걸로 끝이니까 제대로 된 걸 사 가.",
            ["Medical"] = "병원선에 그것들이 퍼지기 직전에 빠져나왔어요. 약품은 그때 챙긴 게 전부예요.\n\n" +
                "다치면 아끼지 말고 쓰세요. 살아 돌아오는 게 먼저니까요.",
            ["Workshop"] = "우리 배의 도약 엔진, 봤지? 저걸 고치면 이 행성을 떠날 수 있어.\n\n" +
                "재료만 가져오면 무기든 부품이든 내가 만들어 줄게.",
        };

        // 정거장 시절의 구역 이름. 씬의 이름이 이 중 하나면 아직 사용자가 고치지 않은 것이다.
        private static readonly HashSet<string> OldZoneNames = new HashSet<string>
        {
            "도킹 구역", "화물칸", "침실", "의무실", "연구소", "보안실", "기관실", "동력 구역",
        };

        private static readonly StoryShot[] NewPrologueShots = { StoryShot.Planet, StoryShot.Attack, StoryShot.Crash, StoryShot.Shelter };
        private static readonly StoryShot[] NewEndingShots = { StoryShot.Launch, StoryShot.Liftoff, StoryShot.Escape, StoryShot.Farewell };

        // 옛 이야기(오르비스-9)가 그대로 남은 에셋이면 새 이야기로 바꾼다. 프롤로그를 사용자가 고쳤으면 건드리지 않는다.
        private static void ApplyWreckFieldTexts()
        {
            var database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestDatabasePath);
            if (database == null)
            {
                return;
            }

            var so = new SerializedObject(database);
            SerializedProperty prologue = so.FindProperty("_prologue");
            if (!prologue.stringValue.Contains("오르비스"))
            {
                return;
            }

            prologue.stringValue = Prologue;
            so.FindProperty("_ending").stringValue = Ending;
            FillShots(so.FindProperty("_prologueShots"), NewPrologueShots);
            FillShots(so.FindProperty("_endingShots"), NewEndingShots);

            SerializedProperty quests = so.FindProperty("_quests");
            foreach (QuestSpec spec in QuestSpecs)
            {
                for (int i = 0; i < quests.arraySize; i++)
                {
                    SerializedProperty quest = quests.GetArrayElementAtIndex(i);
                    if (quest.FindPropertyRelative("_id").stringValue != spec.Id)
                    {
                        continue;
                    }

                    quest.FindPropertyRelative("_title").stringValue = spec.Title;
                    quest.FindPropertyRelative("_story").stringValue = spec.Story;
                    quest.FindPropertyRelative("_doneLine").stringValue = spec.Done ?? string.Empty;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (KeyValuePair<string, string> pair in MerchantGreetings)
            {
                SetMerchantText(pair.Key, null, pair.Value);
            }

            SetMerchantText("LongPod", LongPodName, LongPodGreeting);

            ItemData key = LoadItem("SecurityKey");
            if (key != null)
            {
                var keySo = new SerializedObject(key);
                keySo.FindProperty("_displayName").stringValue = SecurityKeyName;
                keySo.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
        }

        private static void FillShots(SerializedProperty list, StoryShot[] shots)
        {
            list.arraySize = shots.Length;
            for (int i = 0; i < shots.Length; i++)
            {
                list.GetArrayElementAtIndex(i).enumValueIndex = (int)shots[i];
            }
        }

        private static void SetMerchantText(string id, string displayName, string greeting)
        {
            MerchantData merchant = LoadMerchant(id);
            if (merchant == null)
            {
                return;
            }

            var so = new SerializedObject(merchant);
            if (displayName != null)
            {
                so.FindProperty("_displayName").stringValue = displayName;
            }

            so.FindProperty("_greeting").stringValue = greeting;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 정거장 씬: 구역 이름을 배 이름으로 바꾸고, 배 사이의 길(어느 구역에도 들지 않는 바닥)을 협곡 모래로 칠한다.
        private static void ApplyWreckFieldStation()
        {
            GameObject zones = GameObject.Find("Zones");
            if (zones != null)
            {
                foreach (ZoneArea area in zones.GetComponentsInChildren<ZoneArea>())
                {
                    var so = new SerializedObject(area);
                    SerializedProperty name = so.FindProperty("_displayName");
                    if (!OldZoneNames.Contains(name.stringValue))
                    {
                        continue;
                    }

                    foreach (StationLayout.Zone zone in StationLayout.Zones)
                    {
                        if (zone.Id == area.Id)
                        {
                            name.stringValue = zone.Name;
                        }
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Tile sand = EnsureSandTile();
            GameObject grid = GameObject.Find("Grid");
            if (grid == null || sand == null)
            {
                return;
            }

            Tilemap floor = grid.transform.Find("Floor").GetComponent<Tilemap>();
            Tile oldCorridor = LoadPixelTile("Floor_Smooth");
            floor.CompressBounds();
            bool changed = false;
            foreach (Vector3Int cell in floor.cellBounds.allPositionsWithin)
            {
                if (floor.GetTile(cell) != oldCorridor || InAnyZone(cell.x, cell.y))
                {
                    continue;
                }

                floor.SetTile(cell, sand);
                floor.SetTileFlags(cell, TileFlags.None);
                floor.SetColor(cell, Color.white);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(floor);
            }
        }

        private static bool InAnyZone(int x, int y)
        {
            foreach (StationLayout.Zone zone in StationLayout.Zones)
            {
                if (zone.Area.Contains(new Vector2Int(x, y)))
                {
                    return true;
                }
            }

            return false;
        }

        // 협곡 바닥: 모래색 바탕에 결을 넣고 자갈과 금을 흩뿌린 48×48 그림. 무료 팩에 맞는 바닥이 없어서 코드로 그린다.
        private static Tile EnsureSandTile()
        {
            string png = $"{MapArtFolder}/{SandTileName}.png";
            if (!File.Exists(png))
            {
                const int size = 48;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var random = new System.Random(48);
                var baseColor = new Color(0.62f, 0.47f, 0.33f);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // 이어 붙였을 때 경계가 보이지 않게 둘레를 감싸는 잡음을 쓴다.
                        float angleX = x / (float)size * Mathf.PI * 2f;
                        float angleY = y / (float)size * Mathf.PI * 2f;
                        float n = Mathf.PerlinNoise(3f + Mathf.Cos(angleX) * 1.3f, 5f + Mathf.Sin(angleX) * 1.3f + Mathf.Cos(angleY) * 1.3f)
                            * 0.6f + Mathf.PerlinNoise(9f + Mathf.Sin(angleY) * 2.5f, 1f + Mathf.Cos(angleX + angleY) * 2.5f) * 0.4f;
                        float shade = Mathf.Round((0.82f + n * 0.3f) * 6f) / 6f;
                        texture.SetPixel(x, y, baseColor * shade + new Color(0f, 0f, 0f, 1f));
                    }
                }

                for (int i = 0; i < 26; i++)
                {
                    int x = random.Next(size);
                    int y = random.Next(size);
                    Color pebble = random.Next(2) == 0 ? new Color(0.45f, 0.35f, 0.26f) : new Color(0.75f, 0.6f, 0.43f);
                    texture.SetPixel(x, y, pebble);
                    if (random.Next(3) == 0)
                    {
                        texture.SetPixel((x + 1) % size, y, pebble);
                    }
                }

                File.WriteAllBytes(png, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(png);
            }

            EnsurePixelTile(SandTileName, Tile.ColliderType.None);
            return LoadPixelTile(SandTileName);
        }
    }
}
