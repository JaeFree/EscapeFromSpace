using UnityEngine;

namespace EscapeFromSpace
{
    public class PlayerShooter : MonoBehaviour
    {
        // 가방과 무기 슬롯이 생기기 전까지 쓰는 임시 연결.
        [SerializeField] private WeaponData _weapon;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private Transform _muzzle;
        [SerializeField, Tooltip("사격이 맞는 레이어 (벽, 적)")] private LayerMask _hitMask;
        [SerializeField] private LayerMask _wallMask;
        [SerializeField] private GameObject _hitMarkerPrefab;

        private float _nextFireTime;

        private void Update()
        {
            if (_weapon == null || !_input.FireHeld || !_controller.CanAct || _roll.IsRolling || Time.time < _nextFireTime)
            {
                return;
            }

            _nextFireTime = Time.time + 1f / _weapon.FireRate;
            Fire();
        }

        private void Fire()
        {
            Vector2 center = transform.position;
            Vector2 muzzle = _muzzle.position;

            // 벽에 바짝 붙으면 총구가 벽 너머로 나간다. 그대로 쏘면 벽을 관통하므로 몸과 총구 사이의 벽을 먼저 확인한다.
            RaycastHit2D hit = Physics2D.Linecast(center, muzzle, _wallMask);
            if (hit.collider == null)
            {
                hit = Physics2D.Raycast(muzzle, _controller.AimDirection, _weapon.Range, _hitMask);
            }

            if (hit.collider == null)
            {
                return;
            }

            Instantiate(_hitMarkerPrefab, hit.point, Quaternion.identity);

            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(_weapon.Damage, hit.point);
            }
        }
    }
}
