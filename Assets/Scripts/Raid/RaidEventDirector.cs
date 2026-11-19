using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 레이드를 시작할 때 이번 판의 우주선 이변을 적용한다 (기획서 19.4).
    // 로봇을 더하거나 바꾸는 일은 위치 섞기(RaidLayoutRandomizer)와 로봇 생성(RobotZone.Start)보다 먼저 해야 해서 가장 먼저 돈다.
    [DefaultExecutionOrder(-1100)]
    public class RaidEventDirector : MonoBehaviour
    {
        [SerializeField] private RaidConfig _config;
        [SerializeField] private Transform _player;
        [SerializeField] private Transform _containers;
        [SerializeField, Tooltip("이변 알림을 띄울 화면")] private Canvas _canvas;
        [SerializeField, Tooltip("초. 이변이 생긴 배에 들어갔을 때 알림이 떠 있는 시간")] private float _bannerTime = 3f;

        private readonly Dictionary<string, ZoneArea> _zones = new Dictionary<string, ZoneArea>();
        private readonly HashSet<string> _announced = new HashSet<string>();
        private RectTransform _banner;
        private Text _bannerText;
        private float _bannerLeft;
        private SpriteRenderer _darkness;

        private void Awake()
        {
            // 대기실에서 뽑아 둔 것을 쓴다. 씬을 바로 Play했으면 여기서 뽑는다.
            RaidEventPlan plan = RaidEvents.Next ?? RaidEvents.Roll(_config);
            RaidEvents.Next = null;
            RaidEvents.Current = plan;
            Robot.ConfigureEvents(_config);
            if (plan.Kind != RaidEventKind.None)
            {
                Debug.Log($"[RaidEvent] {plan.KindName}: {string.Join(", ", plan.ShipIds)}");
            }

            foreach (ZoneArea zone in FindObjectsByType<ZoneArea>(FindObjectsSortMode.None))
            {
                _zones[zone.Id] = zone;
            }

            foreach (string id in plan.ShipIds)
            {
                if (!_zones.TryGetValue(id, out ZoneArea zone))
                {
                    continue;
                }

                RobotZone robots = FindRegularRobots(zone);
                if (plan.Kind == RaidEventKind.SecurityAlert && robots != null && robots.Spawns.Length > 0)
                {
                    robots.AddSpawns(robots.Spawns[0].Data, _config.AlertExtraRobots);
                }
                else if (plan.Kind == RaidEventKind.AlienInvasion && robots != null && _config.Invaders != null && _config.Invaders.Length > 0)
                {
                    for (int i = 0; i < robots.Spawns.Length; i++)
                    {
                        robots.Spawns[i].SetData(_config.Invaders[i % _config.Invaders.Length]);
                    }
                }

                if (_containers != null)
                {
                    foreach (Lootable lootable in _containers.GetComponentsInChildren<Lootable>())
                    {
                        if (zone.Contains(lootable.transform.position))
                        {
                            lootable.LootMultiplier = 1f + _config.EventLootBonus;
                        }
                    }
                }
            }

            BuildBanner();
            if (plan.Kind == RaidEventKind.Blackout)
            {
                BuildDarkness();
            }
        }

        // 구역의 일반 로봇 무리 (네임드 무리가 아닌 것)
        private static RobotZone FindRegularRobots(ZoneArea zone)
        {
            foreach (RobotZone group in FindObjectsByType<RobotZone>(FindObjectsSortMode.None))
            {
                if (!group.EscortsFollowFirst && group.Spawns.Length > 0 && group.Spawns[0].Waypoints.Length > 0
                    && zone.Contains(group.Spawns[0].Waypoints[0].position))
                {
                    return group;
                }
            }

            return null;
        }

        private void Update()
        {
            RaidEventPlan plan = RaidEvents.Current;
            if (plan == null || plan.Kind == RaidEventKind.None || _player == null)
            {
                return;
            }

            ZoneArea here = ZoneArea.At(_player.position);
            if (here != null && plan.Affects(here.Id) && _announced.Add(here.Id))
            {
                _bannerText.text = $"{plan.KindName} — {here.DisplayName}";
                _bannerLeft = _bannerTime;
            }

            _bannerLeft -= Time.deltaTime;
            _banner.gameObject.SetActive(_bannerLeft > 0f);

            if (_darkness != null)
            {
                // 정전된 배 안에 있을 때만 어둡게 덮는다. 들어가고 나올 때 천천히 바뀐다.
                bool dark = here != null && RaidEvents.IsBlackout(here.Id);
                _darkness.transform.position = new Vector3(_player.position.x, _player.position.y, 0f);
                Color color = _darkness.color;
                color.a = Mathf.MoveTowards(color.a, dark ? 1f : 0f, Time.deltaTime * 2f);
                _darkness.color = color;
                _darkness.enabled = color.a > 0.01f;
            }
        }

        private void BuildBanner()
        {
            if (_canvas == null)
            {
                return;
            }

            _banner = UiBuilder.Rect("EventBanner", _canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f),
                new Vector2(620f, 64f));
            UiBuilder.Panel(_banner, new Color(0.25f, 0.05f, 0.05f, 0.9f), UiTheme.PanelBorder);
            _bannerText = UiBuilder.Text(string.Empty, _banner, 30, TextAnchor.MiddleCenter, UiTheme.Accent);
            _bannerText.fontStyle = FontStyle.Bold;
            _banner.gameObject.SetActive(false);
        }

        // 플레이어 둘레만 뚫린 큰 검은 그림. 화면 밖까지 덮도록 80칸 크기로 만든다.
        private void BuildDarkness()
        {
            const int size = 256;
            const float worldSize = 80f;
            float hole = _config.BlackoutVisionRadius / worldSize * size;
            var texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                    float a = Mathf.Clamp01((d - hole * 0.75f) / (hole * 0.35f));
                    texture.SetPixel(x, y, new Color(0f, 0f, 0.02f, a * 0.94f));
                }
            }

            texture.Apply();
            var dark = new GameObject("BlackoutDarkness");
            _darkness = dark.AddComponent<SpriteRenderer>();
            _darkness.sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size / worldSize);
            _darkness.sortingOrder = 90;
            _darkness.color = new Color(1f, 1f, 1f, 0f);
            _darkness.enabled = false;
        }
    }
}
