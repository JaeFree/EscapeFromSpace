using UnityEngine;

namespace EscapeFromSpace
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerData _data;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private Health _health;
        [SerializeField] private Transform _aimPivot;

        private Rigidbody2D _rigidbody;
        private Camera _camera;
        private float _stunTimeLeft;

        public Vector2 AimDirection { get; private set; } = Vector2.right;

        // 이동·사격·구르기를 할 수 있는 상태인지. 피격 경직 중이거나 HP가 0이면 막는다 (사망 처리는 11/18).
        // 조준 회전은 경직 중에도 된다.
        public bool CanAct => !_health.IsDead && _stunTimeLeft <= 0f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
            _health.Initialize(_data.MaxHp);
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
            _stunTimeLeft = Mathf.Max(0f, _stunTimeLeft - Time.deltaTime);

            if (_health.IsDead)
            {
                return;
            }

            UpdateAim();
        }

        private void FixedUpdate()
        {
            if (_roll.IsRolling)
            {
                _rigidbody.linearVelocity = _roll.TakeStepVelocity();
                return;
            }

            _rigidbody.linearVelocity = CanAct ? _input.Move * _data.MoveSpeed : Vector2.zero;
        }

        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            _stunTimeLeft = _data.HitStunDuration;
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
