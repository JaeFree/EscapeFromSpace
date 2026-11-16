using UnityEngine;

namespace EscapeFromSpace
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerData _data;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private PlayerShooter _shooter;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private Health _health;
        [SerializeField] private Transform _aimPivot;

        private Rigidbody2D _rigidbody;
        private Camera _camera;
        private float _stunTimeLeft;

        public Vector2 AimDirection { get; private set; } = Vector2.right;

        // 이동·사격·구르기를 할 수 있는 상태인지. 피격 경직 중이거나 HP가 0이면 막는다 (사망 처리는 11/18).
        // 조준 회전은 경직 중에도 된다.
        public bool CanAct => !_health.IsDead && _stunTimeLeft <= 0f;

        // 재장전은 경직 중에도 이어지므로 CanAct와 따로 본다.
        public bool IsDead => _health.IsDead;

        // 가방 화면이 열려 있는지. 화면(InventoryUI)이 정한다. 열려 있어도 이동은 되고 사격·구르기·재장전만 막는다.
        public bool IsBagOpen { get; set; }

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

            // 최대 무게의 150%를 넘으면 HP가 서서히 준다.
            if (_inventory.TotalWeight > _data.MaxWeight * _data.OverloadRatio)
            {
                _health.TakeRawDamage(_data.OverloadDamagePerSecond * Time.deltaTime);
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

            // 무게 감속과 재장전 감속은 서로 곱한다. 구르기는 무게의 영향을 받지 않는다.
            float speed = _data.MoveSpeed * _data.WeightSpeedMultiplier(_inventory.TotalWeight);
            if (_shooter.IsReloading)
            {
                speed *= _data.ReloadMoveSpeedMultiplier;
            }

            _rigidbody.linearVelocity = CanAct ? _input.Move * speed : Vector2.zero;
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
