using UnityEngine;

namespace EscapeFromSpace
{
    // 대기실 사격장 (2026-11-16 사용자 결정). 대기실에서는 이 구역 안에서만 쏠 수 있고, 탄약은 줄지 않는다.
    public class ShootingRange : MonoBehaviour
    {
        [SerializeField] private PlayerShooter _shooter;
        [SerializeField, Tooltip("칸. 이 크기의 구역 안에 서 있어야 쏠 수 있다")] private Vector2 _zoneSize = new Vector2(4f, 4f);

        private void Awake()
        {
            _shooter.HasInfiniteAmmo = true;
            _shooter.CanUseWeapon = false;
        }

        private void Update()
        {
            Vector2 offset = (Vector2)_shooter.transform.position - (Vector2)transform.position;
            _shooter.CanUseWeapon = Mathf.Abs(offset.x) <= _zoneSize.x * 0.5f && Mathf.Abs(offset.y) <= _zoneSize.y * 0.5f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.95f, 0.8f, 0.3f);
            Gizmos.DrawWireCube(transform.position, _zoneSize);
        }
    }
}
