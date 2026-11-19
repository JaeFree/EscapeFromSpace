using UnityEngine;

namespace EscapeFromSpace
{
    // 가까운 적에 대해 엄폐 중이면 몸을 가려 주는 벽·엄폐물 칸을 하얗게 빛낸다 (기획서 19.3, 덕코프와 같은 방식).
    // 그림 에셋 없이 흰 사각형 하나를 깜빡인다. PlayerController가 만든다.
    public class PlayerCoverGlow : MonoBehaviour
    {
        private const float CheckInterval = 0.1f;
        private const float ThreatRange = 14f;

        private SpriteRenderer _glow;
        private Health _health;
        private LayerMask _wallMask;
        private float _checkTime;
        private bool _inCover;

        public bool InCover => _inCover;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _wallMask = LayerMask.GetMask("Wall");

            var glow = new GameObject("CoverGlow");
            _glow = glow.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _glow.sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _glow.sortingOrder = 40;
            _glow.enabled = false;
        }

        private void OnDestroy()
        {
            if (_glow != null)
            {
                Destroy(_glow.gameObject);
            }
        }

        private void Update()
        {
            _checkTime -= Time.deltaTime;
            if (_checkTime <= 0f)
            {
                _checkTime = CheckInterval;
                Recheck();
            }

            if (_glow.enabled)
            {
                _glow.color = new Color(1f, 1f, 1f, 0.22f + Mathf.Sin(Time.time * 6f) * 0.08f);
            }
        }

        // 벽에 가리지 않고 보이는 가장 가까운 적을 기준으로 엄폐를 본다.
        private void Recheck()
        {
            _inCover = false;
            CombatConfig config = _health != null ? _health.Config : null;
            Vector2 me = transform.position;
            Robot threat = null;
            float best = ThreatRange;
            if (config != null && !_health.IsDead)
            {
                foreach (Robot robot in Robot.All)
                {
                    float distance = Vector2.Distance(me, robot.transform.position);
                    if (distance < best && Physics2D.Linecast(me, robot.transform.position, _wallMask).collider == null)
                    {
                        best = distance;
                        threat = robot;
                    }
                }
            }

            if (threat != null && Cover.Check(me, threat.transform.position, _wallMask, config.CoverDistance, out Vector2 point))
            {
                _inCover = true;
                _glow.transform.position = new Vector3(point.x, point.y, 0f);
            }

            _glow.enabled = _inCover;
        }
    }
}
