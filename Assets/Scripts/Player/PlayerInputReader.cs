using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeFromSpace
{
    // 입력 액션 이름을 한곳에만 두려고 다른 플레이어 스크립트는 이 컴포넌트를 거쳐 입력을 읽는다.
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;

        private InputActionMap _playerMap;
        private InputAction _move;
        private InputAction _aim;
        private InputAction _roll;

        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 AimScreenPosition => _aim.ReadValue<Vector2>();
        public bool RollPressedThisFrame => _roll.WasPressedThisFrame();

        private void Awake()
        {
            _playerMap = _actions.FindActionMap("Player", true);
            _move = _playerMap.FindAction("Move", true);
            _aim = _playerMap.FindAction("Aim", true);
            _roll = _playerMap.FindAction("Roll", true);
        }

        private void OnEnable()
        {
            _playerMap.Enable();
        }

        private void OnDisable()
        {
            _playerMap.Disable();
        }
    }
}
