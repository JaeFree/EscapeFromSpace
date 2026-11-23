using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 은신과 엄폐 (기획서 19.3, 2026-11-19 사용자 결정): 테크 트리의 넷째 갈래, 소음기, 전투 설정의 은신 수치.
    public static partial class ProjectSetup
    {
        // 테크 트리 아래쪽에 은신 갈래를 붙인다. 이미 있으면 그대로 둔다 (Inspector에서 고친 값을 두려고).
        private static void EnsureStealthBranch()
        {
            var tree = AssetDatabase.LoadAssetAtPath<TechTree>(TechTreePath);
            if (tree == null || tree.Find("sneak1") != null)
            {
                return;
            }

            const string A = "Alloy";
            const string F = "NanoFiber";
            const string C = "EnergyCrystal";
            (string id, string name, TechStat stat, float amount, int column, int row, string[] requires, int level, int points,
                (string item, int count)[] costs)[] nodes =
            {
                ("sneak1", "조용한 발 I", TechStat.SneakSpeed, 15f, 0, 10, new string[0], 2, 1, new[] { (F, 1) }),
                ("shadow1", "그림자 I", TechStat.DetectionReduction, 15f, 1, 9, new[] { "sneak1" }, 3, 1, new[] { (A, 2), (F, 1) }),
                ("sneak2", "조용한 발 II", TechStat.SneakSpeed, 15f, 1, 10, new[] { "sneak1" }, 5, 1, new[] { (F, 3) }),
                ("assassin", "암살자", TechStat.BackstabBonus, 1f, 1, 11, new[] { "sneak1" }, 4, 1, new[] { (A, 3) }),
                ("camo", "위장막", TechStat.CoverDetectionReduction, 25f, 2, 9, new[] { "shadow1" }, 6, 1, new[] { (F, 2), (C, 1) }),
                ("ghost", "유령", TechStat.DetectionReduction, 25f, 3, 10, new[] { "camo", "sneak2" }, 10, 2, new[] { (C, 3) }),
            };

            var so = new SerializedObject(tree);
            SerializedProperty list = so.FindProperty("_nodes");
            int start = list.arraySize;
            list.arraySize = start + nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                SerializedProperty node = list.GetArrayElementAtIndex(start + i);
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
            EditorUtility.SetDirty(tree);
        }

        // 전투 설정 에셋에 은신 수치가 아직 0이면 초기값을 적어 둔다 (나중에 추가한 필드라서).
        private static void EnsureStealthConfig(CombatConfig config)
        {
            if (config == null)
            {
                return;
            }

            SetFloatIfZero(config, "_detectNearTime", 0.5f);
            SetFloatIfZero(config, "_detectFarTime", 2f);
            SetFloatIfZero(config, "_detectNearDistance", 3f);
            SetFloatIfZero(config, "_sneakDetectMultiplier", 0.5f);
            SetFloatIfZero(config, "_coverDetectMultiplier", 0.5f);
            SetFloatIfZero(config, "_rollDetectMultiplier", 1.5f);
            SetFloatIfZero(config, "_detectDecayPerSecond", 0.4f);
            SetFloatIfZero(config, "_suspicionThreshold", 0.5f);
            SetFloatIfZero(config, "_suspicionLookTime", 3f);
            SetFloatIfZero(config, "_sneakSpeedRatio", 0.5f);
            SetFloatIfZero(config, "_walkNoiseRadius", 4f);
            SetFloatIfZero(config, "_rollNoiseRadius", 5f);
            SetFloatIfZero(config, "_footstepInterval", 0.5f);
            SetFloatIfZero(config, "_gunNoiseRadius", 8f);
            SetFloatIfZero(config, "_referenceHearing", 8f);
            SetFloatIfZero(config, "_coverDistance", 1f);
            SetFloatIfZero(config, "_coverSpreadMultiplier", 3f);
            SetFloatIfZero(config, "_backstabMultiplier", 3f);

            // 기획서 20장 (2026-11-23)
            SetFloatIfZero(config, "_movingTargetSpreadMultiplier", 2f);
            SetFloatIfZero(config, "_movingTargetSpeed", 0.5f);
            SetFloatIfZero(config, "_killHitStop", 0.06f);
            EditorUtility.SetDirty(config);
        }
    }
}
