using UnityEngine;

namespace EscapeFromSpace
{
    public class PlayerRoll : MonoBehaviour
    {
        [SerializeField] private PlayerData _data;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private Health _health;

        private Vector2 _direction;
        private float _remainingDistance;
        private float _invincibleTimeLeft;

        public bool IsRolling { get; private set; }
        public float CooldownRemaining { get; private set; }

        private void Update()
        {
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);
            _invincibleTimeLeft = Mathf.Max(0f, _invincibleTimeLeft - Time.deltaTime);
            _health.IsInvincible = _invincibleTimeLeft > 0f;

            if (_input.RollPressedThisFrame && _controller.CanAct && !IsRolling && CooldownRemaining <= 0f)
            {
                StartRoll();
            }
        }

        // 이번 물리 틱에 쓸 구르기 속도. 속도를 쓰는 곳을 PlayerController 하나로 두려고 여기서는 값만 돌려준다.
        // 두 스크립트가 각자 속도를 쓰면 실행 순서에 따라 마지막 틱이 덮여 거리가 짧아진다.
        public Vector2 TakeStepVelocity()
        {
            // 시간으로 끝을 재면 물리 틱 반올림 때문에 거리가 조금씩 달라진다.
            // 남은 거리로 재서 벽에 막히지 않는 한 정확히 구르기 거리만큼 간다.
            float speed = _data.RollDistance / _data.RollDuration;
            float step = Mathf.Min(speed * Time.fixedDeltaTime, _remainingDistance);
            _remainingDistance -= step;

            if (_remainingDistance <= 0.0001f)
            {
                IsRolling = false;
            }

            return _direction * (step / Time.fixedDeltaTime);
        }

        private void StartRoll()
        {
            Vector2 move = _input.Move;
            _direction = move.sqrMagnitude > 0.01f ? move.normalized : _controller.AimDirection;
            _remainingDistance = _data.RollDistance;
            _invincibleTimeLeft = _data.RollInvincibleDuration;
            _health.IsInvincible = true;
            CooldownRemaining = _data.RollCooldown;
            IsRolling = true;
        }
    }
}
