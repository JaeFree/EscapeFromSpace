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

            public RobotData Data => _data;
            public Transform[] Waypoints => _waypoints;
        }

        [SerializeField] private Robot _robotPrefab;
        [SerializeField] private WallGrid _grid;
        [SerializeField, Tooltip("로봇이 찾고 공격할 대상")] private Health _player;
        [SerializeField] private Spawn[] _spawns;

        // WallGrid가 Awake에서 벽을 읽은 뒤에 로봇을 만든다.
        private void Start()
        {
            foreach (Spawn spawn in _spawns)
            {
                if (spawn.Data == null || spawn.Waypoints == null || spawn.Waypoints.Length == 0)
                {
                    continue;
                }

                Robot robot = Instantiate(_robotPrefab, spawn.Waypoints[0].position, Quaternion.identity, transform);
                robot.name = spawn.Data.DisplayName;
                robot.Initialize(spawn.Data, spawn.Waypoints, _grid, _player);
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
