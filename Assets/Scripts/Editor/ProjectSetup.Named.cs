using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 네임드 로봇 7기와 호위 (기획서 15.2, 2026-11-18 사용자 결정: HP 일반의 4배, 호위 2~3기, 자기 구역만 탐색).
    public static partial class ProjectSetup
    {
        private struct NamedSpec
        {
            public string Id;
            public string Name;
            public string Base;
            public string Zone;
            public Vector2[] Route;
            public string[] Escorts;
            public (string item, float chance)[] Special;
        }

        private static readonly NamedSpec[] NamedRobots =
        {
            new NamedSpec { Id = "K7", Name = "짐꾼 K-7", Base = "MeleeRobot", Zone = "Cargo",
                Route = new[] { C(58, 52), C(82, 43), C(58, 36) }, Escorts = new[] { "MeleeRobot", "PistolRobot" } },
            // 2026-11-19: 남쪽 출입구 바로 안쪽에서 시작하면 들어오자마자 무리 전체에 맞아서 침실 안쪽으로 옮겼다.
            new NamedSpec { Id = "N3", Name = "야간 경비 N-3", Base = "PistolRobot", Zone = "Quarters",
                Route = new[] { C(20, 45), C(20, 54) }, Escorts = new[] { "PistolRobot", "PistolRobot" } },
            new NamedSpec { Id = "M1", Name = "소독기 M-1", Base = "ShotgunRobot", Zone = "Medbay",
                Route = new[] { C(12, 84), C(24, 80) }, Escorts = new[] { "PistolRobot", "PistolRobot" } },
            new NamedSpec { Id = "R9", Name = "감시자 R-9", Base = "RifleRobot", Zone = "Lab",
                Route = new[] { C(62, 82), C(88, 82), C(76, 69) }, Escorts = new[] { "RifleRobot", "ShotgunRobot", "MeleeRobot" } },
            new NamedSpec { Id = "S0", Name = "감시탑 S-0", Base = "SniperRobot", Zone = "Security",
                Route = new[] { C(116, 92), C(116, 76) }, Escorts = new[] { "RifleRobot", "RifleRobot", "MeleeRobot" } },
            new NamedSpec { Id = "E5", Name = "용접공 E-5", Base = "MeleeRobot", Zone = "Engine",
                Route = new[] { C(105, 38), C(125, 52) }, Escorts = new[] { "MeleeRobot", "MeleeRobot" },
                Special = new[] { ("PlasmaIgniter", 30f) } },
            // 2026-11-19: 북쪽 출입구 앞을 지나는 경로였다. 발전기 뒤 동쪽 끝으로 옮겼다.
            new NamedSpec { Id = "PX", Name = "과부하 P-X", Base = "RifleRobot", Zone = "Power",
                Route = new[] { C(127, 4), C(134, 13) }, Escorts = new[] { "ShotgunRobot", "RifleRobot" },
                Special = new[] { ("PowerCell", 50f) } },
        };

        private static Vector2 C(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);

        public static string NamedDataName(string id) => "Named" + id;

        private static void EnsureNamedRobots()
        {
            foreach (NamedSpec spec in NamedRobots)
            {
                string path = RobotDataPath(NamedDataName(spec.Id));
                if (AssetDatabase.LoadAssetAtPath<RobotData>(path) != null)
                {
                    continue;
                }

                // 기반 로봇을 복사해 무기·사격 패턴·이동은 그대로 두고 HP·경험치·크기·색만 바꾼다.
                AssetDatabase.CopyAsset(RobotDataPath(spec.Base), path);
                var data = AssetDatabase.LoadAssetAtPath<RobotData>(path);
                EnsureNamedLoot(spec);
                var so = new SerializedObject(data);
                so.FindProperty("_displayName").stringValue = spec.Name;
                so.FindProperty("_maxHp").floatValue *= 4f;
                so.FindProperty("_xpReward").intValue *= 4;
                so.FindProperty("_visualSize").floatValue *= 1.5f;
                so.FindProperty("_isNamed").boolValue = true;
                so.FindProperty("_namedId").stringValue = spec.Id;
                so.FindProperty("_bodyTint").colorValue = new Color(1f, 0.85f, 0.35f);
                so.FindProperty("_wreckSize").vector2IntValue = new Vector2Int(3, 2);
                so.FindProperty("_lootTable").objectReferenceValue = LoadLoot("NamedLoot" + spec.Id);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 연구 컨테이너급 전리품 (줄마다 확률 %). 기반 로봇의 탄약과 무기, 네임드마다 정한 부품.
        private static void EnsureNamedLoot(NamedSpec spec)
        {
            (string ammo, string weapon) gear = spec.Base switch
            {
                "MeleeRobot" => (null, "LightSaberItem"),
                "PistolRobot" => ("PistolCell", "LaserPistolItem"),
                "RifleRobot" => ("RifleCell", "PulseRifleItem"),
                "SniperRobot" => ("SniperCell", "Nebula7Item"),
                _ => ("PlasmaCapsule", "PlasmaScatterItem"),
            };

            var entries = new System.Collections.Generic.List<(string, float)>
            {
                ("QuantumCore", 60f), ("EnergyCrystal", 50f), ("NanoFiber", 60f), ("MediKit", 35f), ("OxygenCan", 40f),
                ("Vest2", 12f), ("Helmet2", 12f), ("Vest3", 6f), ("Helmet3", 6f), (gear.weapon, 25f),
            };
            if (gear.ammo != null)
            {
                entries.Insert(0, (gear.ammo, 100f));
            }

            if (spec.Special != null)
            {
                entries.AddRange(spec.Special);
            }

            EnsureLootTable("NamedLoot" + spec.Id, true, 0, 0, entries.ToArray());
        }

        // 2026-11-19 사용자 결정: 구역마다 로봇 5기, N-3과 동력 구역 로봇은 출입구에서 먼 안쪽에서 시작.
        // 옛 배치로 만든 씬이면 일반 로봇 구역과 N-3 무리를 지운다. 지운 것은 "없으면 만들기"가 새 배치로 다시 만든다.
        // 수가 이미 맞는 구역은 건드리지 않는다 (Scene 뷰에서 옮긴 경로를 두려고).
        private static void ApplyFiveRobotsPerZone()
        {
            GameObject robots = GameObject.Find("Robots");
            WallGrid grid = Object.FindFirstObjectByType<WallGrid>();
            if (robots == null || grid == null)
            {
                return;
            }

            foreach ((string zone, StationLayout.Patrol[] patrols) in StationLayout.Robots)
            {
                Transform old = robots.transform.Find(zone + "Zone");
                if (old != null && SameRobots(old.GetComponent<RobotZone>(), patrols) && !StartsAt(old, OldPowerRifleStart))
                {
                    continue;
                }

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var list = new (string dataName, Vector2[] waypoints)[patrols.Length];
                for (int i = 0; i < patrols.Length; i++)
                {
                    list[i] = (patrols[i].Robot, patrols[i].Points);
                }

                BuildRobotZone(robots.transform, zone + "Zone", grid, list);
            }

            // 입구 앞에서 시작하던 네임드 무리 (옛 시작점)
            foreach ((string id, Vector2 oldStart) in new[] { ("N3", C(20, 29)), ("PX", C(110, 13)) })
            {
                NamedSpec spec = System.Array.Find(NamedRobots, named => named.Id == id);
                Transform group = robots.transform.Find($"Named{id}_{spec.Zone}");
                if (group != null && StartsAt(group, oldStart))
                {
                    Object.DestroyImmediate(group.gameObject);
                }
            }
        }

        // 수와 종류가 배치표와 같은지 (병원선·연구선이 에일리언으로 바뀐 것을 알아보려고)
        private static bool SameRobots(RobotZone zone, StationLayout.Patrol[] patrols)
        {
            var so = new SerializedObject(zone);
            SerializedProperty spawns = so.FindProperty("_spawns");
            if (spawns.arraySize != patrols.Length)
            {
                return false;
            }

            for (int i = 0; i < patrols.Length; i++)
            {
                Object data = spawns.GetArrayElementAtIndex(i).FindPropertyRelative("_data").objectReferenceValue;
                if (data == null || data.name != patrols[i].Robot)
                {
                    return false;
                }
            }

            return true;
        }

        // 동력 구역 AR 로봇의 옛 시작점
        private static readonly Vector2 OldPowerRifleStart = C(102, 12);

        // 그 무리의 첫 로봇이 이 점에서 시작하는지 (첫 자식의 첫 웨이포인트)
        private static bool StartsAt(Transform group, Vector2 point)
        {
            if (group.childCount == 0 || group.GetChild(0).childCount == 0)
            {
                return false;
            }

            return ((Vector2)group.GetChild(0).GetChild(0).position - point).sqrMagnitude < 0.01f;
        }

        // 구역마다 네임드 무리를 하나씩 둔다. 이미 있으면 그대로 둔다 (Scene 뷰에서 옮긴 경로를 두려고).
        private static void EnsureNamedGroups()
        {
            GameObject robots = GameObject.Find("Robots");
            WallGrid grid = Object.FindFirstObjectByType<WallGrid>();
            GameObject zones = GameObject.Find("Zones");
            if (robots == null || zones == null)
            {
                return;
            }

            foreach (NamedSpec spec in NamedRobots)
            {
                string groupName = $"Named{spec.Id}_{spec.Zone}";
                if (robots.transform.Find(groupName) != null)
                {
                    continue;
                }

                var list = new (string dataName, Vector2[] waypoints)[1 + spec.Escorts.Length];
                list[0] = (NamedDataName(spec.Id), spec.Route);
                Vector2 start = spec.Route[0];
                Vector2[] offsets = { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f) };
                for (int i = 0; i < spec.Escorts.Length; i++)
                {
                    list[i + 1] = (spec.Escorts[i], new[] { start + offsets[i] });
                }

                BuildRobotZone(robots.transform, groupName, grid, list);
                var zone = robots.transform.Find(groupName).GetComponent<RobotZone>();
                SetReferenceIfEmpty(zone, "_player", GameObject.Find("Player").GetComponent<Health>());
                var so = new SerializedObject(zone);
                so.FindProperty("_confineToZone").boolValue = true;
                so.FindProperty("_escortsFollowFirst").boolValue = true;
                so.FindProperty("_zone").objectReferenceValue = zones.transform.Find(spec.Zone).GetComponent<ZoneArea>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
