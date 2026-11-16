using UnityEngine;

namespace EscapeFromSpace
{
    // 피격 시 캐릭터가 하얗게 번쩍인다 (기획서 4.1의 5항).
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private PlayerData _data;
        [SerializeField, Tooltip("그림이 원래 흰색이면 색만 바꿔서는 번쩍임이 안 보인다. 넣으면 번쩍이는 동안 이 재질로 그린다")]
        private Material _flashMaterial;

        private Color _baseColor;
        private Material _baseMaterial;
        private float _timeLeft;

        private void Awake()
        {
            _baseColor = _renderer.color;
            _baseMaterial = _renderer.sharedMaterial;
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
                _renderer.sharedMaterial = _baseMaterial;
            }
        }

        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            // 경직 시간을 0으로 둔 것처럼 번쩍임 시간도 0이면 아예 바꾸지 않는다. 되돌릴 프레임이 없기 때문이다.
            if (_data.HitFlashDuration <= 0f)
            {
                return;
            }

            _renderer.color = Color.white;
            if (_flashMaterial != null)
            {
                _renderer.sharedMaterial = _flashMaterial;
            }

            _timeLeft = _data.HitFlashDuration;
        }
    }
}
