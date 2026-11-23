using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 기획서 20장 (2026-11-23): 센티널 수색, 가치와 등급, 귀환 비콘 고장, 첫 출격 안내, 손맛.
    public static partial class ProjectSetup
    {
        private const string ItemGradesPath = "Assets/Data/ItemGrades.asset";

        private static void EnsureTensionAssets()
        {
            // 등급 표는 처음 만들 때만 기획서 20.2 초기값을 넣는다.
            if (AssetDatabase.LoadAssetAtPath<ItemGradeTable>(ItemGradesPath) == null)
            {
                var table = ScriptableObject.CreateInstance<ItemGradeTable>();
                var so = new SerializedObject(table);
                SerializedProperty grades = so.FindProperty("_grades");
                (string name, int min, Color color)[] rows =
                {
                    ("일반", 0, new Color(0.62f, 0.62f, 0.66f)),
                    ("고급", 30, new Color(0.35f, 0.85f, 0.4f)),
                    ("희귀", 80, new Color(0.3f, 0.6f, 1f)),
                    ("영웅", 200, new Color(0.75f, 0.4f, 1f)),
                    ("전설", 500, new Color(1f, 0.78f, 0.2f)),
                };
                grades.arraySize = rows.Length;
                for (int i = 0; i < rows.Length; i++)
                {
                    SerializedProperty row = grades.GetArrayElementAtIndex(i);
                    row.FindPropertyRelative("_name").stringValue = rows[i].name;
                    row.FindPropertyRelative("_minValue").intValue = rows[i].min;
                    row.FindPropertyRelative("_color").colorValue = rows[i].color;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(table, ItemGradesPath);
            }

            // 소리는 파일 이름과 길이만 보고 골랐다. 심장 소리는 낮게 깐 둔탁한 충격음이다.
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
            SetCue(library, "_heartbeat", 0.75f, 0.6f, 0f, ImpactAudio, "impactSoft_heavy_000");
            SetCue(library, "_killConfirm", 0.5f, 1.6f, 0.03f, DigitalAudio, "tone1");
            SetCue(library, "_sentinelAlarm", 0.8f, 1.25f, 0f, DigitalAudio, "lowThreeTone");

            // 증원 로봇은 처음 보는 사람도 상대할 수 있는 권총·AR 로봇으로 시작한다.
            var config = AssetDatabase.LoadAssetAtPath<RaidConfig>(RaidConfigPath);
            var configSo = new SerializedObject(config);
            SerializedProperty sentinels = configSo.FindProperty("_sentinelRobots");
            if (sentinels.arraySize == 0)
            {
                string[] names = { "PistolRobot", "RifleRobot" };
                sentinels.arraySize = names.Length;
                for (int i = 0; i < names.Length; i++)
                {
                    sentinels.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(names[i]));
                }

                configSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(config);
        }

        // 가방 칸의 등급 색과 첫 출격 안내. 대기실과 레이드 캔버스 모두.
        private static void EnsureTensionUi(GameObject canvasObject, GameObject player, RaidManager raid)
        {
            var inventoryUi = canvasObject.GetComponent<InventoryUI>();
            if (inventoryUi != null)
            {
                SetReferenceIfEmpty(inventoryUi, "_grades", AssetDatabase.LoadAssetAtPath<ItemGradeTable>(ItemGradesPath));
            }

            EnsureComponent(canvasObject, out GuideUI guide);
            SetReferenceIfEmpty(guide, "_inventory", player.GetComponent<PlayerInventory>());
            if (raid != null)
            {
                SetReferenceIfEmpty(guide, "_raid", raid);
            }
        }

        // 정거장 씬: 센티널 수색
        private static void EnsureTensionStation(GameObject player, RaidManager raid, int wallLayer)
        {
            GameObject canvas = GameObject.Find("UI");
            if (canvas != null)
            {
                EnsureTensionUi(canvas, player, raid);
            }

            GameObject sentinelObject = GameObject.Find("Sentinel");
            if (sentinelObject == null)
            {
                sentinelObject = new GameObject("Sentinel");
            }

            if (EnsureComponent(sentinelObject, out SentinelDirector sentinel))
            {
                var so = new SerializedObject(sentinel);
                so.FindProperty("_wallMask").intValue = 1 << wallLayer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(sentinel, "_config", AssetDatabase.LoadAssetAtPath<RaidConfig>(RaidConfigPath));
            SetReferenceIfEmpty(sentinel, "_raid", raid);
            SetReferenceIfEmpty(sentinel, "_player", player.GetComponent<Health>());
            SetReferenceIfEmpty(sentinel, "_grid", Object.FindAnyObjectByType<WallGrid>());
            SetReferenceIfEmpty(sentinel, "_robotPrefab", AssetDatabase.LoadAssetAtPath<Robot>(RobotPrefabPath));
            if (canvas != null)
            {
                SetReferenceIfEmpty(sentinel, "_canvas", canvas.GetComponent<Canvas>());
            }
        }
    }
}
