using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 센티널 수색 (기획서 20.1). 오래 머물수록 위험이 커지게 해서 "나갈까, 하나 더 챙길까"의 고민을 만든다.
    // 일정 시간이 지나면 알림을 띄우고, 그 뒤로 정해진 간격마다 플레이어 근처의 보이지 않는 곳에 로봇을 내려보낸다.
    public class SentinelDirector : MonoBehaviour
    {
        // 내려올 자리를 무작위로 고를 때 시도하는 횟수
        private const int PlaceAttempts = 200;

        [SerializeField] private RaidConfig _config;
        [SerializeField] private RaidManager _raid;
        [SerializeField] private Health _player;
        [SerializeField] private WallGrid _grid;
        [SerializeField] private Robot _robotPrefab;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private LayerMask _wallMask;
        [SerializeField, Tooltip("초. 알림이 떠 있는 시간")] private float _bannerTime = 3.5f;

        private readonly List<Vector2> _path = new List<Vector2>();
        private bool _started;
        private float _nextWave;
        private int _spawned;
        private RectTransform _banner;
        private Text _bannerText;
        private float _bannerLeft;

        // 지금까지 내려온 로봇 수 (시험용)
        public int Spawned => _spawned;

        private void Awake()
        {
            BuildBanner();
        }

        private void Update()
        {
            UpdateBanner();
            if (_config == null || _raid == null || _player == null || _robotPrefab == null || _config.SentinelRobots == null
                || _config.SentinelRobots.Length == 0 || _raid.Outcome != RaidOutcome.InProgress || _player.IsDead)
            {
                return;
            }

            float time = _raid.ElapsedTime;
            if (!_started)
            {
                if (time < _config.SentinelStartSeconds)
                {
                    return;
                }

                _started = true;
                _nextWave = time;
                Announce("센티널 수색 시작 — 증원이 내려온다");
            }

            if (time < _nextWave || _spawned >= _config.SentinelMaxRobots)
            {
                return;
            }

            _nextWave = time + Mathf.Max(5f, _config.SentinelInterval);
            int count = Mathf.Min(_config.SentinelRobotsPerWave, _config.SentinelMaxRobots - _spawned);
            int landed = 0;
            for (int i = 0; i < count; i++)
            {
                if (SpawnOne())
                {
                    landed++;
                }
            }

            // 첫 무리는 시작 알림과 함께 오므로 따로 알리지 않는다.
            if (landed > 0 && _spawned > landed)
            {
                Announce($"센티널 증원 {landed}기");
            }
        }

        private bool SpawnOne()
        {
            Vector2 player = _player.transform.position;
            if (!TryFindLanding(player, out Vector2 landing))
            {
                return false;
            }

            RobotData data = _config.SentinelRobots[Random.Range(0, _config.SentinelRobots.Length)];
            if (data == null)
            {
                return false;
            }

            // 순찰은 내려온 자리와 플레이어가 있던 곳을 오간다. 놓친 뒤에도 그 둘레를 뒤지게 하려는 것이다.
            var group = new GameObject($"Sentinel {_spawned + 1}").transform;
            group.SetParent(transform, false);
            var start = new GameObject("Waypoint 0").transform;
            start.SetParent(group, false);
            start.position = landing;
            var target = new GameObject("Waypoint 1").transform;
            target.SetParent(group, false);
            target.position = player;

            Robot robot = Instantiate(_robotPrefab, landing, Quaternion.identity, group);
            robot.name = data.DisplayName;
            robot.Initialize(data, new[] { start, target }, _grid, _player);
            robot.Hunt(player);
            _spawned++;
            Debug.Log($"[Sentinel] {data.DisplayName} 증원: {landing} (플레이어 {player})");
            return true;
        }

        // 플레이어에게서 정해진 거리만큼 떨어지고, 벽 너머라 보이지 않고, 걸어서 닿을 수 있는 바닥 칸
        private bool TryFindLanding(Vector2 player, out Vector2 landing)
        {
            float min = _config.SentinelMinDistance;
            float max = Mathf.Max(min + 1f, _config.SentinelMaxDistance);
            for (int i = 0; i < PlaceAttempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(min, max);
                Vector2 cell = new Vector2(Mathf.Floor(player.x + offset.x) + 0.5f, Mathf.Floor(player.y + offset.y) + 0.5f);
                if (Physics2D.OverlapCircle(cell, 0.45f, _wallMask) != null)
                {
                    continue;
                }

                if (Physics2D.Linecast(player, cell, _wallMask).collider == null)
                {
                    continue;
                }

                // 길이 없거나 너무 돌아가는 곳(다른 배 깊숙이)은 쓰지 않는다.
                if (!_grid.FindPath(cell, player, _path) || PathLength(cell) > max * 1.8f)
                {
                    continue;
                }

                landing = cell;
                return true;
            }

            landing = Vector2.zero;
            return false;
        }

        private float PathLength(Vector2 from)
        {
            float length = 0f;
            Vector2 previous = from;
            foreach (Vector2 point in _path)
            {
                length += Vector2.Distance(previous, point);
                previous = point;
            }

            return length;
        }

        private void Announce(string message)
        {
            if (_bannerText == null)
            {
                return;
            }

            _bannerText.text = message;
            _bannerLeft = _bannerTime;
            Sfx.Play(SfxId.SentinelAlarm);
        }

        private void UpdateBanner()
        {
            if (_banner == null)
            {
                return;
            }

            _bannerLeft -= Time.deltaTime;
            _banner.gameObject.SetActive(_bannerLeft > 0f);
        }

        // 우주선 이변 알림(위쪽 110)과 겹치지 않게 그 아래에 띄운다.
        private void BuildBanner()
        {
            if (_canvas == null)
            {
                return;
            }

            _banner = UiBuilder.Rect("SentinelBanner", _canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f),
                new Vector2(680f, 64f));
            UiBuilder.Panel(_banner, new Color(0.3f, 0.04f, 0.04f, 0.92f), new Color(1f, 0.25f, 0.2f));
            _bannerText = UiBuilder.Text(string.Empty, _banner, 30, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.8f));
            _bannerText.fontStyle = FontStyle.Bold;
            _banner.gameObject.SetActive(false);
        }
    }
}
