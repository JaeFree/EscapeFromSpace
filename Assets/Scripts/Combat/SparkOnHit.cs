using UnityEngine;

namespace EscapeFromSpace
{
    // 로봇은 맞으면 혈흔 대신 불꽃이 튄다 (기획서 4.1의 5항).
    public class SparkOnHit : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private GameObject _sparkPrefab;

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
        }

        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            Instantiate(_sparkPrefab, hitPoint, Quaternion.identity);
        }
    }
}
