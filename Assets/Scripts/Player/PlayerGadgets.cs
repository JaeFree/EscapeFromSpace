using UnityEngine;

namespace EscapeFromSpace
{
    // 도구칸의 은신 소모품 (기획서 19.3): 홀로그램 미끼와 EMP 수류탄. 그림 에셋 없이 코드로 만든 도형을 쓴다.
    public static class PlayerGadgets
    {
        private static Sprite _circle;

        // 커서 쪽으로 쓴다. 사거리를 넘거나 벽에 막히면 그 앞에 떨어진다.
        public static void Use(ItemData item, Transform user, Vector2 aimPoint, LayerMask wallMask)
        {
            Vector2 from = user.position;
            Vector2 toAim = aimPoint - from;
            float distance = Mathf.Min(toAim.magnitude, item.GadgetRange);
            Vector2 direction = toAim.sqrMagnitude > 0.0001f ? toAim.normalized : Vector2.right;
            RaycastHit2D wall = Physics2D.Raycast(from, direction, distance, wallMask);
            Vector2 target = wall.collider != null ? wall.point - direction * 0.4f : from + direction * distance;

            switch (item.Gadget)
            {
                case GadgetKind.Decoy:
                    HologramDecoy.Spawn(user, target, item);
                    break;
                case GadgetKind.Emp:
                    EmpGrenade.Throw(from, target, item);
                    break;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                {
                    const int size = 32;
                    var texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                            texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 8f)));
                        }
                    }

                    texture.Apply();
                    _circle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                }

                return _circle;
            }
        }
    }

    // 내 모습을 비춘 홀로그램. 떠 있는 동안 주기적으로 소리를 내서 둘레의 로봇이 의심하고 살피러 온다.
    public class HologramDecoy : MonoBehaviour
    {
        private const float PingInterval = 1f;

        private float _timeLeft;
        private float _radius;
        private float _ping;
        private SpriteRenderer _renderer;

        public static void Spawn(Transform user, Vector2 at, ItemData item)
        {
            var decoy = new GameObject("HologramDecoy");
            decoy.transform.position = at;
            var renderer = decoy.AddComponent<SpriteRenderer>();

            // 플레이어의 몸 그림을 그대로 비춘다.
            SpriteRenderer body = null;
            foreach (SpriteRenderer candidate in user.GetComponentsInChildren<SpriteRenderer>())
            {
                if (candidate.sprite != null && candidate.enabled && (body == null || candidate.bounds.size.sqrMagnitude > body.bounds.size.sqrMagnitude))
                {
                    body = candidate;
                }
            }

            if (body != null)
            {
                renderer.sprite = body.sprite;
                decoy.transform.localScale = body.transform.lossyScale;
                decoy.transform.rotation = body.transform.rotation;
            }
            else
            {
                renderer.sprite = PlayerGadgets.Circle;
                decoy.transform.localScale = Vector3.one * 0.8f;
            }

            renderer.sortingOrder = 30;
            var component = decoy.AddComponent<HologramDecoy>();
            component._renderer = renderer;
            component._timeLeft = item.GadgetDuration;
            component._radius = item.GadgetRadius;
            Sfx.PlayAt(SfxId.UiPrompt, at);
        }

        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // 홀로그램처럼 푸르게 깜빡인다.
            float flicker = 0.45f + Mathf.Sin(Time.time * 20f) * 0.1f + (Mathf.Repeat(Time.time, 0.7f) < 0.05f ? -0.3f : 0f);
            _renderer.color = new Color(0.45f, 0.95f, 1f, flicker);

            _ping -= Time.deltaTime;
            if (_ping <= 0f)
            {
                _ping = PingInterval;
                Footsteps.Emit(transform.position, _radius);
            }
        }
    }

    // 던진 뒤 잠시 있다 터진다. 반경 안의 로봇은 잠시 멈추고 쏘지 못한다. 에일리언은 멈추지 않는다.
    public class EmpGrenade : MonoBehaviour
    {
        private const float FlightTime = 0.35f;

        private Vector2 _from;
        private Vector2 _to;
        private float _time;
        private float _delay;
        private float _radius;
        private float _stun;

        public static void Throw(Vector2 from, Vector2 to, ItemData item)
        {
            var grenade = new GameObject("EmpGrenade");
            grenade.transform.position = from;
            grenade.transform.localScale = Vector3.one * 0.3f;
            var renderer = grenade.AddComponent<SpriteRenderer>();
            renderer.sprite = PlayerGadgets.Circle;
            renderer.color = new Color(0.4f, 0.8f, 1f);
            renderer.sortingOrder = 30;
            var component = grenade.AddComponent<EmpGrenade>();
            component._from = from;
            component._to = to;
            component._delay = Mathf.Max(FlightTime, item.GadgetDelay);
            component._radius = item.GadgetRadius;
            component._stun = item.GadgetDuration;
            Sfx.PlayAt(SfxId.ItemDropToWorld, from);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float flight = Mathf.Clamp01(_time / FlightTime);
            transform.position = Vector2.Lerp(_from, _to, flight);
            if (_time < _delay)
            {
                return;
            }

            foreach (Robot robot in new System.Collections.Generic.List<Robot>(Robot.All))
            {
                if (robot != null && Vector2.Distance(robot.transform.position, _to) <= _radius)
                {
                    robot.Stun(_stun);
                }
            }

            EmpBlast.Spawn(_to, _radius);
            Sfx.PlayAt(SfxId.RobotAimLock, _to);
            Destroy(gameObject);
        }
    }

    // 터진 자리에서 퍼지는 푸른 고리
    public class EmpBlast : MonoBehaviour
    {
        private const float Duration = 0.4f;

        private float _time;
        private float _radius;
        private SpriteRenderer _renderer;

        public static void Spawn(Vector2 at, float radius)
        {
            var blast = new GameObject("EmpBlast");
            blast.transform.position = at;
            var component = blast.AddComponent<EmpBlast>();
            component._radius = radius;
            component._renderer = blast.AddComponent<SpriteRenderer>();
            component._renderer.sprite = PlayerGadgets.Circle;
            component._renderer.sortingOrder = 29;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float t = Mathf.Clamp01(_time / Duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.3f, _radius * 2f, t);
            _renderer.color = new Color(0.45f, 0.85f, 1f, 0.55f * (1f - t));
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
