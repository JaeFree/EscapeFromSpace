using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 데이터 칩과 설계도 (기획서 19.6, 2026-11-19 사용자 결정).
    public static partial class ProjectSetup
    {
        private static readonly string[] DataChipArt =
        {
            "................",
            "...k.k.k.k.k....",
            "..kkkkkkkkkkk...",
            "..kddddddddak...",
            "..kdaaaaaaadk...",
            "..kdallllladk...",
            "..kdaaaaaaadk...",
            "..kddddddddak...",
            "..kkkkkkkkkkk...",
            "...k.k.k.k.k....",
        };

        // 배마다 단말기 하나 (구역 Id, 자리). 벽에 붙고 출입구에서 먼 곳이다.
        private static readonly (string zone, Vector2 at)[] TerminalSpots =
        {
            ("Cargo", new Vector2(46.5f, 47.5f)),
            ("Quarters", new Vector2(4.5f, 55.5f)),
            ("Medbay", new Vector2(4.5f, 94.5f)),
            ("Lab", new Vector2(70.5f, 94.5f)),
            ("Security", new Vector2(135.5f, 94.5f)),
            ("Engine", new Vector2(102.5f, 56.5f)),
            ("Power", new Vector2(100.5f, 3.5f)),
        };

        // 설계도 6종: (만들어지는 것, 재료)
        private static readonly (string output, (string item, int count)[] inputs)[] BlueprintRecipes =
        {
            ("Suppressor", new[] { ("Alloy", 3), ("NanoFiber", 2) }),
            ("HoloDecoy", new[] { ("CircuitPart", 1), ("EnergyCrystal", 1) }),
            ("EmpGrenade", new[] { ("CircuitPart", 2), ("Alloy", 1) }),
            ("MediKit", new[] { ("MediGel", 2), ("NanoFiber", 1) }),
            ("OxygenCan", new[] { ("Alloy", 1), ("EnergyCrystal", 1) }),
            ("Helmet2", new[] { ("Alloy", 6), ("NanoFiber", 3) }),
        };

        private static void EnsureBlueprints()
        {
            bool isNew = LoadItem("DataChip") == null;
            ItemData chip = EnsureItem("DataChip", "데이터 칩", ItemCategory.Loot, new Color(0.3f, 0.9f, 0.6f), 0.1f, 300, 5, 1);
            SetReferenceIfEmpty(chip, "_icon", EnsurePointArt($"{AttachmentArtFolder}/DataChip.png", DataChipArt, new Color32(80, 240, 150, 255)));
            EnsureItemDatabase();
            EnsureLootTable("TerminalLoot", true, 0, 0, ("DataChip", 100f));

            if (isNew)
            {
                // 연구 컨테이너 10%, 네임드 잔해 40% (기획서 19.6)
                AddLootShare("ResearchContainerLoot", "DataChip", 0.1f);
                foreach (NamedSpec spec in NamedRobots)
                {
                    AddLootEntry($"NamedLoot{spec.Id}", "DataChip", 40f);
                }
            }

            var raidConfig = AssetDatabase.LoadAssetAtPath<RaidConfig>(RaidConfigPath);
            SetReferenceIfEmpty(raidConfig, "_dataChip", chip);
            var raidSo = new SerializedObject(raidConfig);
            SerializedProperty list = raidSo.FindProperty("_blueprints");
            if (list.arraySize == 0)
            {
                list.arraySize = BlueprintRecipes.Length;
                for (int i = 0; i < BlueprintRecipes.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = LoadItem(BlueprintRecipes[i].output);
                }

                raidSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // 제작소에 설계도가 있어야 만드는 줄을 더한다. 같은 결과물에 설계도가 걸린 줄이 이미 있으면 그대로 둔다.
            MerchantData workshop = LoadMerchant("Workshop");
            var workshopSo = new SerializedObject(workshop);
            SerializedProperty recipes = workshopSo.FindProperty("_recipes");
            foreach ((string output, (string item, int count)[] inputs) in BlueprintRecipes)
            {
                bool exists = false;
                for (int i = 0; i < recipes.arraySize; i++)
                {
                    SerializedProperty recipe = recipes.GetArrayElementAtIndex(i);
                    if (recipe.FindPropertyRelative("_output").objectReferenceValue == LoadItem(output)
                        && recipe.FindPropertyRelative("_unlockBlueprint").stringValue == output)
                    {
                        exists = true;
                    }
                }

                if (!exists)
                {
                    AddRecipe(workshopSo, output, 0, string.Empty, inputs);
                    recipes.GetArrayElementAtIndex(recipes.arraySize - 1).FindPropertyRelative("_unlockBlueprint").stringValue = output;
                }
            }

            workshopSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // 정거장 씬: 배마다 함선 단말기. E를 3초 눌러 해킹하면 데이터 칩이 나온다. 판마다 한 번.
        private static void EnsureShipTerminals()
        {
            GameObject parent = GameObject.Find("Terminals");
            if (parent != null)
            {
                return;
            }

            parent = new GameObject("Terminals");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ContainerPrefabPath);
            Sprite sprite = EnsureTerminalSprite();
            foreach ((string zone, Vector2 at) in TerminalSpots)
            {
                var terminal = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                terminal.name = $"ShipTerminal {zone}";
                terminal.transform.position = at;
                var so = new SerializedObject(terminal.GetComponent<Lootable>());
                so.FindProperty("_table").objectReferenceValue = LoadLoot("TerminalLoot");
                so.FindProperty("_width").intValue = 1;
                so.FindProperty("_height").intValue = 1;
                so.FindProperty("_openTime").floatValue = 3f;
                so.FindProperty("_hackNoiseRadius").floatValue = 5f;
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (SpriteRenderer renderer in terminal.GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.sprite = sprite;
                    renderer.color = new Color(0.75f, 1f, 0.85f);
                }
            }
        }
    }
}
