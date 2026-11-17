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
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private Transform _aimPivot;
        [SerializeField, Tooltip("칸. 마우스가 이보다 가까우면 이 거리를 겨누는 것으로 본다. 손에 든 총이 몸 쪽으로 꺾이지 않게 한다")]
        private float _minAimDistance = 3f;

        private Rigidbody2D _rigidbody;
        private Camera _camera;
        private float _stunTimeLeft;
        private float _aimDistance = 3f;

        public Vector2 AimDirection { get; private set; } = Vector2.right;

        // 겨누고 있는 지점. 총은 몸 중심이 아니라 손에 있어서, 손에서 이 점을 향해 쏴야 마우스 위치에 맞는다.
        public Vector2 AimPoint => (Vector2)transform.position + AimDirection * _aimDistance;

        // 이동·사격·구르기를 할 수 있는 상태인지. 피격 경직 중이거나 HP가 0이면 막는다.
        // 조준 회전은 경직 중에도 된다.
        public bool CanAct => !_health.IsDead && _stunTimeLeft <= 0f;

        // 재장전은 경직 중에도 이어지므로 CanAct와 따로 본다.
        public bool IsDead => _health.IsDead;

        // 가방 화면이 열려 있는지. 화면(InventoryUI)이 정한다. 열려 있어도 이동은 되고 사격·구르기·재장전만 막는다.
        public bool IsBagOpen { get; set; }

        // 탈출 키를 누르고 있는 동안에는 걸을 수 없다. RaidManager가 정한다.
        public bool IsMovementLocked { get; set; }

        // 대기실에서는 창고 물건을 잔뜩 들어도 다치지 않게 끈다.
        public bool TakesOverloadDamage { get; set; } = true;

        // 무게 상한. 기본값에 강화한 만큼을 더한다.
        public float MaxWeight => _data.MaxWeight + _progress.Bonus(TechStat.MaxWeight);

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
            _health.Initialize(_data.MaxHp);
        }

        // 저장 내용은 다른 오브젝트의 Awake에서 불러온다. 그 뒤에 읽으려고 Start에서 강화를 반영한다.
        private void Start()
        {
            ApplyProgress();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _progress.Changed += ApplyProgress;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _progress.Changed -= ApplyProgress;
        }

        // 강화한 최대 체력과 방어력을 Health에 넣는다. 나머지 능력치는 쓰는 곳에서 그때그때 읽는다.
        private void ApplyProgress()
        {
            float maxHp = _data.MaxHp + _progress.Bonus(TechStat.MaxHp);
            if (!Mathf.Approximately(maxHp, _health.MaxHp))
            {
                _health.SetMaxHp(maxHp);
            }

            _health.SetBonusArmor(Mathf.RoundToInt(_progress.Bonus(TechStat.Armor)));
        }

        private void Update()
        {
            _stunTimeLeft = Mathf.Max(0f, _stunTimeLeft - Time.deltaTime);

            if (_health.IsDead)
            {
                return;
            }

            // 최대 무게의 150%를 넘으면 HP가 서서히 준다.
            if (TakesOverloadDamage && _inventory.TotalWeight > MaxWeight * _data.OverloadRatio)
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
            float speed = _data.MoveSpeed * (1f + _progress.Bonus(TechStat.MoveSpeed) / 100f)
                * _data.WeightSpeedMultiplier(_inventory.TotalWeight, MaxWeight);
            if (_shooter.IsReloading)
            {
                speed *= _data.ReloadMoveSpeedMultiplier;
            }

            _rigidbody.linearVelocity = CanAct && !IsMovementLocked ? _input.Move * speed : Vector2.zero;
        }

        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            _stunTimeLeft = _data.HitStunDuration;
            Sfx.PlayAt(SfxId.PlayerHurt, transform.position);
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
            _aimDistance = Mathf.Max(toMouse.magnitude, _minAimDistance);
            float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
            _aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
