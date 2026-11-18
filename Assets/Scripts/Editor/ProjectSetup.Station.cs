using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace.EditorTools
{
    // 맵 확장 (기획서 15장, 2026-11-18 사용자 결정). 옛 60×40 맵이면 맵·로봇·컨테이너·포드를 새 배치로 바꾼다.
    // 이미 새 크기면 아무것도 지우지 않는다 (Scene 뷰에서 고친 것을 두려고).
    public static partial class ProjectSetup
    {
        private static bool IsOldStation()
        {
            GameObject grid = GameObject.Find("Grid");
            if (grid == null)
            {
                return false;
            }

            Tilemap walls = grid.transform.Find("Walls").GetComponent<Tilemap>();
            walls.CompressBounds();
            return walls.cellBounds.size.x < StationLayout.Width;
        }

        // 새 배치로 다시 만들 것들을 지운다. 지운 뒤에는 EnsureScene의 "없으면 만들기"가 새로 만든다.
        private static void ClearOldStation()
        {
            foreach (string name in new[] { "Grid", "Robots", "Containers", "EscapePods", "Zones", "PodLever" })
            {
                GameObject target = GameObject.Find(name);
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(StationLayout.SpawnX, StationLayout.SpawnY, 0f);
            }

            GameObject camera = GameObject.Find("Main Camera");
            if (camera != null)
            {
                camera.transform.position = new Vector3(StationLayout.SpawnX, StationLayout.SpawnY, -10f);
            }
        }

        private static void EnsureZones()
        {
            if (GameObject.Find("Zones") != null)
            {
                return;
            }

            var parent = new GameObject("Zones");
            foreach (StationLayout.Zone zone in StationLayout.Zones)
            {
                var zoneObject = new GameObject(zone.Id);
                zoneObject.transform.SetParent(parent.transform, false);
                var area = zoneObject.AddComponent<ZoneArea>();
                var so = new SerializedObject(area);
                so.FindProperty("_id").stringValue = zone.Id;
                so.FindProperty("_displayName").stringValue = zone.Name;
                so.FindProperty("_area").rectValue = new Rect(zone.Area.x, zone.Area.y, zone.Area.width, zone.Area.height);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildStationRobots(Transform parent, WallGrid grid)
        {
            foreach ((string zone, StationLayout.Patrol[] robots) in StationLayout.Robots)
            {
                var list = new (string dataName, Vector2[] waypoints)[robots.Length];
                for (int i = 0; i < robots.Length; i++)
                {
                    list[i] = (robots[i].Robot, robots[i].Points);
                }

                BuildRobotZone(parent, zone + "Zone", grid, list);
            }
        }

        private static void BuildStationContainers(Transform parent)
        {
            var normal = AssetDatabase.LoadAssetAtPath<GameObject>(ContainerPrefabPath);
            var research = AssetDatabase.LoadAssetAtPath<GameObject>(ResearchContainerPrefabPath);
            foreach (Vector2 position in StationLayout.NormalContainers)
            {
                var container = (GameObject)PrefabUtility.InstantiatePrefab(normal, parent);
                container.transform.position = position;
            }

            foreach (Vector2 position in StationLayout.ResearchContainers)
            {
                var container = (GameObject)PrefabUtility.InstantiatePrefab(research, parent);
                container.transform.position = position;
            }

            // 동력 구역의 연구 컨테이너는 동력 셀이 들 수 있는 표를 쓴다.
            LootTable powerTable = LoadLoot("PowerResearchLoot");
            foreach (Vector2 position in StationLayout.PowerResearchContainers)
            {
                var container = (GameObject)PrefabUtility.InstantiatePrefab(research, parent);
                container.transform.position = position;
                container.name = "PowerResearchContainer";
                var so = new SerializedObject(container.GetComponent<Lootable>());
                so.FindProperty("_table").objectReferenceValue = powerTable;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildStationPods(Transform parent, Sprite square)
        {
            CreatePod(parent, "WestPod", StationLayout.WestPod, square);
            CreatePod(parent, "EastPod", StationLayout.EastPod, square);
            CreatePod(parent, "LabPod", StationLayout.LabPod, square);

            var lab = parent.Find("LabPod").GetComponent<EscapePod>();
            var so = new SerializedObject(lab);
            so.FindProperty("_requiresLever").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 연구소 서버실의 비상 레버. 붉은 손잡이 그림, 위에 E 글자와 진행 바.
        private static void EnsurePodLever(GameObject player)
        {
            if (GameObject.Find("PodLever") != null)
            {
                return;
            }

            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            var lever = new GameObject("PodLever");
            lever.transform.position = StationLayout.LabLever;
            var renderer = lever.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.sortingOrder = 1;
            lever.transform.localScale = new Vector3(0.35f, 0.7f, 1f);

            var prompt = new GameObject("Prompt");
            prompt.transform.SetParent(lever.transform, false);
            prompt.transform.localScale = new Vector3(1f / 0.35f, 1f / 0.7f, 1f);
            prompt.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            var text = new GameObject("E").AddComponent<TextMesh>();
            text.transform.SetParent(prompt.transform, false);
            text.text = "E";
            text.anchor = TextAnchor.MiddleCenter;
            text.characterSize = 0.12f;
            text.fontSize = 48;
            text.color = new Color(1f, 0.85f, 0.4f);
            text.GetComponent<MeshRenderer>().sortingOrder = 20;

            var barBack = new GameObject("BarBack").AddComponent<SpriteRenderer>();
            barBack.transform.SetParent(prompt.transform, false);
            barBack.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            barBack.transform.localScale = new Vector3(1.2f, 0.14f, 1f);
            barBack.sprite = square;
            barBack.color = new Color(0.1f, 0.08f, 0.15f, 0.9f);
            barBack.sortingOrder = 20;

            var fillPivot = new GameObject("FillPivot");
            fillPivot.transform.SetParent(prompt.transform, false);
            fillPivot.transform.localPosition = new Vector3(-0.6f, -0.45f, 0f);
            var fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(fillPivot.transform, false);
            fill.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            fill.transform.localScale = new Vector3(1.2f, 0.14f, 1f);
            fill.sprite = square;
            fill.color = new Color(1f, 0.85f, 0.3f);
            fill.sortingOrder = 21;
            prompt.SetActive(false);

            var component = lever.AddComponent<PodLever>();
            var so = new SerializedObject(component);
            so.FindProperty("_pod").objectReferenceValue = GameObject.Find("EscapePods").transform.Find("LabPod").GetComponent<EscapePod>();
            so.FindProperty("_input").objectReferenceValue = player.GetComponent<PlayerInputReader>();
            so.FindProperty("_controller").objectReferenceValue = player.GetComponent<PlayerController>();
            so.FindProperty("_renderer").objectReferenceValue = renderer;
            so.FindProperty("_prompt").objectReferenceValue = prompt;
            so.FindProperty("_progressFill").objectReferenceValue = fillPivot.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 포드가 3곳이 되었으므로 전체 맵과 RaidManager의 포드 목록을 다시 채운다.
        private static void RefreshPodLists(RaidManager raid)
        {
            EscapePod[] pods = GameObject.Find("EscapePods").GetComponentsInChildren<EscapePod>();
            foreach (Object target in new Object[] { raid, Object.FindFirstObjectByType<MapUI>() })
            {
                if (target == null)
                {
                    continue;
                }

                var so = new SerializedObject(target);
                SerializedProperty list = so.FindProperty("_pods");
                if (list.arraySize == pods.Length)
                {
                    continue;
                }

                list.arraySize = pods.Length;
                for (int i = 0; i < pods.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = pods[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
