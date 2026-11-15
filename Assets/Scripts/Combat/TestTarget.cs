using UnityEngine;

namespace EscapeFromSpace
{
    // AI 없이 맞기만 하는 사격 확인용 표적. 11/16에 로봇으로 대체한다.
    public class TestTarget : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private float _maxHp;

        private void Awake()
        {
            _health.Initialize(_maxHp);
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            _health.Died -= OnDied;
        }

        private void OnDied()
        {
            Destroy(gameObject);
        }
    }
}
