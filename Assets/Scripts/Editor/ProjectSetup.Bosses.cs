using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 기획서 21장 (2026-11-23): 네임드 패턴. 에셋은 처음 만들 때만 초기값을 넣는다 (그 뒤로는 Inspector 값을 지킨다).
    public static partial class ProjectSetup
    {
        private const string BossFolder = "Assets/Data/Bosses";

        private struct PatternValues
        {
            public string Name;
            public BossPatternKind Kind;
            public float Cooldown;
            public float UseDistance;
            public float Windup;
            public float Damage;
            public float SecondaryDamage;
            public float Radius;
            public float Length;
            public int Count;
            public float Angle;
            public float Speed;
            public float Interval;
            public float Duration;
            public int Repeat;
            public int MaxUses;
        }

        private static void EnsureBossAssets()
        {
            if (!AssetDatabase.IsValidFolder(BossFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Bosses");
            }

            BossData k7 = EnsureBoss("K7",
                new PatternValues { Name = "화물 상자 던지기", Kind = BossPatternKind.CrateThrow, Cooldown = 6f, UseDistance = 9f, Windup = 1f, Damage = 25f, Radius = 1.2f, Count = 1, Repeat = 1 },
                new PatternValues { Name = "돌진", Kind = BossPatternKind.Charge, Cooldown = 15f, UseDistance = 9f, Windup = 0.8f, Damage = 35f, Radius = 0.6f, Length = 8f, Speed = 14f, Duration = 1.5f, Count = 1, Repeat = 1 });
            BossData n3 = EnsureBoss("N3",
                new PatternValues { Name = "부채꼴 사격", Kind = BossPatternKind.FanShot, Cooldown = 6f, UseDistance = 12f, Windup = 0.5f, Damage = 12f, Length = 12f, Count = 5, Angle = 50f, Speed = 10f, Repeat = 1 },
                new PatternValues { Name = "비상 경보", Kind = BossPatternKind.Summon, Cooldown = 15f, UseDistance = 14f, Windup = 1f, Count = 2, Repeat = 1, MaxUses = 2 });
            BossData m1 = EnsureBoss("M1",
                new PatternValues { Name = "소독 가스", Kind = BossPatternKind.GasPool, Cooldown = 6f, UseDistance = 10f, Windup = 0.7f, Damage = 10f, Radius = 1.5f, Duration = 4f, Count = 1, Repeat = 1 },
                new PatternValues { Name = "격리 봉쇄", Kind = BossPatternKind.Quarantine, Cooldown = 15f, UseDistance = 10f, Windup = 1f, Damage = 20f, SecondaryDamage = 10f, Radius = 4f, Length = 9f, Count = 7, Angle = 50f, Speed = 9f, Interval = 1f, Duration = 5f, Repeat = 3 });
            BossData e5 = EnsureBoss("E5",
                new PatternValues { Name = "회전 베기", Kind = BossPatternKind.SpinSlash, Cooldown = 6f, UseDistance = 3f, Windup = 0.7f, Damage = 30f, Radius = 2.5f, Count = 1, Repeat = 1 },
                new PatternValues { Name = "용접 돌진", Kind = BossPatternKind.WeldDash, Cooldown = 15f, UseDistance = 9f, Windup = 0.8f, Damage = 25f, SecondaryDamage = 15f, Radius = 0.6f, Length = 8f, Speed = 11f, Duration = 4f, Count = 1, Repeat = 1 });
            BossData px = EnsureBoss("PX",
                new PatternValues { Name = "방전", Kind = BossPatternKind.Discharge, Cooldown = 6f, UseDistance = 4.5f, Windup = 1f, Damage = 30f, Radius = 4f, Count = 1, Repeat = 1 },
                new PatternValues { Name = "전기 사슬", Kind = BossPatternKind.ChainLightning, Cooldown = 15f, UseDistance = 7f, Windup = 0.8f, Damage = 30f, Radius = 0.3f, Length = 7f, Count = 4, Speed = 45f, Duration = 3f, Repeat = 1 });
            BossData r9 = EnsureBoss("R9",
                new PatternValues { Name = "회전 난사", Kind = BossPatternKind.Spiral, Cooldown = 7f, UseDistance = 12f, Windup = 0.4f, Damage = 8f, Length = 10f, Count = 2, Angle = 14f, Speed = 7f, Interval = 0.08f, Duration = 2f, Repeat = 1 },
                new PatternValues { Name = "관통 레이저", Kind = BossPatternKind.PiercingLaser, Cooldown = 15f, UseDistance = 16f, Windup = 1f, Damage = 40f, Radius = 0.4f, Length = 16f, Count = 3, Angle = 20f, Repeat = 1 });
            BossData s0 = EnsureBoss("S0",
                new PatternValues { Name = "3연속 저격", Kind = BossPatternKind.TripleSnipe, Cooldown = 7f, UseDistance = 20f, Windup = 0.7f, Damage = 30f, Radius = 0.35f, Length = 20f, Interval = 0.25f, Count = 1, Repeat = 3 },
                new PatternValues { Name = "포격 요청", Kind = BossPatternKind.Artillery, Cooldown = 16f, UseDistance = 20f, Windup = 0.9f, Damage = 30f, Radius = 1.5f, Length = 3f, Count = 5, Interval = 0.35f, Repeat = 1 });

            // 비상 경보가 부르는 로봇
            var n3So = new SerializedObject(n3);
            if (n3So.FindProperty("_summonPrefab").objectReferenceValue == null)
            {
                n3So.FindProperty("_summonPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Robot>(RobotPrefabPath);
                n3So.FindProperty("_summonData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath("PistolRobot"));
                n3So.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(n3);
            }

            // 상위 3기의 마지막 발악은 하위 4기의 특수 패턴을 빌린다 (사용자 결정).
            BossData[] lower = { k7, n3, m1, e5 };
            foreach (BossData upper in new[] { r9, s0, px })
            {
                var so = new SerializedObject(upper);
                SerializedProperty sources = so.FindProperty("_lastStandSources");
                if (!so.FindProperty("_hasLastStand").boolValue && sources.arraySize == 0)
                {
                    so.FindProperty("_hasLastStand").boolValue = true;
                    sources.arraySize = lower.Length;
                    for (int i = 0; i < lower.Length; i++)
                    {
                        sources.GetArrayElementAtIndex(i).objectReferenceValue = lower[i];
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(upper);
                }
            }

            // 네임드 로봇 에셋에 연결한다. 이미 다른 것이 들어 있으면 그대로 둔다.
            foreach ((string robot, BossData boss) in new[] { ("NamedK7", k7), ("NamedN3", n3), ("NamedM1", m1), ("NamedE5", e5), ("NamedPX", px), ("NamedR9", r9), ("NamedS0", s0) })
            {
                var data = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(robot));
                if (data == null)
                {
                    continue;
                }

                var so = new SerializedObject(data);
                SerializedProperty property = so.FindProperty("_boss");
                if (property.objectReferenceValue == null)
                {
                    property.objectReferenceValue = boss;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(data);
                }
            }
        }

        private static BossData EnsureBoss(string id, PatternValues normal, PatternValues special)
        {
            string path = $"{BossFolder}/Boss{id}.asset";
            var boss = AssetDatabase.LoadAssetAtPath<BossData>(path);
            if (boss != null)
            {
                return boss;
            }

            boss = ScriptableObject.CreateInstance<BossData>();
            var so = new SerializedObject(boss);
            WritePattern(so.FindProperty("_normal"), normal);
            WritePattern(so.FindProperty("_special"), special);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(boss, path);
            return boss;
        }

        private static void WritePattern(SerializedProperty p, PatternValues v)
        {
            p.FindPropertyRelative("_displayName").stringValue = v.Name;
            p.FindPropertyRelative("_kind").enumValueIndex = (int)v.Kind;
            p.FindPropertyRelative("_cooldown").floatValue = v.Cooldown;
            p.FindPropertyRelative("_useDistance").floatValue = v.UseDistance;
            p.FindPropertyRelative("_windup").floatValue = v.Windup;
            p.FindPropertyRelative("_damage").floatValue = v.Damage;
            p.FindPropertyRelative("_secondaryDamage").floatValue = v.SecondaryDamage;
            p.FindPropertyRelative("_radius").floatValue = v.Radius;
            p.FindPropertyRelative("_length").floatValue = v.Length;
            p.FindPropertyRelative("_count").intValue = v.Count;
            p.FindPropertyRelative("_angle").floatValue = v.Angle;
            p.FindPropertyRelative("_speed").floatValue = v.Speed;
            p.FindPropertyRelative("_interval").floatValue = v.Interval;
            p.FindPropertyRelative("_duration").floatValue = v.Duration;
            p.FindPropertyRelative("_repeat").intValue = v.Repeat;
            p.FindPropertyRelative("_maxUses").intValue = v.MaxUses;
        }
    }
}
