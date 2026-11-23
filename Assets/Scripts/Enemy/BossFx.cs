using UnityEngine;

namespace EscapeFromSpace
{
    // 네임드 패턴의 예고 표시와 판정 도우미 (기획서 21장). 그림 파일 없이 코드로 그린다.
    public static class BossFx
    {
        public const int HazardOrder = 2;
        public const int GroundOrder = 3;
        public const int LineOrder = 9;
        public const int BulletOrder = 30;

        public static readonly Color Warning = new Color(1f, 0.2f, 0.2f, 1f);

        // Circle 그림의 한 변 길이(유닛). 반경을 크기로 바꿀 때 쓴다.
        private static float CircleSize => PlayerGadgets.Circle.bounds.size.x;

        public static SpriteRenderer Disc(Vector2 center, float radius, Color color, int order)
        {
            var go = new GameObject("BossDisc");
            go.transform.position = center;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PlayerGadgets.Circle;
            renderer.color = color;
            renderer.sortingOrder = order;
            SetRadius(renderer, radius);
            return renderer;
        }

        public static void SetRadius(SpriteRenderer disc, float radius)
        {
            float scale = radius * 2f / CircleSize;
            disc.transform.localScale = new Vector3(scale, scale, 1f);
        }

        public static LineRenderer Line(Material material, Color color, float width)
        {
            var go = new GameObject("BossLine");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sortingOrder = LineOrder;
            line.numCapVertices = 2;
            SetLine(line, color, width);
            return line;
        }

        public static void SetLine(LineRenderer line, Color color, float width)
        {
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
        }

        public static LineRenderer Ring(Material material, Vector2 center, float radius, Color color, float width)
        {
            const int segments = 48;
            LineRenderer line = Line(material, color, width);
            line.loop = true;
            line.positionCount = segments;
            line.sortingOrder = GroundOrder;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            return line;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // 빔이 벽에 막히는 곳까지
        public static Vector2 BeamEnd(Vector2 origin, Vector2 direction, float length, LayerMask wallMask)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, length, wallMask);
            return hit.collider != null ? hit.point : origin + direction * length;
        }

        public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(point, a + ab * t);
        }

        // 폭발은 벽 너머까지 닿지 않는다.
        public static bool InBlast(Health target, Vector2 center, float radius, LayerMask wallMask)
        {
            Vector2 position = target.transform.position;
            return Vector2.Distance(position, center) <= radius && Physics2D.Linecast(center, position, wallMask).collider == null;
        }

        public static Vector2 Rotate(Vector2 direction, float degrees)
        {
            return Quaternion.Euler(0f, 0f, degrees) * direction;
        }

        // 잠깐 보였다가 흐려지며 사라진다.
        public static void Flash(Vector2 center, float radius, Color color, float time)
        {
            SpriteRenderer disc = Disc(center, radius, color, GroundOrder);
            disc.gameObject.AddComponent<BossFade>().Begin(time);
        }
    }

    // 붙은 그림(스프라이트·선)을 흐리게 하다가 지운다.
    public class BossFade : MonoBehaviour
    {
        private float _time;
        private float _left;
        private SpriteRenderer _sprite;
        private LineRenderer _line;
        private Color _spriteColor;
        private Color _lineColor;

        public void Begin(float time)
        {
            _time = Mathf.Max(0.01f, time);
            _left = _time;
            _sprite = GetComponent<SpriteRenderer>();
            _line = GetComponent<LineRenderer>();
            if (_sprite != null)
            {
                _spriteColor = _sprite.color;
            }

            if (_line != null)
            {
                _lineColor = _line.startColor;
            }
        }

        private void Update()
        {
            _left -= Time.deltaTime;
            float alpha = Mathf.Clamp01(_left / _time);
            if (_sprite != null)
            {
                _sprite.color = BossFx.WithAlpha(_spriteColor, _spriteColor.a * alpha);
            }

            if (_line != null)
            {
                Color color = BossFx.WithAlpha(_lineColor, _lineColor.a * alpha);
                _line.startColor = color;
                _line.endColor = color;
            }

            if (_left <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    // 바닥에 남는 장판(가스, 불) 또는 고리. 안에 있으면 초당 피해를 나눠서 준다.
    public class BossHazard : MonoBehaviour
    {
        private const float TickInterval = 0.25f;

        private Health _target;
        private float _damagePerSecond;
        private float _radius;
        private float _innerRadius;
        private float _left;
        private float _tick;
        private SpriteRenderer _disc;
        private LineRenderer _ring;
        private Color _color;

        // innerRadius가 0보다 크면 고리다 (안쪽은 안전).
        public static BossHazard Spawn(Vector2 center, float radius, float innerRadius, float damagePerSecond, float duration, Color color,
            Health target, Material lineMaterial)
        {
            BossHazard hazard;
            if (innerRadius > 0f)
            {
                LineRenderer ring = BossFx.Ring(lineMaterial, center, (radius + innerRadius) * 0.5f, color, radius - innerRadius);
                ring.sortingOrder = BossFx.HazardOrder;
                hazard = ring.gameObject.AddComponent<BossHazard>();
                hazard._ring = ring;
                hazard.transform.position = center;
            }
            else
            {
                SpriteRenderer disc = BossFx.Disc(center, radius, color, BossFx.HazardOrder);
                hazard = disc.gameObject.AddComponent<BossHazard>();
                hazard._disc = disc;
            }

            hazard.name = "BossHazard";
            hazard._target = target;
            hazard._radius = radius;
            hazard._innerRadius = innerRadius;
            hazard._damagePerSecond = damagePerSecond;
            hazard._left = duration;
            hazard._color = color;
            return hazard;
        }

        private void Update()
        {
            _left -= Time.deltaTime;

            // 마지막 0.5초는 흐려지며 사라지는 중이라 피해가 없다.
            float alpha = Mathf.Clamp01(_left / 0.5f);
            Color color = BossFx.WithAlpha(_color, _color.a * alpha);
            if (_disc != null)
            {
                _disc.color = color;
            }

            if (_ring != null)
            {
                _ring.startColor = color;
                _ring.endColor = color;
            }

            if (_left <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _tick -= Time.deltaTime;
            if (_tick > 0f || _left < 0.5f || _target == null)
            {
                return;
            }

            _tick = TickInterval;
            Vector2 position = _target.transform.position;
            float distance = Vector2.Distance(position, transform.position);
            if (distance <= _radius && distance >= _innerRadius)
            {
                _target.TakeDamage(_damagePerSecond * TickInterval, position);
            }
        }
    }
}
