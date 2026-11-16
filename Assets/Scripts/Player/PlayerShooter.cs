using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    public class PlayerShooter : MonoBehaviour
    {
        // 가방과 무기 슬롯이 생기기 전까지 쓰는 임시 연결.
        // 그때까지는 Play 중에 이 칸에 다른 무기 에셋을 끌어다 놓아 바꾼다.
        [SerializeField] private WeaponData _weapon;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private Transform _muzzle;
        [SerializeField, Tooltip("사격이 맞는 레이어 (벽, 적)")] private LayerMask _hitMask;
        [SerializeField] private LayerMask _wallMask;
        [SerializeField] private GameObject _hitMarkerPrefab;
        [SerializeField] private BulletTracer _tracerPrefab;
        [SerializeField] private SwingArc _swingArcPrefab;

        [Header("탄약")]
        // 가방(11/17)이 생기면 가방의 탄약으로 바꾼다. 그때까지는 여기에 직접 넣는다.
        [SerializeField, Tooltip("예비탄 (임시)")] private int _reserveAmmo;

        // HUD가 생기기 전까지 Inspector에서 탄창을 보려고 노출한다.
        [SerializeField] private int _ammoInMagazine;

        // 쌓인 반동(도). Inspector에서 보려고 노출한다.
        [SerializeField] private float _currentRecoil;

        // 탄창 상태가 어느 무기의 것인지. Inspector에서 무기를 바꾼 것을 알아채는 데 쓴다.
        private WeaponData _equipped;
        private float _nextFireTime;
        private float _reloadTimeLeft;
        private CameraShake _cameraShake;

        // 한 번 휘두를 때 같은 대상을 두 번 맞히지 않으려고 쓴다. 매번 새로 만들지 않게 재사용한다.
        private readonly List<Health> _swingHits = new List<Health>();

        public bool IsReloading { get; private set; }
        public int AmmoInMagazine => _ammoInMagazine;
        public int ReserveAmmo => _reserveAmmo;
        public float CurrentRecoil => _currentRecoil;

        // 이번 발에 적용되는 퍼짐(±도). 이동 중에는 기본 퍼짐과 쌓인 반동을 더한 전체가 2배가 된다.
        public float CurrentSpread => (BaseSpread + _currentRecoil) * MoveSpreadScale;

        // 저격총은 우클릭을 누르는 동안 기본 퍼짐만 줄어든다. 반동 누적과 이동 중 2배는 그대로 받는다.
        public bool IsAiming => _weapon != null && _weapon.Category == WeaponCategory.Sniper && _input.AimHeld;

        private float BaseSpread => IsAiming ? _weapon.AimSpreadAngle : _weapon.SpreadAngle;

        private bool UsesAmmo => _weapon.MagazineSize > 0;
        private float MoveSpreadScale => _input.Move.sqrMagnitude > 0.01f ? 2f : 1f;

        private void Awake()
        {
            // 카메라는 씬에 있어서 프리팹에서 연결할 수 없다.
            _cameraShake = Camera.main.GetComponent<CameraShake>();
            _currentRecoil = 0f;

            if (_weapon != null)
            {
                _equipped = _weapon;
                _ammoInMagazine = _weapon.MagazineSize;
            }
        }

        private void Update()
        {
            if (_weapon == null)
            {
                return;
            }

            if (_weapon != _equipped)
            {
                OnWeaponSwapped();
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
            if (_weapon.Category == WeaponCategory.Melee)
            {
                Swing();
            }
            else
            {
                Fire();
            }

            // 반동은 쏜 뒤에 쌓인다. 첫 발은 기본 퍼짐만 받는다.
            _currentRecoil = Mathf.Min(_currentRecoil + _weapon.RecoilPerShot, _weapon.MaxRecoil);
            if (_cameraShake != null)
            {
                _cameraShake.AddRecoil(_weapon.RecoilPerShot);
            }
        }

        // 가방이 생기기 전까지의 임시 처리. 새 무기는 가득 찬 탄창과 탄창 2개분 예비탄으로 시작한다.
        private void OnWeaponSwapped()
        {
            _equipped = _weapon;
            _ammoInMagazine = _weapon.MagazineSize;
            _reserveAmmo = _weapon.MagazineSize * 2;
            _currentRecoil = 0f;
            _nextFireTime = 0f;
            IsReloading = false;
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

        // 근접 무기는 플레이어 앞쪽 부채꼴 안의 대상을 모두 맞힌다. 무기의 퍼짐 값이 부채꼴 전체 각이다.
        private void Swing()
        {
            Vector2 center = transform.position;
            Vector2 aim = _controller.AimDirection;
            float halfAngle = _weapon.SpreadAngle * 0.5f;

            // 휘두른 범위 표시. 플레이어가 움직여도 따라오도록 자식으로 둔다.
            float aimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            SwingArc arc = Instantiate(_swingArcPrefab, center, Quaternion.Euler(0f, 0f, aimAngle), transform);
            arc.Show(_weapon.Range, _weapon.SpreadAngle);

            _swingHits.Clear();
            foreach (Collider2D target in Physics2D.OverlapCircleAll(center, _weapon.Range, _hitMask & ~_wallMask))
            {
                var health = target.GetComponentInParent<Health>();
                if (health == null || _swingHits.Contains(health))
                {
                    continue;
                }

                // 가장 가까운 점이나 중심 중 하나라도 부채꼴 안에 있고 벽에 가리지 않으면 맞은 것으로 본다.
                Vector2 closest = target.ClosestPoint(center);
                Vector2 middle = target.bounds.center;
                Vector2 hitPoint;
                if (IsInSwing(center, aim, halfAngle, closest))
                {
                    hitPoint = closest;
                }
                else if (IsInSwing(center, aim, halfAngle, middle))
                {
                    hitPoint = middle;
                }
                else
                {
                    continue;
                }

                _swingHits.Add(health);
                Instantiate(_hitMarkerPrefab, hitPoint, Quaternion.identity);
                health.TakeDamage(_weapon.Damage, hitPoint);
            }
        }

        private bool IsInSwing(Vector2 center, Vector2 aim, float halfAngle, Vector2 point)
        {
            Vector2 toPoint = point - center;
            if (toPoint.magnitude > _weapon.Range)
            {
                return false;
            }

            // 대상과 겹쳐 있으면 방향을 잴 수 없다. 붙어 있는 것이므로 맞은 것으로 본다.
            if (toPoint.sqrMagnitude > 0.0001f && Vector2.Angle(aim, toPoint) > halfAngle)
            {
                return false;
            }

            return Physics2D.Linecast(center, point, _wallMask).collider == null;
        }

        private void Fire()
        {
            Vector2 center = transform.position;
            Vector2 muzzle = _muzzle.position;

            // 벽에 바짝 붙으면 총구가 벽 너머로 나간다. 그대로 쏘면 벽을 관통하므로 몸과 총구 사이의 벽을 먼저 확인한다.
            RaycastHit2D blocked = Physics2D.Linecast(center, muzzle, _wallMask);
            if (blocked.collider != null)
            {
                Instantiate(_hitMarkerPrefab, blocked.point, Quaternion.identity);
                return;
            }

            // 총소리. 벽에 막혀 탄이 못 나간 경우와 광선검은 소리를 내지 않는다.
            GunNoise.Emit(center);

            float moveScale = MoveSpreadScale;
            bool isFan = _weapon.Category == WeaponCategory.Shotgun;

            // 샷건의 퍼짐 값은 부채꼴 전체 각이다. 반동은 부채꼴의 중심 방향을 흔들고, 펠릿은 그 안에서 따로 흩어진다.
            float fanCenter = isFan ? Random.Range(-_currentRecoil, _currentRecoil) * moveScale : 0f;
            float fanHalf = _weapon.SpreadAngle * 0.5f * moveScale;
            float spread = CurrentSpread;

            for (int i = 0; i < _weapon.PelletCount; i++)
            {
                float angle = isFan ? fanCenter + Random.Range(-fanHalf, fanHalf) : Random.Range(-spread, spread);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * _controller.AimDirection;
                FirePellet(muzzle, direction);
            }
        }

        private void FirePellet(Vector2 muzzle, Vector2 direction)
        {
            RaycastHit2D hit = Physics2D.Raycast(muzzle, direction, _weapon.Range, _hitMask);
            Vector2 end = hit.collider != null ? hit.point : muzzle + direction * _weapon.Range;

            Instantiate(_tracerPrefab).Show(muzzle, end);

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
