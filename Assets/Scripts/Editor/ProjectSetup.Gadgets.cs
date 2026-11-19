using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 은신 2 (기획서 19.3): 소음기, 홀로그램 미끼, EMP 수류탄과 도구칸 4·5.
    public static partial class ProjectSetup
    {
        private static readonly string[] SuppressorArt =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "kkkkkkkkkkkkkkk.",
            "kddmmmmmmmmmmlkk",
            "kdaaddddddddddmk",
            "kddmmmmmmmmmmlkk",
            "kkkkkkkkkkkkkkk.",
        };

        private static readonly string[] DecoyArt =
        {
            "......aaaa......",
            ".....aaaaaa.....",
            ".....aa..aa.....",
            ".....aaaaaa.....",
            "......aaaa......",
            "....aaaaaaaa....",
            "...aa.aaaa.aa...",
            "...a..aaaa..a...",
            "......a..a......",
            "......a..a......",
            "....kkkkkkkk....",
            "...kmmmmmmmmk...",
            "...kllaaaallk...",
            "....kkkkkkkk....",
        };

        private static readonly string[] EmpArt =
        {
            "......kkk.......",
            ".....kmmmk......",
            "......kmk.......",
            "....kkkkkkk.....",
            "...kmmmmmmmk....",
            "..kmaaaaaaamk...",
            "..kmadddddamk...",
            "..kmadaaadamk...",
            "..kmadddddamk...",
            "..kmaaaaaaamk...",
            "...kmmmmmmmk....",
            "....kkkkkkk.....",
        };

        private static void EnsureGadgets()
        {
            bool isNew = LoadItem("HoloDecoy") == null;
            var gray = new Color(0.45f, 0.48f, 0.56f);

            // 소음기: 총구 칸. 총소리 반경 8 → 3, 반동 −5%. 상점에는 없고 연구 컨테이너와 설계도로만 얻는다.
            bool suppressorNew = LoadItem("Suppressor") == null;
            ItemData suppressor = EnsureItem("Suppressor", "소음기", ItemCategory.Attachment, gray, 0.3f, 200, 1, 1);
            if (suppressorNew)
            {
                var so = new SerializedObject(suppressor);
                so.FindProperty("_attachmentSlot").enumValueIndex = (int)AttachmentSlot.Muzzle;
                so.FindProperty("_recoilMultiplier").floatValue = 0.95f;
                so.FindProperty("_spreadMultiplier").floatValue = 1f;
                so.FindProperty("_aimSpreadMultiplier").floatValue = 1f;
                so.FindProperty("_noiseRadius").floatValue = 3f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(suppressor, "_icon", EnsurePointArt($"{AttachmentArtFolder}/Suppressor.png", SuppressorArt, new Color32(120, 200, 255, 255)));

            ItemData decoy = EnsureGadgetItem("HoloDecoy", "홀로그램 미끼", GadgetKind.Decoy, 0.3f, 125, 8f, 10f, 6f, 0f);
            SetReferenceIfEmpty(decoy, "_icon", EnsurePointArt($"{AttachmentArtFolder}/HoloDecoy.png", DecoyArt, new Color32(110, 235, 255, 255)));
            ItemData emp = EnsureGadgetItem("EmpGrenade", "EMP 수류탄", GadgetKind.Emp, 0.5f, 150, 8f, 3f, 4f, 1f);
            SetReferenceIfEmpty(emp, "_icon", EnsurePointArt($"{AttachmentArtFolder}/EmpGrenade.png", EmpArt, new Color32(90, 170, 255, 255)));
            EnsureItemDatabase();

            if (isNew)
            {
                // 뽑을 때의 몫: 소음기는 연구 컨테이너 3%, 미끼는 일반·연구 5%, EMP는 연구 5% (기획서 19.3).
                AddLootShare("ResearchContainerLoot", "Suppressor", 0.03f);
                AddLootShare("NormalContainerLoot", "HoloDecoy", 0.05f);
                AddLootShare("ResearchContainerLoot", "HoloDecoy", 0.05f);
                AddLootShare("ResearchContainerLoot", "EmpGrenade", 0.05f);
            }

            // 플레이어 도구칸 4·5
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            var user = root.GetComponent<PlayerItemUser>();
            var userSo = new SerializedObject(user);
            SerializedProperty slots = userSo.FindProperty("_slots");
            if (slots.arraySize < PlayerItemUser.SlotCount)
            {
                slots.arraySize = PlayerItemUser.SlotCount;
                slots.GetArrayElementAtIndex(3).objectReferenceValue = decoy;
                slots.GetArrayElementAtIndex(4).objectReferenceValue = emp;
            }

            if (userSo.FindProperty("_wallMask").intValue == 0)
            {
                userSo.FindProperty("_wallMask").intValue = LayerMask.GetMask("Wall");
            }

            userSo.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static ItemData EnsureGadgetItem(string file, string name, GadgetKind kind, float weight, int value, float range, float radius,
            float duration, float delay)
        {
            bool created = LoadItem(file) == null;
            ItemData item = EnsureItem(file, name, ItemCategory.Consumable, new Color(0.4f, 0.8f, 1f), weight, value, 3, 1);
            if (created)
            {
                var so = new SerializedObject(item);
                so.FindProperty("_gadget").enumValueIndex = (int)kind;
                so.FindProperty("_gadgetRange").floatValue = range;
                so.FindProperty("_gadgetRadius").floatValue = radius;
                so.FindProperty("_gadgetDuration").floatValue = duration;
                so.FindProperty("_gadgetDelay").floatValue = delay;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return item;
        }

        // 비율로 뽑는 표에 "뽑을 때 이 몫(share)"이 되도록 비율을 계산해 넣는다. 확률을 굴리는 표는 % 그대로 넣는다.
        private static void AddLootShare(string tableName, string itemName, float share)
        {
            LootTable table = LoadLoot(tableName);
            if (table == null)
            {
                return;
            }

            var so = new SerializedObject(table);
            if (so.FindProperty("_rollEachEntry").boolValue)
            {
                AddLootEntry(tableName, itemName, share * 100f);
                return;
            }

            float total = 0f;
            SerializedProperty list = so.FindProperty("_entries");
            for (int i = 0; i < list.arraySize; i++)
            {
                total += list.GetArrayElementAtIndex(i).FindPropertyRelative("_weight").floatValue;
            }

            AddLootEntry(tableName, itemName, Mathf.Max(0.1f, total * share / (1f - share)));
        }
    }
}
