using UnityEngine;

namespace EscapeFromSpace
{
    public enum WeaponCategory
    {
        Melee,
        Pistol,
        AssaultRifle,
        Sniper,
        Shotgun,
    }

    // 기획서 4.2 무기 표의 한 줄. 수치는 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    [CreateAssetMenu(fileName = "WeaponData", menuName = "EscapeFromSpace/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private WeaponCategory _category;

        [Header("사격")]
        [SerializeField, Tooltip("한 발(펠릿 하나)의 피해")] private float _damage;
        [SerializeField, Tooltip("한 번 쏠 때 나가는 펠릿 수")] private int _pelletCount;
        [SerializeField, Tooltip("발/초")] private float _fireRate;
        [SerializeField, Tooltip("칸")] private float _range;

        [Header("탄창")]
        [SerializeField, Tooltip("0이면 탄을 쓰지 않는다")] private int _magazineSize;
        [SerializeField, Tooltip("초")] private float _reloadTime;
        [SerializeField, Tooltip("이 무기가 쓰는 탄약 아이템. 가방에 있는 이 탄약이 예비탄이다")] private ItemData _ammoItem;

        [Header("탄퍼짐과 반동")]
        [SerializeField, Tooltip("도. 총은 ±이 값, 샷건은 부채꼴 전체 각, 근접 무기는 공격 범위 각도")] private float _spreadAngle;
        [SerializeField, Tooltip("도. 저격총을 우클릭으로 조준하는 동안의 퍼짐. 저격총만 쓴다")] private float _aimSpreadAngle;
        [SerializeField, Tooltip("도/발")] private float _recoilPerShot;
        [SerializeField, Tooltip("도/초. 쌓인 반동이 줄어드는 속도")] private float _recoilRecovery;
        [SerializeField, Tooltip("도. 반동이 이 값보다 더 쌓이지 않는다")] private float _maxRecoil;

        [Header("가방")]
        [SerializeField, Tooltip("kg")] private float _weight;

        [Header("모양")]
        [SerializeField, Tooltip("손에 든 모습. 총구(칼끝)가 위를 보는 그림")] private Sprite _sprite;
        [SerializeField, Tooltip("칸. 손에 들었을 때 그림의 길이. 사거리나 총구 위치와는 상관없다")] private float _visualLength;

        [Header("소리")]
        [SerializeField, Tooltip("쏘거나 휘두를 때")] private SoundCue _fireSound = new SoundCue();

        public SoundCue FireSound => _fireSound;
        public string DisplayName => _displayName;
        public WeaponCategory Category => _category;
        public float Damage => _damage;
        public int PelletCount => _pelletCount;
        public float FireRate => _fireRate;
        public float Range => _range;
        public int MagazineSize => _magazineSize;
        public float ReloadTime => _reloadTime;
        public ItemData AmmoItem => _ammoItem;
        public float SpreadAngle => _spreadAngle;
        public float AimSpreadAngle => _aimSpreadAngle;
        public float RecoilPerShot => _recoilPerShot;
        public float RecoilRecovery => _recoilRecovery;
        public float MaxRecoil => _maxRecoil;
        public float Weight => _weight;
        public Sprite Sprite => _sprite;
        public float VisualLength => _visualLength;
    }
}
