using UnityEngine;

namespace EscapeFromSpace
{
    public enum ItemCategory
    {
        Ammo,
        Medical,
        Consumable,
        Loot,
        Weapon,
        Helmet,
        Vest,

        // 2026-11-18 추가. 저장 파일과 에셋에 숫자로 적히므로 끝에만 덧붙인다.
        Attachment,
        PodPart,
    }

    // 총기 부착물의 부위 (기획서 17장). 무기 하나에 부위마다 하나씩 단다. 저장 파일의 순서이므로 끝에만 덧붙인다.
    public enum AttachmentSlot
    {
        Stock,
        Grip,
        Scope,
        Muzzle,
        Laser,
    }

    // 기획서 6.3 아이템 표와 6.5 방어구 표의 한 줄. 수치는 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    [CreateAssetMenu(fileName = "ItemData", menuName = "EscapeFromSpace/Item Data")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private ItemCategory _category;
        [SerializeField, Tooltip("칸의 바탕에 칠하는 종류별 색")] private Color _color = Color.white;
        [SerializeField, Tooltip("칸에 그리는 그림. 없으면 색과 이름만 나온다")] private Sprite _icon;

        [Header("가방")]
        [SerializeField, Tooltip("칸")] private int _width = 1;
        [SerializeField, Tooltip("칸")] private int _height = 1;
        [SerializeField, Tooltip("kg. 탄약은 기본 수량 한 묶음의 무게")] private float _weight;
        [SerializeField] private int _value;
        [SerializeField, Tooltip("한 칸에 쌓이는 최대 수량. 1이면 쌓이지 않는다")] private int _maxStack = 1;
        [SerializeField, Tooltip("루팅으로 나올 때의 수량 (탄약의 발 수)")] private int _defaultCount = 1;
        [SerializeField, Tooltip("켜면 암시장에서 팔 수 없다 (포드 부품 등, 기획서 16장)")] private bool _cannotSell;

        [Header("무기")]
        [SerializeField, Tooltip("무기 아이템이면 그 무기의 수치")] private WeaponData _weapon;

        [Header("방어구")]
        [SerializeField] private int _armor;
        [SerializeField, Tooltip("방탄복이 가방 아래에 붙이는 줄 수")] private int _extraBagRows;
        [SerializeField, Tooltip("초. 헬멧이 늘리는 산소")] private float _extraOxygenSeconds;

        [Header("탄약")]
        [SerializeField, Tooltip("이 탄으로 쏠 때 피해에 곱한다. 고출력 탄약은 1.25 (기획서 16장)")] private float _ammoDamageMultiplier = 1f;

        [Header("총기 부착물 (기획서 17장)")]
        [SerializeField] private AttachmentSlot _attachmentSlot;
        [SerializeField, Tooltip("반동(도/발)에 곱한다. 0.8이면 20% 감소")] private float _recoilMultiplier = 1f;
        [SerializeField, Tooltip("기본 퍼짐(샷건은 부채꼴)에 곱한다")] private float _spreadMultiplier = 1f;
        [SerializeField, Tooltip("이동 중 퍼짐 배율. 0이면 기본(2배) 그대로")] private float _moveSpreadScale;
        [SerializeField, Tooltip("저격총 조준 퍼짐에 곱한다")] private float _aimSpreadMultiplier = 1f;
        [SerializeField, Tooltip("켜면 총구에서 조준 레이저가 보인다")] private bool _laserSight;

        [Header("사용 효과")]
        [SerializeField] private float _healAmount;
        [SerializeField, Tooltip("초")] private float _useTime;
        [SerializeField, Tooltip("초. 산소 캔이 채우는 산소")] private float _oxygenSeconds;

        public string DisplayName => _displayName;
        public ItemCategory Category => _category;
        public Color Color => _color;
        public Sprite Icon => _icon;
        public int Width => _width;
        public int Height => _height;
        public float Weight => _weight;
        public int Value => _value;
        public int MaxStack => _maxStack;
        public int DefaultCount => _defaultCount;
        public bool CannotSell => _cannotSell;
        public WeaponData Weapon => _weapon;
        public AttachmentSlot AttachmentSlot => _attachmentSlot;
        public float AmmoDamageMultiplier => _ammoDamageMultiplier;
        public float RecoilMultiplier => _recoilMultiplier;
        public float SpreadMultiplier => _spreadMultiplier;
        public float MoveSpreadScale => _moveSpreadScale;
        public float AimSpreadMultiplier => _aimSpreadMultiplier;
        public bool LaserSight => _laserSight;
        public int Armor => _armor;
        public int ExtraBagRows => _extraBagRows;
        public float ExtraOxygenSeconds => _extraOxygenSeconds;
        public float HealAmount => _healAmount;
        public float UseTime => _useTime;
        public float OxygenSeconds => _oxygenSeconds;
    }
}
