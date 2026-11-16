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
        private InputAction _fire;
        private InputAction _reload;
        private InputAction _aimHold;
        private InputAction _interact;
        private InputAction _toggleBag;
        private readonly InputAction[] _tools = new InputAction[3];

        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 AimScreenPosition => _aim.ReadValue<Vector2>();
        public bool RollPressedThisFrame => _roll.WasPressedThisFrame();
        public bool FireHeld => _fire.IsPressed();
        public bool FirePressedThisFrame => _fire.WasPressedThisFrame();
        public bool ReloadPressedThisFrame => _reload.WasPressedThisFrame();
        public bool AimHeld => _aimHold.IsPressed();
        public bool InteractHeld => _interact.IsPressed();
        public bool ToggleBagPressedThisFrame => _toggleBag.WasPressedThisFrame();

        // 도구칸 1, 2, 3. index는 0부터.
        public bool ToolPressedThisFrame(int index)
        {
            return _tools[index].WasPressedThisFrame();
        }

        private void Awake()
        {
            _playerMap = _actions.FindActionMap("Player", true);
            _move = _playerMap.FindAction("Move", true);
            _aim = _playerMap.FindAction("Aim", true);
            _roll = _playerMap.FindAction("Roll", true);
            _fire = _playerMap.FindAction("Fire", true);
            _reload = _playerMap.FindAction("Reload", true);
            _aimHold = _playerMap.FindAction("AimHold", true);
            _interact = _playerMap.FindAction("Interact", true);
            _toggleBag = _playerMap.FindAction("ToggleBag", true);
            for (int i = 0; i < _tools.Length; i++)
            {
                _tools[i] = _playerMap.FindAction($"Tool{i + 1}", true);
            }
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
