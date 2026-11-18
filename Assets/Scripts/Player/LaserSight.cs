using UnityEngine;

namespace EscapeFromSpace
{
    // 레이저 사이트 부착물을 단 총의 조준 레이저 (기획서 17장, 10장의 "조준 레이저 궤적").
    // 총구에서 마우스 쪽으로 벽이나 로봇에 닿을 때까지 가는 붉은 선을 긋는다. 판정에는 쓰지 않는다.
    public class LaserSight : MonoBehaviour
    {
        [SerializeField] private PlayerShooter _shooter;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField, Tooltip("레이저가 멈추는 레이어 (벽, 적)")] private LayerMask _hitMask;
        [SerializeField] private Material _material;
        [SerializeField] private Color _color = new Color(1f, 0.15f, 0.12f, 0.55f);
        [SerializeField, Tooltip("칸")] private float _width = 0.035f;
        [SerializeField, Tooltip("칸. 아무것도 닿지 않을 때 그리는 길이")] private float _maxLength = 25f;

        private LineRenderer _line;

        private void Awake()
        {
            var child = new GameObject("LaserLine");
            child.transform.SetParent(transform, false);
            _line = child.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.startWidth = _width;
            _line.endWidth = _width;
            _line.numCapVertices = 2;
            _line.sortingOrder = 9;
            if (_material != null)
            {
                _line.sharedMaterial = _material;
            }

            // 총구 쪽이 진하고 끝으로 갈수록 옅게 해서 빛줄기처럼 보이게 한다.
            _line.startColor = _color;
            _line.endColor = new Color(_color.r, _color.g, _color.b, _color.a * 0.35f);
            _line.enabled = false;
        }

        private void LateUpdate()
        {
            bool show = _shooter.HasLaserSight && !_controller.IsDead && !_controller.IsBagOpen
                && (_roll == null || !_roll.IsRolling) && _shooter.CanUseWeapon;
            _line.enabled = show;
            if (!show)
            {
                return;
            }

            Vector2 start = _shooter.Muzzle.position;
            Vector2 toAim = _controller.AimPoint - start;
            Vector2 direction = toAim.sqrMagnitude > 0.0001f ? toAim.normalized : _controller.AimDirection;
            RaycastHit2D hit = Physics2D.Raycast(start, direction, _maxLength, _hitMask);
            Vector2 end = hit.collider != null ? hit.point : start + direction * _maxLength;
            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
        }
    }
}
