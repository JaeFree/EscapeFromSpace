using UnityEngine;

namespace EscapeFromSpace
{
    // 에일리언 스피터의 산성 침 (기획서 19.4). 히트스캔이 아니라 눈에 보이게 날아가서, 보고 피하거나 구를 수 있다.
    public class AcidSpit : MonoBehaviour
    {
        private const float Radius = 0.18f;

        private Vector2 _direction;
        private float _speed;
        private float _range;
        private float _damage;
        private float _travelled;
        private LayerMask _mask;

        public static void Launch(Vector2 from, Vector2 direction, RobotData data, LayerMask mask)
        {
            Launch(from, direction, data.ProjectileSpeed > 0f ? data.ProjectileSpeed : 8f, data.Range, data.Damage, mask,
                new Color(0.55f, 1f, 0.3f), 0.35f);
        }

        // 네임드 패턴의 탄도 같은 방식으로 날아간다 (기획서 21장). 보고 피할 수 있게 히트스캔이 아니다.
        public static void Launch(Vector2 from, Vector2 direction, float speed, float range, float damage, LayerMask mask, Color color, float scale)
        {
            var spit = new GameObject("AcidSpit");
            spit.transform.position = from;
            spit.transform.localScale = Vector3.one * scale;
            var renderer = spit.AddComponent<SpriteRenderer>();
            renderer.sprite = PlayerGadgets.Circle;
            renderer.color = color;
            renderer.sortingOrder = 30;
            var component = spit.AddComponent<AcidSpit>();
            component._direction = direction.normalized;
            component._speed = speed;
            component._range = range;
            component._damage = damage;
            component._mask = mask;
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            Vector2 position = transform.position;
            RaycastHit2D hit = Physics2D.CircleCast(position, Radius, _direction, step, _mask);
            if (hit.collider != null)
            {
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.TakeDamage(_damage, hit.point);
                }

                Destroy(gameObject);
                return;
            }

            transform.position = position + _direction * step;
            _travelled += step;
            if (_travelled >= _range)
            {
                Destroy(gameObject);
            }
        }
    }
}
