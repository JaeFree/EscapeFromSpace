using UnityEngine;

namespace EscapeFromSpace
{
    public class PlayerShooter : MonoBehaviour
    {
        // 가방과 무기 슬롯이 생기기 전까지 쓰는 임시 연결.
        [SerializeField] private WeaponData _weapon;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private Transform _muzzle;
        [SerializeField, Tooltip("사격이 맞는 레이어 (벽, 적)")] private LayerMask _hitMask;
        [SerializeField] private LayerMask _wallMask;
        [SerializeField] private GameObject _hitMarkerPrefab;

        [Header("탄약")]
        // 가방(11/17)이 생기면 가방의 탄약으로 바꾼다. 그때까지는 여기에 직접 넣는다.
        [SerializeField, Tooltip("예비탄 (임시)")] private int _reserveAmmo;

        // HUD가 생기기 전까지 Inspector에서 탄창을 보려고 노출한다.
        [SerializeField] private int _ammoInMagazine;

        // 쌓인 반동(도). Inspector에서 보려고 노출한다.
        [SerializeField] private float _currentRecoil;

        private float _nextFireTime;
        private float _reloadTimeLeft;
        private CameraShake _cameraShake;

        public bool IsReloading { get; private set; }
        public int AmmoInMagazine => _ammoInMagazine;
        public int ReserveAmmo => _reserveAmmo;

        private bool UsesAmmo => _weapon.MagazineSize > 0;

        public float CurrentRecoil => _currentRecoil;

        // 이번 발에 적용되는 퍼짐(±도). 이동 중에는 기본 퍼짐과 쌓인 반동을 더한 전체가 2배가 된다.
        public float CurrentSpread
        {
            get
            {
                float spread = _weapon.SpreadAngle + _currentRecoil;
                return _input.Move.sqrMagnitude > 0.01f ? spread * 2f : spread;
            }
        }

        private void Awake()
        {
            // 카메라는 씬에 있어서 프리팹에서 연결할 수 없다.
            _cameraShake = Camera.main.GetComponent<CameraShake>();
            _currentRecoil = 0f;

            if (_weapon != null)
            {
                _ammoInMagazine = _weapon.MagazineSize;
            }
        }

        private void Update()
        {
            if (_weapon == null)
            {
                return;
            }

            UpdateReload();

            // 반동은 지연 없이 항상 회복한다 (연사 사이에도).
            _currentRecoil = Mathf.Max(0f, _currentRecoil - _weapon.RecoilRecovery * Time.deltaTime);

            if (!_input.FireHeld || !_controller.CanAct || _roll.IsRolling || IsReloading || Time.time < _nextFireTime)
            {
                return;
            }

            if (UsesAmmo)
            {
                if (_ammoInMagazine <= 0)
                {
                    return;
                }

                _ammoInMagazine--;
            }

            _nextFireTime = Time.time + 1f / _weapon.FireRate;
            Fire();

            // 반동은 쏜 뒤에 쌓인다. 첫 발은 기본 퍼짐만 받는다.
            _currentRecoil = Mathf.Min(_currentRecoil + _weapon.RecoilPerShot, _weapon.MaxRecoil);
            if (_cameraShake != null)
            {
                _cameraShake.AddRecoil(_weapon.RecoilPerShot);
            }
        }

        private void UpdateReload()
        {
            // 구르면 재장전이 취소된다. 탄창이 비어 있으면 구르기가 끝난 뒤 아래에서 다시 자동으로 시작한다.
            if (_roll.IsRolling || _controller.IsDead)
            {
                IsReloading = false;
                return;
            }

            if (IsReloading)
            {
                _reloadTimeLeft -= Time.deltaTime;
                if (_reloadTimeLeft <= 0f)
                {
                    FinishReload();
                }

                return;
            }

            if (!UsesAmmo || _reserveAmmo <= 0 || _ammoInMagazine >= _weapon.MagazineSize)
            {
                return;
            }

            // 탄을 다 쓰면 클릭 없이 자동으로 재장전한다. 탄이 남아 있으면 R키로만 한다.
            if (_ammoInMagazine <= 0 || _input.ReloadPressedThisFrame)
            {
                IsReloading = true;
                _reloadTimeLeft = _weapon.ReloadTime;
            }
        }

        private void FinishReload()
        {
            // 남은 탄은 유지하고 모자란 만큼만 예비탄에서 채운다.
            int loaded = Mathf.Min(_weapon.MagazineSize - _ammoInMagazine, _reserveAmmo);
            _ammoInMagazine += loaded;
            _reserveAmmo -= loaded;
            IsReloading = false;
        }

        private void Fire()
        {
            Vector2 center = transform.position;
            Vector2 muzzle = _muzzle.position;

            // 벽에 바짝 붙으면 총구가 벽 너머로 나간다. 그대로 쏘면 벽을 관통하므로 몸과 총구 사이의 벽을 먼저 확인한다.
            RaycastHit2D hit = Physics2D.Linecast(center, muzzle, _wallMask);
            if (hit.collider == null)
            {
                float spread = CurrentSpread;
                float angle = Random.Range(-spread, spread);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * _controller.AimDirection;
                hit = Physics2D.Raycast(muzzle, direction, _weapon.Range, _hitMask);
            }

            if (hit.collider == null)
            {
                return;
            }

            Instantiate(_hitMarkerPrefab, hit.point, Quaternion.identity);

            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(_weapon.Damage, hit.point);
            }
        }
    }
}
