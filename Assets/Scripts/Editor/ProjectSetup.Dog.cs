using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 보관함 확장과 로봇 개 (기획서 19.5, 2026-11-19 사용자 결정).
    public static partial class ProjectSetup
    {
        private const string DogArtFolder = "Assets/Art/Pixel/Dog";

        // 채굴선의 부서진 로봇 개 자리 (구석, 출입구에서 먼 곳)
        private static readonly Vector2 DogWreckPosition = new Vector2(125.5f, 29.5f);

        // 위에서 본 네발 로봇 개, 머리가 아래
        private static readonly string[] DogArt =
        {
            "....kk....kk....",
            "...kmmk..kmmk...",
            "...kmmkkkkmmk...",
            "....kllllllk....",
            "...kllaaaallk...",
            "..kkllaaaallkk..",
            ".kmkllllllllkmk.",
            ".kmkllllllllkmk.",
            "..kkllaaaallkk..",
            "...kllllllllk...",
            "....kllllllk....",
            "...kmkllllkmk...",
            "...kmkkaakkmk...",
            "....k.kllk.k....",
            "......krrk......",
            ".......kk.......",
        };

        private static readonly string[] DogCoreArt =
        {
            "................",
            "......kkkk......",
            "....kkaaaakk....",
            "...kaallllaak...",
            "...kalaaaalak...",
            "..kalaarraalak..",
            "..kalaarraalak..",
            "...kalaaaalak...",
            "...kaallllaak...",
            "....kkaaaakk....",
            "......kkkk......",
        };

        private static void EnsureDogAssets()
        {
            Directory.CreateDirectory(DogArtFolder);
            bool coreNew = LoadItem("RobotDogCore") == null;
            ItemData core = EnsureItem("RobotDogCore", "로봇 개 코어", ItemCategory.Loot, new Color(0.9f, 0.6f, 0.2f), 1f, 0, 1, 1);
            if (coreNew)
            {
                var so = new SerializedObject(core);
                so.FindProperty("_cannotSell").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(core, "_icon", EnsurePointArt($"{DogArtFolder}/RobotDogCore.png", DogCoreArt, new Color32(255, 170, 60, 255)));
            EnsureItemDatabase();
            EnsureLootTable("DogWreckLoot", true, 0, 0, ("RobotDogCore", 100f));

            Sprite dog = EnsurePointArt($"{DogArtFolder}/RobotDog.png", DogArt, new Color32(255, 170, 60, 255));
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            SetReferenceIfEmpty(root.GetComponent<PlayerInventory>(), "_dogSprite", dog);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            EnsureDogQuest();
        }

        // 퀘스트: 보관함 확장 보상(한의 4·9·16)과 로봇 개 퀘스트 21 (오토, 채굴선 방문 다음).
        private static void EnsureDogQuest()
        {
            var database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestDatabasePath);
            if (database == null)
            {
                return;
            }

            var so = new SerializedObject(database);
            SerializedProperty quests = so.FindProperty("_quests");
            SetSecureReward(quests, "Q04", new Vector2Int(2, 1), "보관함 2칸");
            SetSecureReward(quests, "Q09", new Vector2Int(2, 2), "보관함 4칸");
            SetSecureReward(quests, "Q16", new Vector2Int(3, 2), "보관함 6칸");

            if (IndexOfQuest(quests, "Q21") < 0)
            {
                // 같은 상인의 퀘스트는 목록 순서대로 열린다. 채굴선 방문(Q05) 바로 뒤에 넣는다.
                int at = IndexOfQuest(quests, "Q05") + 1;
                quests.InsertArrayElementAtIndex(at);
                SerializedProperty quest = quests.GetArrayElementAtIndex(at);
                quest.FindPropertyRelative("_id").stringValue = "Q21";
                quest.FindPropertyRelative("_title").stringValue = "고철 사냥개";
                quest.FindPropertyRelative("_npc").stringValue = "Workshop";
                quest.FindPropertyRelative("_chapter").intValue = 2;
                quest.FindPropertyRelative("_story").stringValue =
                    "채굴선 구석에 부서진 로봇 개가 있더라. 짐 나르는 용도로 만든 놈이야.\n\n" +
                    "그 안의 코어만 가져오면 내가 고쳐 줄게. 가방을 하나 등에 달아 주면, 죽어도 그 안의 것만은 지켜 줄 거야.";
                quest.FindPropertyRelative("_doneLine").stringValue = "코어가 살아 있어! 이제 이 녀석이 너를 따라다닐 거야. 등의 가방 세 칸은 죽어도 남아.";
                quest.FindPropertyRelative("_requires").stringValue = "Q05";
                quest.FindPropertyRelative("_kind").enumValueIndex = (int)QuestKind.Deliver;
                quest.FindPropertyRelative("_count").intValue = 1;
                quest.FindPropertyRelative("_robot").objectReferenceValue = null;
                quest.FindPropertyRelative("_namedId").stringValue = string.Empty;
                quest.FindPropertyRelative("_zoneId").stringValue = string.Empty;
                quest.FindPropertyRelative("_credits").intValue = 0;
                quest.FindPropertyRelative("_rewardNote").stringValue = "로봇 개 합류 (가방 3칸, 죽어도 남음)";
                FillCosts(quest.FindPropertyRelative("_deliver"), new[] { ("RobotDogCore", 1) });
                FillCosts(quest.FindPropertyRelative("_rewardItems"), new (string, int)[0]);
                quest.FindPropertyRelative("_secureBoxSize").vector2IntValue = Vector2Int.zero;
                quest.FindPropertyRelative("_unlocksDog").boolValue = true;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static int IndexOfQuest(SerializedProperty quests, string id)
        {
            for (int i = 0; i < quests.arraySize; i++)
            {
                if (quests.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void SetSecureReward(SerializedProperty quests, string id, Vector2Int size, string note)
        {
            int index = IndexOfQuest(quests, id);
            if (index < 0)
            {
                return;
            }

            SerializedProperty quest = quests.GetArrayElementAtIndex(index);
            SerializedProperty sizeProperty = quest.FindPropertyRelative("_secureBoxSize");
            if (sizeProperty.vector2IntValue != Vector2Int.zero)
            {
                return;
            }

            sizeProperty.vector2IntValue = size;
            SerializedProperty rewardNote = quest.FindPropertyRelative("_rewardNote");
            rewardNote.stringValue = string.IsNullOrEmpty(rewardNote.stringValue) ? note : $"{rewardNote.stringValue}, {note}";
        }

        // 정거장 씬: 채굴선 구석의 부서진 로봇 개. 이미 개가 있으면 레이드에서 스스로 사라진다.
        private static void EnsureDogWreck()
        {
            if (GameObject.Find("RobotDogWreck") != null)
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RobotWreckPrefabPath);
            var wreck = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            wreck.name = "RobotDogWreck";
            wreck.transform.position = DogWreckPosition;
            var lootable = wreck.GetComponent<Lootable>();
            var so = new SerializedObject(lootable);
            so.FindProperty("_table").objectReferenceValue = LoadLoot("DogWreckLoot");
            so.FindProperty("_width").intValue = 1;
            so.FindProperty("_height").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            var renderer = wreck.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(0.75f, 0.5f, 0.2f);
            }

            wreck.AddComponent<DogWreck>();
        }
    }
}
