using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 레벨과 테크 트리 (기획서 12장, 2026-11-17 사용자 결정). 다른 에셋과 같은 규칙을 따른다:
    // 없는 것만 만들고, 비어 있는 칸만 채운다.
    public static partial class ProjectSetup
    {
        private const string ItemArtFolder = "Assets/Art/Items";
        private const string TechTreePath = "Assets/Data/TechTree.asset";
        private const string TerminalSpritePath = ItemArtFolder + "/Terminal.png";

        private static void EnsureProgression()
        {
            Directory.CreateDirectory(ItemArtFolder);

            // 강화 재료 3종. 처음 만들 때만 루팅 표에 넣는다 (나중에 표에서 뺀 것을 다시 넣지 않으려는 것이다).
            bool isNew = LoadItem("Alloy") == null;
            var materialColor = new Color(0.55f, 0.75f, 0.85f);
            EnsureItem("Alloy", "강화 합금", ItemCategory.Loot, materialColor, 0.5f, 15, 10, 1);
            EnsureItem("NanoFiber", "나노 섬유", ItemCategory.Loot, materialColor, 0.3f, 40, 10, 1);
            EnsureItem("EnergyCrystal", "에너지 결정", ItemCategory.Loot, materialColor, 0.4f, 120, 10, 1);
            EnsureItemDatabase();

            SetReferenceIfEmpty(LoadItem("Alloy"), "_icon", EnsureAlloySprite());
            SetReferenceIfEmpty(LoadItem("NanoFiber"), "_icon", EnsureFiberSprite());
            SetReferenceIfEmpty(LoadItem("EnergyCrystal"), "_icon", EnsureCrystalSprite());

            if (isNew)
            {
                // 합금은 흔하게, 섬유는 가끔, 결정은 연구 구역과 강한 로봇에서만 나온다.
                AddLootEntry("NormalContainerLoot", "Alloy", 20f);
                AddLootEntry("NormalContainerLoot", "NanoFiber", 8f);
                AddLootEntry("ResearchContainerLoot", "NanoFiber", 14f);
                AddLootEntry("ResearchContainerLoot", "EnergyCrystal", 14f);
                AddLootEntry("MeleeRobotLoot", "Alloy", 50f);
                AddLootEntry("PistolRobotLoot", "Alloy", 50f);
                AddLootEntry("RifleRobotLoot", "Alloy", 50f);
                AddLootEntry("SniperRobotLoot", "NanoFiber", 40f);
                AddLootEntry("SniperRobotLoot", "EnergyCrystal", 20f);
                AddLootEntry("ShotgunRobotLoot", "NanoFiber", 40f);
                AddLootEntry("ShotgunRobotLoot", "EnergyCrystal", 20f);
            }

            // 로봇을 부수면 얻는 경험치. 강한 로봇일수록 많다.
            SetRobotXp("MeleeRobot", 10);
            SetRobotXp("PistolRobot", 10);
            SetRobotXp("RifleRobot", 15);
            SetRobotXp("ShotgunRobot", 20);
            SetRobotXp("SniperRobot", 25);

            EnsureTechTree();

            SetCue(AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath), "_levelUp", 0.7f, 1.25f, 0f, DigitalAudio, "powerUp2");

            // 플레이어에 진행 상태를 붙이고, 강화를 읽는 스크립트들에 잇는다.
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            EnsureComponent(root, out PlayerProgress progress);
            SetReferenceIfEmpty(progress, "_tree", AssetDatabase.LoadAssetAtPath<TechTree>(TechTreePath));
            SetReferenceIfEmpty(root.GetComponent<PlayerController>(), "_progress", progress);
            SetReferenceIfEmpty(root.GetComponent<PlayerShooter>(), "_progress", progress);
            SetReferenceIfEmpty(root.GetComponent<PlayerOxygen>(), "_progress", progress);
            SetReferenceIfEmpty(root.GetComponent<PlayerRoll>(), "_progress", progress);
            SetReferenceIfEmpty(root.GetComponent<PlayerInteractor>(), "_progress", progress);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void SetRobotXp(string robotName, int xp)
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(robotName)));
            SerializedProperty property = so.FindProperty("_xpReward");
            if (property.intValue == 0)
            {
                property.intValue = xp;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 이미 있는 루팅 표에 한 줄을 덧붙인다. 그 아이템이 이미 있으면 그대로 둔다.
        private static void AddLootEntry(string tableName, string itemName, float weight)
        {
            ItemData item = LoadItem(itemName);
            var so = new SerializedObject(LoadLoot(tableName));
            SerializedProperty list = so.FindProperty("_entries");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("_item").objectReferenceValue == item)
                {
                    return;
                }
            }

            list.arraySize++;
            SerializedProperty entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            entry.FindPropertyRelative("_item").objectReferenceValue = item;
            entry.FindPropertyRelative("_weight").floatValue = weight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 트리의 초기 모양: 전투, 생존, 운반 세 갈래가 왼쪽에서 오른쪽으로 가지를 친다.
        // 처음 만들 때만 넣는다. 이후에는 에셋의 Inspector에서 고친다.
        private static void EnsureTechTree()
        {
            if (AssetDatabase.LoadAssetAtPath<TechTree>(TechTreePath) != null)
            {
                return;
            }

            const string A = "Alloy";
            const string F = "NanoFiber";
            const string C = "EnergyCrystal";

            (string id, string name, TechStat stat, float amount, int column, int row, string[] requires, int level, int points,
                (string item, int count)[] costs)[] nodes =
            {
                // 전투
                ("dmg1", "화력 I", TechStat.Damage, 5f, 0, 1, new string[0], 1, 1, new[] { (A, 2) }),
                ("dmg2", "화력 II", TechStat.Damage, 5f, 1, 0, new[] { "dmg1" }, 3, 1, new[] { (A, 3) }),
                ("reload1", "빠른 손 I", TechStat.ReloadSpeed, 10f, 1, 2, new[] { "dmg1" }, 2, 1, new[] { (A, 2), (F, 1) }),
                ("dmg3", "화력 III", TechStat.Damage, 10f, 2, 0, new[] { "dmg2" }, 6, 1, new[] { (A, 4), (C, 1) }),
                ("reload2", "빠른 손 II", TechStat.ReloadSpeed, 10f, 2, 2, new[] { "reload1" }, 5, 1, new[] { (F, 3) }),
                ("dmg4", "과충전", TechStat.Damage, 10f, 3, 1, new[] { "dmg3", "reload2" }, 10, 2, new[] { (C, 3) }),

                // 생존
                ("hp1", "강화 신체 I", TechStat.MaxHp, 10f, 0, 4, new string[0], 1, 1, new[] { (A, 2) }),
                ("hp2", "강화 신체 II", TechStat.MaxHp, 15f, 1, 3, new[] { "hp1" }, 3, 1, new[] { (A, 3) }),
                ("oxy1", "산소 탱크 I", TechStat.Oxygen, 60f, 1, 5, new[] { "hp1" }, 2, 1, new[] { (A, 1), (F, 1) }),
                ("hp3", "강화 신체 III", TechStat.MaxHp, 25f, 2, 3, new[] { "hp2" }, 6, 1, new[] { (A, 4), (C, 1) }),
                ("armor1", "내장 장갑", TechStat.Armor, 1f, 2, 4, new[] { "hp2" }, 5, 1, new[] { (A, 3), (F, 2) }),
                ("oxy2", "산소 탱크 II", TechStat.Oxygen, 90f, 2, 5, new[] { "oxy1" }, 5, 1, new[] { (F, 3) }),
                ("armor2", "생존 본능", TechStat.Armor, 2f, 3, 4, new[] { "hp3", "oxy2" }, 10, 2, new[] { (C, 3) }),

                // 운반과 기동
                ("weight1", "근력 I", TechStat.MaxWeight, 3f, 0, 7, new string[0], 1, 1, new[] { (A, 2) }),
                ("weight2", "근력 II", TechStat.MaxWeight, 4f, 1, 6, new[] { "weight1" }, 3, 1, new[] { (A, 3) }),
                ("move1", "경량 보행 I", TechStat.MoveSpeed, 5f, 1, 8, new[] { "weight1" }, 2, 1, new[] { (A, 1), (F, 1) }),
                ("weight3", "근력 III", TechStat.MaxWeight, 5f, 2, 6, new[] { "weight2" }, 6, 1, new[] { (A, 4), (C, 1) }),
                ("loot1", "능숙한 손", TechStat.LootSpeed, 20f, 2, 7, new[] { "weight2" }, 4, 1, new[] { (F, 2) }),
                ("roll1", "민첩", TechStat.RollCooldown, 15f, 2, 8, new[] { "move1" }, 5, 1, new[] { (F, 3) }),
                ("move2", "경량 보행 II", TechStat.MoveSpeed, 5f, 3, 7, new[] { "weight3", "roll1" }, 10, 2, new[] { (C, 3) }),
            };

            var tree = ScriptableObject.CreateInstance<TechTree>();
            var so = new SerializedObject(tree);
            SerializedProperty list = so.FindProperty("_nodes");
            list.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                SerializedProperty node = list.GetArrayElementAtIndex(i);
                node.FindPropertyRelative("_id").stringValue = nodes[i].id;
                node.FindPropertyRelative("_displayName").stringValue = nodes[i].name;
                node.FindPropertyRelative("_stat").enumValueIndex = (int)nodes[i].stat;
                node.FindPropertyRelative("_amount").floatValue = nodes[i].amount;
                node.FindPropertyRelative("_column").intValue = nodes[i].column;
                node.FindPropertyRelative("_row").intValue = nodes[i].row;
                node.FindPropertyRelative("_requiredLevel").intValue = nodes[i].level;
                node.FindPropertyRelative("_pointCost").intValue = nodes[i].points;

                SerializedProperty requires = node.FindPropertyRelative("_requires");
                requires.arraySize = nodes[i].requires.Length;
                for (int j = 0; j < nodes[i].requires.Length; j++)
                {
                    requires.GetArrayElementAtIndex(j).stringValue = nodes[i].requires[j];
                }

                SerializedProperty costs = node.FindPropertyRelative("_costs");
                costs.arraySize = nodes[i].costs.Length;
                for (int j = 0; j < nodes[i].costs.Length; j++)
                {
                    SerializedProperty cost = costs.GetArrayElementAtIndex(j);
                    cost.FindPropertyRelative("_item").objectReferenceValue = LoadItem(nodes[i].costs[j].item);
                    cost.FindPropertyRelative("_count").intValue = nodes[i].costs[j].count;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(tree, TechTreePath);
        }

        // ---------- 그림 ----------

        // 테두리와 채움 색을 거리로 섞는다. distance는 도형 경계에서 잰 거리(안쪽이 음수)다.
        private static Color Shape(float distance, Color fill)
        {
            var outline = new Color(0.1f, 0.09f, 0.16f);
            Color color = Color.Lerp(fill, outline, Mathf.Clamp01(distance / 0.04f + 1f));
            color.a = 1f - Mathf.Clamp01((distance - 0.06f) / 0.04f + 0.5f);
            return color;
        }

        // 강화 합금: 육각 금속 덩이
        private static Sprite EnsureAlloySprite()
        {
            return EnsureEffectSprite(ItemArtFolder + "/Alloy.png", 64, (ColorAt)((x, y) =>
            {
                float hex = Mathf.Max(Mathf.Abs(x) * 0.866f + Mathf.Abs(y) * 0.5f, Mathf.Abs(y)) - 0.72f;
                Color fill = Color.Lerp(new Color(0.42f, 0.48f, 0.6f), new Color(0.8f, 0.86f, 0.95f), Mathf.InverseLerp(-0.7f, 0.7f, y));

                // 안쪽에 한 겹 더 밝은 선을 넣어 깎은 면처럼 보이게 한다.
                if (hex > -0.3f && hex < -0.22f)
                {
                    fill = Color.Lerp(fill, Color.white, 0.5f);
                }

                return Shape(hex, fill);
            }));
        }

        // 나노 섬유: 감아 놓은 실타래
        private static Sprite EnsureFiberSprite()
        {
            return EnsureEffectSprite(ItemArtFolder + "/NanoFiber.png", 64, (ColorAt)((x, y) =>
            {
                float radius = Mathf.Sqrt(x * x + y * y);
                float ring = Mathf.Abs(radius - 0.52f) - 0.26f;
                float strand = 0.5f + 0.5f * Mathf.Sin(Mathf.Atan2(y, x) * 9f + radius * 14f);
                Color fill = Color.Lerp(new Color(0.12f, 0.55f, 0.5f), new Color(0.55f, 1f, 0.85f), strand);
                return Shape(ring, fill);
            }));
        }

        // 에너지 결정: 마름모 보석
        private static Sprite EnsureCrystalSprite()
        {
            return EnsureEffectSprite(ItemArtFolder + "/EnergyCrystal.png", 64, (ColorAt)((x, y) =>
            {
                float diamond = (Mathf.Abs(x) / 0.58f + Mathf.Abs(y) / 0.86f - 1f) * 0.48f;

                // 네 면의 밝기를 달리해 깎인 보석처럼 보이게 한다.
                float shade = (x < 0f ? 0.25f : 0f) + (y > 0f ? 0.45f : 0f);
                Color fill = Color.Lerp(new Color(0.6f, 0.2f, 0.85f), new Color(1f, 0.75f, 1f), shade);
                return Shape(diamond, fill);
            }));
        }

        // 강화 단말기: 어두운 몸체에 푸른 화면과 호박색 단추
        private static Sprite EnsureTerminalSprite()
        {
            return EnsureEffectSprite(TerminalSpritePath, 64, (ColorAt)((x, y) =>
            {
                float body = Mathf.Max(Mathf.Abs(x) - 0.8f, Mathf.Abs(y) - 0.8f);
                var fill = new Color(0.2f, 0.17f, 0.3f);

                if (body > -0.1f)
                {
                    fill = new Color(1f, 0.68f, 0.22f);
                }
                else if (Mathf.Abs(x) < 0.56f && y > -0.12f && y < 0.58f)
                {
                    // 화면에 가로줄을 넣는다.
                    float line = Mathf.Repeat(y * 8f, 1f) < 0.5f ? 1f : 0.8f;
                    fill = new Color(0.3f, 0.85f, 0.95f) * line;
                }
                else if (y < -0.3f && y > -0.52f && Mathf.Repeat(x * 2.5f + 0.5f, 1f) > 0.3f && Mathf.Abs(x) < 0.56f)
                {
                    fill = new Color(1f, 0.78f, 0.35f);
                }

                fill.a = 1f;
                return Shape(body, fill);
            }));
        }

        // ---------- 대기실 씬 ----------

        // 강화 단말기와 그 화면. 단말기는 처음 한 번만 놓고 이후에는 Scene 뷰에서 옮긴다.
        private static void EnsureTechTerminal(GameObject player, GameObject canvasObject, LobbyManager lobby, int wallLayer)
        {
            GameObject terminal = GameObject.Find("TechTerminal");
            if (terminal == null)
            {
                // 오른쪽 위 구석. 창고(왼쪽 위)와 떨어뜨려 E가 겹치지 않게 한다.
                terminal = new GameObject("TechTerminal");
                terminal.transform.position = new Vector3(LobbyLayout.Width - 2.2f, LobbyLayout.StorageY, 0f);
                terminal.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                terminal.layer = wallLayer;

                var renderer = terminal.AddComponent<SpriteRenderer>();
                renderer.sprite = EnsureTerminalSprite();

                // 창고처럼 지나갈 수 없게 막는다.
                terminal.AddComponent<BoxCollider2D>().size = new Vector2(0.85f, 0.85f);
            }

            EnsureComponent(canvasObject, out TechTreeUI tech);
            SetReferenceIfEmpty(tech, "_inventory", player.GetComponent<PlayerInventory>());
            SetReferenceIfEmpty(tech, "_input", player.GetComponent<PlayerInputReader>());
            SetReferenceIfEmpty(tech, "_controller", player.GetComponent<PlayerController>());
            SetReferenceIfEmpty(tech, "_lobby", lobby);
            SetReferenceIfEmpty(tech, "_terminal", terminal.transform);
        }
    }
}
