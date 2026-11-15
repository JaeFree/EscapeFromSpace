using UnityEngine;

namespace EscapeFromSpace
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerData _data;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private Transform _aimPivot;

        private Rigidbody2D _rigidbody;
        private Camera _camera;

        public Vector2 AimDirection { get; private set; } = Vector2.right;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
        }

        private void Update()
        {
            UpdateAim();
        }

        private void FixedUpdate()
        {
            _rigidbody.linearVelocity = _roll.IsRolling
                ? _roll.TakeStepVelocity()
                : _input.Move * _data.MoveSpeed;
        }

        private void UpdateAim()
        {
            Vector2 mouseWorld = _camera.ScreenToWorldPoint(_input.AimScreenPosition);
            Vector2 toMouse = mouseWorld - (Vector2)transform.position;

            // 마우스가 캐릭터 바로 위에 있으면 방향이 튀므로 직전 방향을 유지한다.
            if (toMouse.sqrMagnitude < 0.0001f)
            {
                return;
            }

            AimDirection = toMouse.normalized;
            float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
            _aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
