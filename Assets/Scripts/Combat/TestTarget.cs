using UnityEngine;

namespace EscapeFromSpace
{
    // AI 없이 맞기만 하는 사격 확인용 표적. 11/16에 로봇으로 대체한다.
    public class TestTarget : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private float _maxHp;

        // 무기 여러 종을 계속 쏴 보려고 다시 생기게 한다. 로봇으로 대체할 때 함께 없앤다.
        [SerializeField, Tooltip("초. 부서진 뒤 같은 자리에 다시 생기기까지")] private float _respawnDelay;

        private SpriteRenderer _renderer;
        private Collider2D _collider;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _health.Initialize(_maxHp);
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            _health.Died -= OnDied;
        }

        private void OnDied()
        {
            // 오브젝트를 끄면 타이머도 멈추므로 보이는 것과 충돌만 끈다.
            SetPresent(false);
            Invoke(nameof(Respawn), _respawnDelay);
        }

        private void Respawn()
        {
            _health.Initialize(_maxHp);
            SetPresent(true);
        }

        private void SetPresent(bool present)
        {
            _renderer.enabled = present;
            _collider.enabled = present;
        }
    }
}
