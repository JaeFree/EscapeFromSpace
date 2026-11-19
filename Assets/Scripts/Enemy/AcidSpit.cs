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
            var spit = new GameObject("AcidSpit");
            spit.transform.position = from;
            spit.transform.localScale = Vector3.one * 0.35f;
            var renderer = spit.AddComponent<SpriteRenderer>();
            renderer.sprite = PlayerGadgets.Circle;
            renderer.color = new Color(0.55f, 1f, 0.3f);
            renderer.sortingOrder = 30;
            var component = spit.AddComponent<AcidSpit>();
            component._direction = direction.normalized;
            component._speed = data.ProjectileSpeed > 0f ? data.ProjectileSpeed : 8f;
            component._range = data.Range;
            component._damage = data.Damage;
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
