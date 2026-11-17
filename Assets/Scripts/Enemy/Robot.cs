using UnityEngine;

namespace EscapeFromSpace
{
    // 보안 로봇의 상태 머신 (기획서 5.1). 5종이 이 스크립트 하나를 쓰고 RobotData로 나뉜다.
    public class Robot : MonoBehaviour
    {
        // 길을 못 찾았을 때 매 프레임 다시 찾지 않도록 두는 간격
        private const float RepathDelay = 1f;

        // 사거리 밖의 플레이어에게 다가갈 때 길을 다시 찾는 간격
        private const float ApproachRepathInterval = 0.3f;

        // 사거리 끝에 걸쳐 서면 플레이어가 한 걸음만 물러나도 다시 걸어야 해서 조금 더 다가가 멈춘다.
        private const float StopRangeRatio = 0.8f;

        private const float ReturnArriveDistance = 0.2f;

        private enum State
        {
            Patrol,
            Alert,
            Attack,
            Chase,
            Return,
        }

        [SerializeField] private Health _health;
        [SerializeField] private RobotMotor _motor;
        [SerializeField] private RobotAttack _attack;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField, Tooltip("단색 도형일 때 바라보는 쪽을 알려 주는 막대. 그림이 있으면 숨긴다")] private SpriteRenderer _barrel;
        [SerializeField, Tooltip("경계 상태에서 머리 위에 켜는 표시")] private GameObject _alertMark;
        [SerializeField] private LayerMask _wallMask;
        [SerializeField, Tooltip("부서진 자리에 남는 잔해. 컨테이너처럼 뒤진다")] private Lootable _wreckPrefab;
        [SerializeField, Tooltip("부서질 때 터지는 효과. 비우면 없다")] private GameObject _explosionPrefab;

        // Inspector에서 지금 상태를 보려고 노출한다.
        [SerializeField] private State _state;

        private RobotData _data;
        private Health _player;
        private Transform[] _waypoints;
        private int _waypointIndex;
        private bool _resumeWaypoint;
        private float _repathTimeLeft;
        private float _stateTime;
        private Vector2 _lastSeenPosition;
        private Vector2 _chaseTarget;
        private Vector2 _returnPosition;
        private Vector2 _sweepCenter;
        private float _sweepOffset;
        private float _sweepDirection = 1f;

        public RobotData Data => _data;

        // 순찰 속도가 0인 로봇(저격)은 걷지 않고 제자리에서 좌우로 훑는다.
        private bool IsStationary => _data.PatrolSpeed <= 0f;
        private Vector2 Position => transform.position;
        private Vector2 PlayerPosition => _player.transform.position;
        private bool IsPlayerAlive => _player != null && !_player.IsDead;

        // RobotZone이 만든 직후에 부른다. 첫 웨이포인트가 시작 위치다.
        public void Initialize(RobotData data, Transform[] waypoints, WallGrid grid, Health player)
        {
            _data = data;
            _waypoints = waypoints;
            _player = player;
            // 몸은 바라보는 방향으로 도는 자식 아래에 있어서 그 +X를 보게만 해 두면 된다.
            data.ApplyLook(_body, 0f);
            if (_barrel != null)
            {
                _barrel.enabled = data.Sprite == null;
            }

            _health.Initialize(data.MaxHp);
            _alertMark.SetActive(false);

            // 두 번째 웨이포인트 쪽을 보고 시작한다. 움직이지 않는 로봇은 이 방향을 중심으로 훑는다.
            Vector2 facing = Vector2.right;
            if (waypoints.Length > 1)
            {
                Vector2 toNext = waypoints[1].position - waypoints[0].position;
                if (toNext.sqrMagnitude > 0.0001f)
                {
                    facing = toNext.normalized;
                }
            }

            _sweepCenter = facing;
            _motor.Initialize(grid, data.TurnSpeed, facing);
            _motor.Speed = data.PatrolSpeed;
            _attack.Initialize(data, player);
            _state = State.Patrol;
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
            _health.Damaged += OnDamaged;
            GunNoise.Fired += OnGunNoise;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
            _health.Damaged -= OnDamaged;
            GunNoise.Fired -= OnGunNoise;
        }

        private void Update()
        {
            if (_data == null)
            {
                return;
            }

            // 플레이어 HP가 0이면 싸움을 멈추고 순찰로 돌아간다 (사망 처리는 11/18).
            if (!IsPlayerAlive && _state != State.Patrol && _state != State.Return)
            {
                BeginReturn();
            }

            _stateTime += Time.deltaTime;

            switch (_state)
            {
                case State.Patrol:
                    UpdatePatrol();
                    break;
                case State.Alert:
                    UpdateAlert();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Chase:
                    UpdateChase();
                    break;
                case State.Return:
                    UpdateReturn();
                    break;
            }
        }

        // ---------- 순찰 ----------

        private void UpdatePatrol()
        {
            if (CanSeePlayer(true))
            {
                BeginAlert();
                return;
            }

            if (IsStationary)
            {
                UpdateSweep();
                return;
            }

            if (_motor.IsMoving || _waypoints.Length < 2)
            {
                return;
            }

            _repathTimeLeft -= Time.deltaTime;
            if (_repathTimeLeft > 0f)
            {
                return;
            }

            // 자리로 돌아온 직후에는 가던 웨이포인트로 마저 간다.
            if (_resumeWaypoint)
            {
                _resumeWaypoint = false;
            }
            else
            {
                _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
            }

            if (!_motor.MoveTo(_waypoints[_waypointIndex].position))
            {
                // 이 웨이포인트로는 길이 없다. 잠시 뒤 다음 웨이포인트를 시도한다.
                _repathTimeLeft = RepathDelay;
            }
        }

        private void UpdateSweep()
        {
            _sweepOffset += _sweepDirection * _data.SweepSpeed * Time.deltaTime;
            if (Mathf.Abs(_sweepOffset) >= _data.SweepAngle)
            {
                _sweepOffset = Mathf.Sign(_sweepOffset) * _data.SweepAngle;
                _sweepDirection = -_sweepDirection;
            }

            _motor.TurnTowards(Quaternion.Euler(0f, 0f, _sweepOffset) * _sweepCenter);
        }

        // ---------- 경계 ----------

        private void BeginAlert()
        {
            RememberReturnPosition();
            SetState(State.Alert);
            _motor.Stop();
            _lastSeenPosition = PlayerPosition;
        }

        // 멈춰서 플레이어 쪽을 본다. 시간이 끝났을 때 계속 보이면 사격, 사라졌으면 마지막으로 본 곳으로 추격.
        private void UpdateAlert()
        {
            bool visible = CanSeePlayer(false);
            if (visible)
            {
                _lastSeenPosition = PlayerPosition;
            }

            _motor.TurnTowards(_lastSeenPosition - Position);

            if (_stateTime < _data.AlertDuration)
            {
                return;
            }

            if (visible)
            {
                SetState(State.Attack);
            }
            else
            {
                BeginChase(_lastSeenPosition);
            }
        }

        // ---------- 사격 ----------

        private void UpdateAttack()
        {
            // 이미 플레이어를 알아챈 뒤라 몸을 돌려 따라간다. 그래서 시야각은 보지 않고 거리와 벽만 본다.
            if (!CanSeePlayer(false))
            {
                _attack.Cancel();
                BeginChase(_lastSeenPosition);
                return;
            }

            _lastSeenPosition = PlayerPosition;
            float distance = Vector2.Distance(Position, PlayerPosition);
            bool inRange = distance <= _data.Range;

            _attack.Tick(inRange);

            if (_attack.IsWindingUp)
            {
                // 충전 중에는 서서 쏠 방향을 본다.
                _motor.Stop();
                _motor.TurnTowards(_attack.AimDirection);
                return;
            }

            if (_data.ChaseSpeed > 0f && distance > _data.Range)
            {
                // 보이지만 사거리 밖이면 다가간다.
                _motor.Speed = _data.ChaseSpeed;
                _repathTimeLeft -= Time.deltaTime;
                if (_repathTimeLeft <= 0f)
                {
                    _repathTimeLeft = ApproachRepathInterval;
                    _motor.MoveTo(PlayerPosition);
                }
            }
            else if (distance <= _data.Range * StopRangeRatio || _data.ChaseSpeed <= 0f)
            {
                _motor.Stop();
            }

            if (!_motor.IsMoving)
            {
                _motor.TurnTowards(PlayerPosition - Position);
            }
        }

        // ---------- 추격 ----------

        private void BeginChase(Vector2 target)
        {
            RememberReturnPosition();
            SetState(State.Chase);
            _chaseTarget = target;
            _motor.Speed = _data.ChaseSpeed;

            if (_data.ChaseSpeed > 0f)
            {
                _motor.MoveTo(target);
            }
            else
            {
                _motor.Stop();
            }
        }

        // 마지막으로 본 곳(또는 소리가 난 곳)까지 간다. 정해진 시간 동안 못 찾으면 원래 자리로 돌아간다.
        private void UpdateChase()
        {
            if (CanSeePlayer(true))
            {
                _repathTimeLeft = 0f;
                SetState(State.Attack);
                return;
            }

            if (_stateTime >= _data.ChaseTimeout)
            {
                BeginReturn();
                return;
            }

            if (!_motor.IsMoving)
            {
                // 움직이지 못하는 로봇은 그쪽을 바라보기만 한다.
                _motor.TurnTowards(_chaseTarget - Position);
            }
        }

        // ---------- 복귀 ----------

        private void BeginReturn()
        {
            _attack.Cancel();
            SetState(State.Return);
            _motor.Speed = _data.PatrolSpeed;
            _repathTimeLeft = 0f;

            if (IsStationary)
            {
                _motor.Stop();
            }
            else if (!_motor.MoveTo(_returnPosition))
            {
                _repathTimeLeft = RepathDelay;
            }
        }

        // 순찰하다 벗어난 자리로 걸어서 돌아간 뒤 순찰을 다시 시작한다.
        private void UpdateReturn()
        {
            if (CanSeePlayer(true))
            {
                BeginAlert();
                return;
            }

            if (IsStationary || Vector2.Distance(Position, _returnPosition) <= ReturnArriveDistance)
            {
                SetState(State.Patrol);
                _resumeWaypoint = true;
                _repathTimeLeft = 0f;
                return;
            }

            if (_motor.IsMoving)
            {
                return;
            }

            // 밀려서 길이 끊겼거나 길을 못 찾았다. 잠시 뒤 다시 찾는다.
            _repathTimeLeft -= Time.deltaTime;
            if (_repathTimeLeft <= 0f)
            {
                _repathTimeLeft = RepathDelay;
                _motor.MoveTo(_returnPosition);
            }
        }

        // ---------- 감지 ----------

        // 기획서 5.2: 거리, 시야각, 벽 세 조건을 모두 만족해야 보인다.
        private bool CanSeePlayer(bool checkAngle)
        {
            if (!IsPlayerAlive)
            {
                return false;
            }

            Vector2 toPlayer = PlayerPosition - Position;
            if (toPlayer.magnitude > _data.SightDistance)
            {
                return false;
            }

            if (checkAngle && Vector2.Angle(_motor.Facing, toPlayer) > _data.SightAngle * 0.5f)
            {
                return false;
            }

            return Physics2D.Linecast(Position, PlayerPosition, _wallMask).collider == null;
        }

        private void OnGunNoise(Vector2 position)
        {
            if (_data != null && Vector2.Distance(Position, position) <= _data.HearingRadius)
            {
                Investigate(position);
            }
        }

        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            // 맞은 자리가 아니라 쏜 쪽(플레이어)이 있는 곳으로 간다.
            if (_data != null && IsPlayerAlive)
            {
                Investigate(PlayerPosition);
            }
        }

        // 총소리를 듣거나 맞으면 그 위치로 추격한다. 이미 플레이어를 보고 싸우는 중이면 그대로 싸운다.
        private void Investigate(Vector2 position)
        {
            if (_state == State.Attack || !IsPlayerAlive)
            {
                return;
            }

            BeginChase(position);
        }

        // ---------- 공통 ----------

        private void SetState(State state)
        {
            _state = state;
            _stateTime = 0f;
            _alertMark.SetActive(state == State.Alert);
            if (state == State.Alert)
            {
                Sfx.PlayAt(SfxId.RobotAlert, Position);
            }
        }

        // 순찰에서 벗어나는 순간의 자리를 기억한다. 복귀 중에 다시 벗어날 때는 처음 자리를 유지한다.
        private void RememberReturnPosition()
        {
            if (_state == State.Patrol)
            {
                _returnPosition = Position;
            }
        }

        private void OnDied()
        {
            // 쓰러진 자리에 잔해를 남긴다 (기획서 6.2). 무엇이 나오는지는 로봇 종류마다 다르다.
            Lootable wreck = Instantiate(_wreckPrefab, transform.position, Quaternion.identity);
            wreck.name = $"{_data.DisplayName} 잔해";
            wreck.SetTable(_data.LootTable);

            var wreckRenderer = wreck.GetComponentInChildren<SpriteRenderer>();
            if (wreckRenderer != null)
            {
                // 어느 로봇의 잔해인지 알 수 있게 그 로봇의 색을 입힌다. 어두운 바닥에서도 보이도록 조금만 어둡게 한다.
                wreckRenderer.color = Color.Lerp(_data.Color, Color.black, 0.25f);
            }

            if (_explosionPrefab != null)
            {
                Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
            }

            // 로봇을 부수는 것은 플레이어뿐이다.
            if (PlayerProgress.Current != null)
            {
                PlayerProgress.Current.AddXp(_data.XpReward);
            }

            Sfx.PlayAt(SfxId.RobotDestroyed, Position);
            Destroy(gameObject);
        }
    }
}
