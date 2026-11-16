using UnityEngine;

namespace EscapeFromSpace
{
    // 플레이어의 겉모습: 우주인 그림을 조준 방향으로 통째로 돌리고, 든 무기의 그림을 손에 붙이고, 걷기·구르기·휘두르기를 움직임으로 보여 준다.
    // 위에서 본 그림 한 장을 돌려 쓰는 방식이라 동작은 그림을 바꾸지 않고 회전과 크기로 만든다 (2026-11-16 사용자 결정).
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private PlayerShooter _shooter;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _weapon;
        [SerializeField, Tooltip("탄이 나가는 점. 든 무기 그림의 끝으로 옮긴다")] private Transform _muzzle;

        // 그림은 모두 오른쪽을 보고 있어야 한다.
        [Header("몸")]
        [SerializeField, Tooltip("맨손일 때")] private Sprite _unarmedSprite;
        [SerializeField, Tooltip("무기를 들었을 때 (팔을 앞으로 뻗은 자세)")] private Sprite _armedSprite;
        [SerializeField, Tooltip("칸. 몸 중심에서 무기를 쥐는 손까지")] private float _handOffset = 0.2f;

        [Header("걷기")]
        [SerializeField, Tooltip("1칸을 걷는 동안의 걸음 수")] private float _stepsPerUnit = 1.1f;
        [SerializeField, Tooltip("도. 걸을 때 몸이 좌우로 흔들리는 각")] private float _swayAngle = 7f;
        [SerializeField, Tooltip("걸을 때 몸이 커졌다 작아지는 비율 (0.05 = 5%)")] private float _bobScale = 0.05f;
        [SerializeField, Tooltip("멈췄을 때 흔들림이 사라지는 빠르기")] private float _settleSpeed = 12f;

        [Header("구르기")]
        [SerializeField, Tooltip("구르는 동안 몸이 뒤집히는 횟수")] private float _rollTumbles = 2f;
        [SerializeField, Tooltip("뒤집힐 때 몸이 구르는 방향으로 납작해지는 비율 (0.6 = 60%)")] private float _rollSquash = 0.6f;

        [Header("휘두르기 (근접 무기)")]
        [SerializeField, Tooltip("초. 무기가 범위의 왼쪽 끝에서 오른쪽 끝까지 가는 시간")] private float _swingDuration = 0.15f;

        private Vector3 _bodyScale;
        private Vector2 _lastPosition;
        private float _walkPhase;
        private float _walkAmount;
        private WeaponData _shownWeapon;
        private bool _hasShownWeapon;

        private void Awake()
        {
            _bodyScale = _body.transform.localScale;
            _lastPosition = transform.position;
        }

        // 이동과 조준이 모두 끝난 뒤에 그려야 한 프레임 늦지 않는다.
        private void LateUpdate()
        {
            UpdateWeapon();

            Vector2 position = transform.position;
            float moved = Vector2.Distance(position, _lastPosition);
            _lastPosition = position;

            if (_roll.IsRolling)
            {
                DrawRoll();
                return;
            }

            // 걸음은 실제로 움직인 거리로 센다. 무거워서 느리면 걸음도 느려지고, 벽에 막혀 제자리면 멈춘다.
            bool walking = _controller.CanAct && _input.Move.sqrMagnitude > 0.01f && moved > 0.0005f;
            if (walking)
            {
                _walkPhase += moved * _stepsPerUnit * Mathf.PI;
            }

            _walkAmount = Mathf.MoveTowards(_walkAmount, walking ? 1f : 0f, _settleSpeed * Time.deltaTime);

            float sway = Mathf.Sin(_walkPhase) * _swayAngle * _walkAmount;
            float bob = Mathf.Abs(Mathf.Sin(_walkPhase)) * _bobScale * _walkAmount;

            _body.transform.rotation = Quaternion.Euler(0f, 0f, AngleOf(_controller.AimDirection) + sway);
            _body.transform.localScale = _bodyScale * (1f + bob);
        }

        // 위에서 보면 앞구르기는 몸이 구르는 방향으로 납작해졌다 펴지는 것으로 보인다. 무기는 몸에 가려진 것으로 치고 숨긴다.
        private void DrawRoll()
        {
            float tumble = Mathf.Abs(Mathf.Sin(_roll.Progress * Mathf.PI * _rollTumbles));
            _body.transform.rotation = Quaternion.Euler(0f, 0f, AngleOf(_roll.Direction));
            _body.transform.localScale = new Vector3(_bodyScale.x * (1f - _rollSquash * tumble), _bodyScale.y, _bodyScale.z);
            _weapon.enabled = false;
            _walkAmount = 0f;
        }

        // 무기 슬롯에 든 무기의 그림을 손에 붙인다. 그림의 길이는 무기 데이터의 값에 맞춘다.
        private void UpdateWeapon()
        {
            ItemStack equipped = _inventory.GetEquipped(EquipSlot.Weapon);
            WeaponData weapon = equipped != null ? equipped.Data.Weapon : null;
            bool visible = weapon != null && weapon.Sprite != null;
            _weapon.enabled = visible;

            if (!_hasShownWeapon || weapon != _shownWeapon)
            {
                _hasShownWeapon = true;
                _shownWeapon = weapon;
                _body.sprite = weapon != null ? _armedSprite : _unarmedSprite;

                if (visible)
                {
                    // 무기 그림은 위를 보고 있어서 길이는 그림의 세로다.
                    _weapon.sprite = weapon.Sprite;
                    float scale = weapon.VisualLength / weapon.Sprite.bounds.size.y;
                    _weapon.transform.localScale = new Vector3(scale, scale, 1f);

                    // 탄은 무기 그림의 끝에서 나간다 (2026-11-16 사용자 결정).
                    _muzzle.localPosition = new Vector3(_handOffset + weapon.VisualLength, 0f, 0f);
                }
            }

            if (!visible)
            {
                return;
            }

            // 근접 무기를 휘두르면 무기 그림이 범위의 왼쪽 끝에서 오른쪽 끝으로 지나간다. 판정은 휘두른 순간에 이미 끝났다.
            float swing = 0f;
            float sinceSwing = Time.time - _shooter.LastSwingTime;
            if (weapon.Category == WeaponCategory.Melee && _swingDuration > 0f && sinceSwing >= 0f && sinceSwing < _swingDuration)
            {
                float half = weapon.SpreadAngle * 0.5f;
                swing = Mathf.Lerp(half, -half, sinceSwing / _swingDuration);
            }

            // 조준 방향(부모의 +X)에서 swing만큼 돌린 방향으로 손에서부터 뻗는다. 그림이 위를 보고 있어서 90° 눕힌다.
            Quaternion turn = Quaternion.Euler(0f, 0f, swing);
            _weapon.transform.localPosition = turn * new Vector3(_handOffset + weapon.VisualLength * 0.5f, 0f, 0f);
            _weapon.transform.localRotation = Quaternion.Euler(0f, 0f, swing - 90f);
        }

        private static float AngleOf(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }
}
