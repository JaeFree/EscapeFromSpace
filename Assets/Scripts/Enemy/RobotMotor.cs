using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 로봇의 이동과 회전. 속도를 쓰는 곳을 여기 하나로 둔다 (플레이어와 같은 이유).
    [RequireComponent(typeof(Rigidbody2D))]
    public class RobotMotor : MonoBehaviour
    {
        private const float ArriveDistance = 0.05f;

        [SerializeField, Tooltip("바라보는 방향으로 도는 자식")] private Transform _facingPivot;

        private readonly List<Vector2> _path = new List<Vector2>();
        private Rigidbody2D _rigidbody;
        private WallGrid _grid;
        private int _pathIndex;
        private float _turnSpeed;
        private Vector2 _turnTarget = Vector2.right;

        public Vector2 Facing { get; private set; } = Vector2.right;
        public float Speed { get; set; }
        public bool IsMoving => _pathIndex < _path.Count;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        public void Initialize(WallGrid grid, float turnSpeed, Vector2 facing)
        {
            _grid = grid;
            _turnSpeed = turnSpeed;
            Facing = facing;
            _turnTarget = facing;
            ApplyFacing();
        }

        // 길을 찾아 걷기 시작한다. 길이 없으면 false를 돌려주고 멈춘다.
        public bool MoveTo(Vector2 destination)
        {
            _pathIndex = 0;
            return _grid.FindPath(_rigidbody.position, destination, _path);
        }

        public void Stop()
        {
            _path.Clear();
            _pathIndex = 0;
        }

        // 걷는 중에는 걷는 방향을 보므로 멈춰 있을 때만 쓴다.
        public void TurnTowards(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                _turnTarget = direction.normalized;
            }
        }

        private void FixedUpdate()
        {
            Vector2 velocity = Vector2.zero;

            if (IsMoving && Speed > 0f)
            {
                Vector2 toPoint = _path[_pathIndex] - _rigidbody.position;
                float distance = toPoint.magnitude;
                if (distance <= ArriveDistance)
                {
                    _pathIndex++;
                }
                else
                {
                    // 한 틱에 지나치지 않게 남은 거리만큼만 간다.
                    Vector2 direction = toPoint / distance;
                    velocity = direction * Mathf.Min(Speed, distance / Time.fixedDeltaTime);
                    _turnTarget = direction;
                }
            }

            _rigidbody.linearVelocity = velocity;
        }

        private void Update()
        {
            float current = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg;
            float target = Mathf.Atan2(_turnTarget.y, _turnTarget.x) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(current, target, _turnSpeed * Time.deltaTime) * Mathf.Deg2Rad;
            Facing = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            _facingPivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg);
        }
    }
}
