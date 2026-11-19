using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace
{
    // 판마다 로봇과 컨테이너의 위치를 섞는다 (기획서 19.2, 2026-11-19 사용자 결정: 규칙으로 자동 배치).
    // 수와 종류는 씬에 놓인 그대로 두고 자리만 바꾼다. 맞는 자리가 없으면 원래 자리에 둔다.
    // 로봇 길찾기(WallGrid)가 Awake에서 컨테이너 자리를 막힌 칸으로 읽으므로 그보다 먼저 옮긴다.
    [DefaultExecutionOrder(-1000)]
    public class RaidLayoutRandomizer : MonoBehaviour
    {
        [SerializeField] private Tilemap _walls;
        [SerializeField, Tooltip("컨테이너들의 부모. 이 아래의 Lootable을 섞는다")] private Transform _containers;
        [SerializeField, Tooltip("0이면 판마다 무작위. 같은 판을 다시 보려면 로그에 찍힌 시드를 넣는다")] private int _fixedSeed;

        [Header("로봇 (칸)")]
        [SerializeField, Tooltip("일반 로봇의 시작점이 출입구에서 떨어진 최소 거리 (구역 안 걸음 수)")] private int _robotDoorDistance = 10;
        [SerializeField, Tooltip("네임드 무리의 시작점이 출입구에서 떨어진 최소 거리")] private int _namedDoorDistance = 12;
        [SerializeField, Tooltip("무리끼리 시작점 사이의 최소 거리")] private float _groupSpacing = 6f;
        [SerializeField, Tooltip("순찰점 사이 거리의 최소·최대")] private Vector2 _patrolStep = new Vector2(6f, 15f);
        [SerializeField, Tooltip("저격 로봇 앞이 트여 있어야 하는 거리")] private float _sniperClearSight = 12f;

        [Header("컨테이너 (칸)")]
        [SerializeField] private float _containerSpacing = 4f;
        [SerializeField] private int _containerDoorDistance = 3;
        [SerializeField, Tooltip("연구 컨테이너가 출입구에서 떨어진 최소 거리")] private int _researchDoorDistance = 10;
        [SerializeField, Tooltip("포드·레버 둘레에 비워 두는 거리")] private float _fixedObjectClearance = 2.5f;

        private static readonly Vector2Int[] Neighbours4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        private System.Random _random;
        private readonly List<Vector2> _fixedObjects = new List<Vector2>();

        // 이번 판의 시드. 테스트와 로그에서 본다.
        public static int LastSeed { get; private set; }

        private void Awake()
        {
            int seed = _fixedSeed != 0 ? _fixedSeed : System.Environment.TickCount & 0x7fffffff;
            LastSeed = seed;
            _random = new System.Random(seed);
            Debug.Log($"[RaidLayout] 시드 {seed}");

            foreach (EscapePod pod in FindObjectsByType<EscapePod>(FindObjectsSortMode.None))
            {
                _fixedObjects.Add(pod.transform.position);
            }

            foreach (PodLever lever in FindObjectsByType<PodLever>(FindObjectsSortMode.None))
            {
                _fixedObjects.Add(lever.transform.position);
            }

            // 컨테이너가 아닌 고정 루팅 오브젝트 (부서진 로봇 개, 함선 단말기) 둘레도 비워 둔다.
            foreach (Lootable lootable in FindObjectsByType<Lootable>(FindObjectsSortMode.None))
            {
                if (_containers == null || !lootable.transform.IsChildOf(_containers))
                {
                    _fixedObjects.Add(lootable.transform.position);
                }
            }

            // ZoneArea는 OnEnable에서 목록에 오르는데, 이 Awake가 그보다 먼저일 수 있어 직접 찾는다.
            foreach (ZoneArea zone in FindObjectsByType<ZoneArea>(FindObjectsSortMode.None))
            {
                ShuffleZone(zone.Area);
            }

            // 옮긴 컨테이너의 충돌체를 길찾기의 칸 읽기 전에 물리에 반영한다.
            Physics2D.SyncTransforms();
        }

        // ---------- 한 구역 ----------

        private sealed class ZoneCells
        {
            public RectInt Rect;
            public readonly Dictionary<Vector2Int, int> DoorDistance = new Dictionary<Vector2Int, int>();
            public readonly List<Vector2Int> Reachable = new List<Vector2Int>();
            public Vector2 Center;
        }

        private void ShuffleZone(Rect area)
        {
            var rect = new RectInt(Mathf.FloorToInt(area.xMin), Mathf.FloorToInt(area.yMin), Mathf.RoundToInt(area.width), Mathf.RoundToInt(area.height));
            ZoneCells zone = ReadZone(rect);
            if (zone.Reachable.Count == 0)
            {
                return;
            }

            var starts = new List<Vector2>();
            foreach (RobotZone group in FindObjectsByType<RobotZone>(FindObjectsSortMode.None))
            {
                if (group.Spawns.Length == 0 || group.Spawns[0].Waypoints.Length == 0 || !area.Contains(group.Spawns[0].Waypoints[0].position))
                {
                    continue;
                }

                if (group.EscortsFollowFirst)
                {
                    PlaceGroup(zone, group, starts);
                }
                else
                {
                    foreach (RobotZone.Spawn spawn in group.Spawns)
                    {
                        PlaceRobot(zone, spawn, _robotDoorDistance, starts);
                    }
                }
            }

            PlaceContainers(zone, area);
        }

        // 구역 안의 바닥 칸과, 출입구에서 구역 안으로 걸어 들어간 거리를 센다.
        private ZoneCells ReadZone(RectInt rect)
        {
            var zone = new ZoneCells { Rect = rect, Center = rect.center };
            var queue = new Queue<Vector2Int>();
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!IsFloor(cell))
                    {
                        continue;
                    }

                    // 출입구: 구역 밖의 바닥과 맞닿은 칸
                    foreach (Vector2Int step in Neighbours4)
                    {
                        Vector2Int next = cell + step;
                        if (!rect.Contains(next) && IsFloor(next) && !zone.DoorDistance.ContainsKey(cell))
                        {
                            zone.DoorDistance[cell] = 0;
                            queue.Enqueue(cell);
                        }
                    }
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                zone.Reachable.Add(cell);
                foreach (Vector2Int step in Neighbours4)
                {
                    Vector2Int next = cell + step;
                    if (rect.Contains(next) && IsFloor(next) && !zone.DoorDistance.ContainsKey(next))
                    {
                        zone.DoorDistance[next] = zone.DoorDistance[cell] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return zone;
        }

        // ---------- 로봇 ----------

        private void PlaceRobot(ZoneCells zone, RobotZone.Spawn spawn, int doorDistance, List<Vector2> starts)
        {
            Transform[] points = spawn.Waypoints;
            bool fixedSniper = spawn.Data != null && spawn.Data.PatrolSpeed <= 0f && spawn.Data.ChaseSpeed <= 0f;
            Vector2Int? start = PickStart(zone, doorDistance, starts, fixedSniper);
            if (start == null)
            {
                return;
            }

            starts.Add(Center(start.Value));
            points[0].position = Center(start.Value);
            if (fixedSniper)
            {
                // 저격 로봇은 움직이지 않고, 두 번째 점은 훑는 방향의 중심만 정한다. 구역 안쪽을 본다.
                for (int i = 1; i < points.Length; i++)
                {
                    points[i].position = zone.Center;
                }

                return;
            }

            Vector2Int previous = start.Value;
            for (int i = 1; i < points.Length; i++)
            {
                Vector2Int? next = PickPatrolPoint(zone, previous);
                if (next == null)
                {
                    break;
                }

                points[i].position = Center(next.Value);
                previous = next.Value;
            }
        }

        // 네임드 무리: 우두머리만 자리를 정하고, 호위는 그 둘레에서 시작해 따라다닌다.
        private void PlaceGroup(ZoneCells zone, RobotZone group, List<Vector2> starts)
        {
            RobotZone.Spawn leader = group.Spawns[0];
            Vector2 before = leader.Waypoints[0].position;
            PlaceRobot(zone, leader, _namedDoorDistance, starts);
            Vector2 start = leader.Waypoints[0].position;
            if (start == before)
            {
                return;
            }

            Vector2[] offsets = { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) };
            for (int i = 1; i < group.Spawns.Length; i++)
            {
                Vector2 at = start + offsets[(i - 1) % offsets.Length];
                Vector2Int cell = Vector2Int.FloorToInt(at);
                foreach (Transform point in group.Spawns[i].Waypoints)
                {
                    point.position = IsFloor(cell) ? Center(cell) : start;
                }
            }
        }

        // 출입구에서 충분히 떨어지고 다른 무리와 겹치지 않는 시작 칸. 못 찾으면 거리 조건을 반씩 풀어 본다.
        private Vector2Int? PickStart(ZoneCells zone, int doorDistance, List<Vector2> starts, bool sniper)
        {
            for (int relax = 0; relax < 3; relax++)
            {
                int minDoor = doorDistance >> relax;
                float spacing = _groupSpacing / (1 << relax);
                var candidates = new List<Vector2Int>();
                foreach (Vector2Int cell in zone.Reachable)
                {
                    if (zone.DoorDistance[cell] < minDoor || !IsOpen(cell) || TooClose(Center(cell), starts, spacing))
                    {
                        continue;
                    }

                    if (sniper && !HasClearSight(cell, zone.Center, _sniperClearSight / (1 << relax)))
                    {
                        continue;
                    }

                    candidates.Add(cell);
                }

                if (candidates.Count > 0)
                {
                    return candidates[_random.Next(candidates.Count)];
                }
            }

            return null;
        }

        private Vector2Int? PickPatrolPoint(ZoneCells zone, Vector2Int from)
        {
            var candidates = new List<Vector2Int>();
            foreach (Vector2Int cell in zone.Reachable)
            {
                float distance = Vector2Int.Distance(cell, from);
                if (distance >= _patrolStep.x && distance <= _patrolStep.y && IsOpen(cell))
                {
                    candidates.Add(cell);
                }
            }

            return candidates.Count > 0 ? candidates[_random.Next(candidates.Count)] : (Vector2Int?)null;
        }

        // ---------- 컨테이너 ----------

        private void PlaceContainers(ZoneCells zone, Rect area)
        {
            if (_containers == null)
            {
                return;
            }

            var normal = new List<Lootable>();
            var research = new List<Lootable>();
            foreach (Lootable lootable in _containers.GetComponentsInChildren<Lootable>())
            {
                if (!area.Contains(lootable.transform.position))
                {
                    continue;
                }

                (lootable.name.StartsWith("Research") ? research : normal).Add(lootable);
            }

            // 조건이 까다로운 연구 컨테이너부터 자리를 잡는다.
            var placed = new List<Vector2>();
            foreach (Lootable lootable in research)
            {
                Place(zone, lootable, _researchDoorDistance, placed);
            }

            foreach (Lootable lootable in normal)
            {
                Place(zone, lootable, _containerDoorDistance, placed);
            }
        }

        private void Place(ZoneCells zone, Lootable lootable, int doorDistance, List<Vector2> placed)
        {
            var candidates = new List<Vector2Int>();
            foreach (Vector2Int cell in zone.Reachable)
            {
                Vector2 at = Center(cell);
                if (zone.DoorDistance[cell] < doorDistance || !AgainstWall(cell) || TooClose(at, placed, _containerSpacing)
                    || TooClose(at, _fixedObjects, _fixedObjectClearance))
                {
                    continue;
                }

                candidates.Add(cell);
            }

            if (candidates.Count == 0)
            {
                placed.Add(lootable.transform.position);
                return;
            }

            Vector2 position = Center(candidates[_random.Next(candidates.Count)]);
            lootable.transform.position = new Vector3(position.x, position.y, lootable.transform.position.z);
            placed.Add(position);
        }

        // 한쪽만 벽에 붙고 나머지 세 방향이 트인 칸. 한 칸 폭 통로를 막지 않는다.
        private bool AgainstWall(Vector2Int cell)
        {
            int walls = 0;
            foreach (Vector2Int step in Neighbours4)
            {
                if (!IsFloor(cell + step))
                {
                    walls++;
                }
            }

            return walls == 1;
        }

        // ---------- 칸 도구 ----------

        private bool IsFloor(Vector2Int cell) => !_walls.HasTile(new Vector3Int(cell.x, cell.y, 0));

        // 사방이 바닥인 칸. 로봇이 벽에 끼어 시작하지 않게 한다.
        private bool IsOpen(Vector2Int cell)
        {
            foreach (Vector2Int step in Neighbours4)
            {
                if (!IsFloor(cell + step))
                {
                    return false;
                }
            }

            return IsFloor(cell);
        }

        private bool HasClearSight(Vector2Int cell, Vector2 toward, float distance)
        {
            Vector2 from = Center(cell);
            Vector2 direction = toward - from;
            if (direction.sqrMagnitude < 0.01f)
            {
                return false;
            }

            direction.Normalize();
            for (float d = 1f; d <= distance; d += 0.5f)
            {
                if (!IsFloor(Vector2Int.FloorToInt(from + direction * d)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TooClose(Vector2 at, List<Vector2> others, float spacing)
        {
            foreach (Vector2 other in others)
            {
                if ((other - at).sqrMagnitude < spacing * spacing)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 Center(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.5f);
    }
}
