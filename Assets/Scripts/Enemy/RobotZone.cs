using System;
using UnityEngine;

namespace EscapeFromSpace
{
    // 한 구역의 로봇 배치 (기획서 9.1). 구역마다 로봇 수와 종류를 Inspector에서 넣고 뺀다.
    public class RobotZone : MonoBehaviour
    {
        [Serializable]
        public class Spawn
        {
            [SerializeField] private RobotData _data;
            [SerializeField, Tooltip("첫 번째가 시작 위치. 순서대로 돌며 순찰한다")] private Transform[] _waypoints;

            public Spawn()
            {
            }

            public Spawn(RobotData data, Transform[] waypoints)
            {
                _data = data;
                _waypoints = waypoints;
            }

            public RobotData Data => _data;
            public Transform[] Waypoints => _waypoints;

            // 에일리언 습격 (기획서 19.4): 레이드를 시작할 때 일반 로봇을 에일리언으로 바꾼다.
            public void SetData(RobotData data)
            {
                _data = data;
            }
        }

        [SerializeField] private Robot _robotPrefab;
        [SerializeField] private WallGrid _grid;
        [SerializeField, Tooltip("로봇이 찾고 공격할 대상")] private Health _player;
        [SerializeField] private Spawn[] _spawns;

        [Header("네임드 무리 (기획서 15.2)")]
        [SerializeField, Tooltip("켜면 이 무리는 아래 구역 안에서만 보고 듣고 쫓는다")] private bool _confineToZone;
        [SerializeField] private ZoneArea _zone;
        [SerializeField, Tooltip("켜면 첫 번째 로봇이 우두머리이고 나머지는 그 둘레를 따라다닌다")] private bool _escortsFollowFirst;

        // 판마다 위치를 섞는 RaidLayoutRandomizer가 읽고 웨이포인트를 옮긴다 (기획서 19.2).
        public Spawn[] Spawns => _spawns;

        // 보안 경계 (기획서 19.4): 같은 종류의 로봇을 더 놓는다. 자리는 첫 로봇 곁이고, 위치 섞기가 다시 정한다.
        public void AddSpawns(RobotData data, int count)
        {
            if (data == null || count <= 0 || _spawns.Length == 0)
            {
                return;
            }

            var list = new System.Collections.Generic.List<Spawn>(_spawns);
            Vector3 origin = _spawns[0].Waypoints[0].position;
            for (int i = 0; i < count; i++)
            {
                var route = new GameObject($"Extra {data.name} {i + 1}").transform;
                route.SetParent(transform, false);
                var points = new Transform[2];
                for (int j = 0; j < points.Length; j++)
                {
                    points[j] = new GameObject($"Waypoint {j + 1}").transform;
                    points[j].SetParent(route, false);
                    points[j].position = origin;
                }

                list.Add(new Spawn(data, points));
            }

            _spawns = list.ToArray();
        }
        public bool EscortsFollowFirst => _escortsFollowFirst;

        // WallGrid가 Awake에서 벽을 읽은 뒤에 로봇을 만든다.
        private void Start()
        {
            Robot leader = null;
            for (int i = 0; i < _spawns.Length; i++)
            {
                Spawn spawn = _spawns[i];
                if (spawn.Data == null || spawn.Waypoints == null || spawn.Waypoints.Length == 0)
                {
                    continue;
                }

                Robot robot = Instantiate(_robotPrefab, spawn.Waypoints[0].position, Quaternion.identity, transform);
                robot.name = spawn.Data.DisplayName;
                robot.Initialize(spawn.Data, spawn.Waypoints, _grid, _player);
                if (_confineToZone)
                {
                    robot.ConfineTo(_zone);
                }

                if (_escortsFollowFirst)
                {
                    if (leader == null)
                    {
                        leader = robot;
                    }
                    else
                    {
                        // 우두머리 둘레에 고르게 흩어져 따라간다.
                        float angle = (i - 1) * 2f * Mathf.PI / Mathf.Max(1, _spawns.Length - 1);
                        robot.Follow(leader, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.6f);
                    }
                }
            }
        }

        // 로봇은 Play 중에만 생기므로 Scene 뷰에서 배치를 볼 수 있게 그린다.
        private void OnDrawGizmos()
        {
            if (_spawns == null)
            {
                return;
            }

            foreach (Spawn spawn in _spawns)
            {
                Transform[] waypoints = spawn.Waypoints;
                if (waypoints == null || waypoints.Length == 0 || waypoints[0] == null)
                {
                    continue;
                }

                Gizmos.color = spawn.Data != null ? spawn.Data.Color : Color.white;
                Gizmos.DrawWireCube(waypoints[0].position, new Vector3(0.8f, 0.8f, 0f));

                for (int i = 0; i < waypoints.Length; i++)
                {
                    Transform next = waypoints[(i + 1) % waypoints.Length];
                    if (waypoints[i] == null || next == null)
                    {
                        continue;
                    }

                    Gizmos.DrawSphere(waypoints[i].position, 0.12f);
                    Gizmos.DrawLine(waypoints[i].position, next.position);
                }
            }
        }
    }
}
