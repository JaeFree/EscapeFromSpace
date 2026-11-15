using UnityEngine;

namespace EscapeFromSpace
{
    // 피격 시 캐릭터가 하얗게 번쩍인다 (기획서 4.1의 5항).
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private PlayerData _data;

        private Color _baseColor;
        private float _timeLeft;

        private void Awake()
        {
            _baseColor = _renderer.color;
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
        }

        private void Update()
        {
            if (_timeLeft <= 0f)
            {
                return;
            }

            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                _renderer.color = _baseColor;
            }
        }

        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            _renderer.color = Color.white;
            _timeLeft = _data.HitFlashDuration;
        }
    }
}
