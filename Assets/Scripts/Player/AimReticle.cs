using UnityEngine;

namespace EscapeFromSpace
{
    // 저격총을 조준하는 동안 마우스 위치에 빈 원을 그린다.
    // 원의 크기는 그 거리에서 탄이 퍼질 수 있는 범위라서, 반동이 회복되면 줄어든다.
    [RequireComponent(typeof(LineRenderer))]
    public class AimReticle : MonoBehaviour
    {
        private const int PointCount = 48;

        [SerializeField] private PlayerShooter _shooter;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private Transform _muzzle;
        [SerializeField, Tooltip("칸. 원이 이보다 작아지지 않는다")] private float _minRadius = 0.08f;

        private LineRenderer _line;
        private Camera _camera;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.loop = true;
            _line.positionCount = PointCount;
            _line.enabled = false;
            _camera = Camera.main;
        }

        // 카메라가 흔들림까지 반영해 자리를 잡은 뒤에 그려야 원이 마우스에서 어긋나지 않는다.
        private void LateUpdate()
        {
            if (!_shooter.IsAiming)
            {
                _line.enabled = false;
                return;
            }

            Vector2 center = _camera.ScreenToWorldPoint(_input.AimScreenPosition);
            float distance = Vector2.Distance(_muzzle.position, center);
            float radius = Mathf.Max(_minRadius, distance * Mathf.Tan(_shooter.CurrentSpread * Mathf.Deg2Rad));

            for (int i = 0; i < PointCount; i++)
            {
                float angle = i * 2f * Mathf.PI / PointCount;
                _line.SetPosition(i, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            _line.enabled = true;
        }
    }
}
