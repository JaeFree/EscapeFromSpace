using UnityEngine;

namespace EscapeFromSpace
{
    // 로봇의 사격 패턴 (기획서 5.3): (충전) → 발 수만큼 [쏘고 발 간격만큼 기다림] → 쉼.
    // 언제 공격할 수 있는지는 Robot이 정하고, 여기서는 패턴과 판정만 맡는다.
    public class RobotAttack : MonoBehaviour
    {
        // 궤적 선이 몸 안에서 시작하지 않게 띄우는 거리
        private const float MuzzleOffset = 0.5f;

        private enum Phase
        {
            Ready,
            Windup,
            Burst,
            Rest,
        }

        [SerializeField, Tooltip("로봇의 탄이 맞는 레이어 (벽, 플레이어). 다른 로봇은 통과한다")] private LayerMask _shotMask;
        [SerializeField] private LayerMask _wallMask;
        [SerializeField] private BulletTracer _tracerPrefab;
        [SerializeField] private SwingArc _swingArcPrefab;
        [SerializeField] private AttackTelegraph _telegraph;
        [SerializeField, Tooltip("쏠 때 몸 앞에 잠깐 번쩍이는 빛. 비우면 없다")] private GameObject _muzzleFlashPrefab;

        // 조준선이 고정되는 순간을 소리로 한 번만 알린다.
        private bool _lockAnnounced;

        private RobotData _data;
        private Health _player;
        private Phase _phase;
        private float _timer;
        private int _shotsLeft;
        private Vector2 _aimDirection = Vector2.right;

        // 충전하거나 조준선을 보여 주는 중. 이때는 Robot이 걸음을 멈춘다.
        public bool IsWindingUp => _phase == Phase.Windup;

        // 충전 중에는 이 방향을 본다. 방향이 고정된 뒤에는 플레이어를 따라가지 않는다.
        public Vector2 AimDirection => _aimDirection;

        public void Initialize(RobotData data, Health player)
        {
            _data = data;
            _player = player;
        }

        // 매 프레임 부른다. canAttack은 플레이어가 보이고 사거리 안에 있는지.
        public void Tick(bool canAttack)
        {
            switch (_phase)
            {
                case Phase.Ready:
                    if (canAttack)
                    {
                        Begin();
                    }

                    break;

                case Phase.Windup:
                    UpdateWindup(canAttack);
                    break;

                case Phase.Burst:
                    UpdateBurst(canAttack);
                    break;

                case Phase.Rest:
                    // 쉬는 시간은 플레이어가 안 보여도 흐른다.
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        _phase = Phase.Ready;
                    }

                    break;
            }
        }

        // 충전을 취소한다. 쏘는 중이었다면 남은 발을 버리고 쉰다.
        public void Cancel()
        {
            _telegraph.Hide();

            if (_phase == Phase.Windup)
            {
                _phase = Phase.Ready;
            }
            else if (_phase == Phase.Burst)
            {
                StartRest();
            }
        }

        private void Begin()
        {
            _aimDirection = DirectionToPlayer();

            if (_data.WindupTime > 0f)
            {
                _phase = Phase.Windup;
                _timer = _data.WindupTime;
                _lockAnnounced = false;
                Sfx.PlayAt(SfxId.RobotCharge, transform.position);
                UpdateTelegraph();
            }
            else
            {
                StartBurst();
            }
        }

        private void UpdateWindup(bool canAttack)
        {
            // 조준 중에 시야가 끊기거나 사거리를 벗어나면 취소한다.
            if (!canAttack)
            {
                Cancel();
                return;
            }

            _timer -= Time.deltaTime;

            // 마지막 구간(AimLockTime)에는 방향을 고정한다. 그 뒤에 움직이거나 구르면 피할 수 있다.
            if (_timer > _data.AimLockTime)
            {
                _aimDirection = DirectionToPlayer();
            }
            else if (!_lockAnnounced && _data.AimLockTime > 0f && _data.AttackKind == RobotAttackKind.Sniper)
            {
                _lockAnnounced = true;
                Sfx.PlayAt(SfxId.RobotAimLock, transform.position);
            }

            if (_timer <= 0f)
            {
                _telegraph.Hide();
                StartBurst();
            }
            else
            {
                UpdateTelegraph();
            }
        }

        private void UpdateTelegraph()
        {
            bool locked = _timer <= _data.AimLockTime;

            if (_data.AttackKind == RobotAttackKind.Shotgun)
            {
                float progress = 1f - _timer / _data.WindupTime;
                _telegraph.ShowFan(_aimDirection, _data.SpreadAngle, _data.Range, progress);
            }
            else
            {
                Vector2 origin = transform.position;
                RaycastHit2D wall = Physics2D.Raycast(origin, _aimDirection, _data.Range, _wallMask);
                Vector2 end = wall.collider != null ? wall.point : origin + _aimDirection * _data.Range;
                _telegraph.ShowLine(origin + _aimDirection * MuzzleOffset, end, locked);
            }
        }

        private void StartBurst()
        {
            _phase = Phase.Burst;
            _shotsLeft = _data.BurstCount;
            _timer = 0f;
            UpdateBurst(true);
        }

        private void UpdateBurst(bool canAttack)
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            if (_shotsLeft <= 0 || !canAttack)
            {
                StartRest();
                return;
            }

            Fire();
            _shotsLeft--;
            _timer = _data.BurstInterval;

            // 발 간격이 없으면(한 발짜리) 바로 쉰다.
            if (_shotsLeft <= 0 && _data.BurstInterval <= 0f)
            {
                StartRest();
            }
        }

        private void StartRest()
        {
            _phase = Phase.Rest;
            _timer = _data.RestTime;
        }

        private void Fire()
        {
            // 충전한 공격은 고정해 둔 방향으로, 나머지는 쏘는 순간의 플레이어 쪽으로 나간다.
            if (_data.WindupTime <= 0f)
            {
                _aimDirection = DirectionToPlayer();
            }

            Vector2 origin = transform.position;
            Sfx.PlayAt(_data.AttackSound, origin);
            if (_muzzleFlashPrefab != null && _data.AttackKind != RobotAttackKind.Melee)
            {
                float angle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;
                Instantiate(_muzzleFlashPrefab, origin + _aimDirection * MuzzleOffset, Quaternion.Euler(0f, 0f, angle));
            }

            switch (_data.AttackKind)
            {
                case RobotAttackKind.Melee:
                    Swing();
                    break;

                case RobotAttackKind.Projectile:
                    AcidSpit.Launch(origin + _aimDirection * MuzzleOffset, _aimDirection, _data, _shotMask);
                    break;

                case RobotAttackKind.Shotgun:
                    // 퍼짐 값이 부채꼴 전체 각이다. 펠릿마다 그 안에서 따로 흩어진다.
                    for (int i = 0; i < _data.PelletCount; i++)
                    {
                        FireRay(_data.SpreadAngle * 0.5f * CoverSpreadScale());
                    }

                    break;

                default:
                    for (int i = 0; i < _data.PelletCount; i++)
                    {
                        FireRay(_data.SpreadAngle * CoverSpreadScale());
                    }

                    break;
            }
        }

        private void FireRay(float spread)
        {
            Vector2 origin = transform.position;
            Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-spread, spread)) * _aimDirection;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, _data.Range, _shotMask);
            Vector2 end = hit.collider != null ? hit.point : origin + direction * _data.Range;

            if (Vector2.Distance(origin, end) > MuzzleOffset)
            {
                Instantiate(_tracerPrefab).Show(origin + direction * MuzzleOffset, end);
            }

            if (hit.collider == null)
            {
                return;
            }

            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(_data.Damage, hit.point);
            }
        }

        // 근접 공격. 퍼짐 값이 부채꼴 전체 각이다. 플레이어 쪽으로 휘두르므로 거리와 벽만 확인하면 된다.
        private void Swing()
        {
            Vector2 origin = transform.position;
            float angle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;
            SwingArc arc = Instantiate(_swingArcPrefab, origin, Quaternion.Euler(0f, 0f, angle), transform);
            arc.Show(_data.Range, _data.SpreadAngle);

            Vector2 target = _player.transform.position;
            if (Vector2.Distance(origin, target) > _data.Range)
            {
                return;
            }

            if (Physics2D.Linecast(origin, target, _wallMask).collider != null)
            {
                return;
            }

            _player.TakeDamage(_data.Damage, target);
        }

        // 엄폐 중인 플레이어를 쏘면 퍼짐이 커진다 (기획서 19.3).
        private float CoverSpreadScale()
        {
            CombatConfig config = _player.Config;
            if (config == null || !Cover.IsInCover(_player.transform.position, transform.position, _wallMask, config.CoverDistance))
            {
                return 1f;
            }

            return config.CoverSpreadMultiplier;
        }

        private Vector2 DirectionToPlayer()
        {
            Vector2 toPlayer = (Vector2)_player.transform.position - (Vector2)transform.position;
            return toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : _aimDirection;
        }
    }
}
