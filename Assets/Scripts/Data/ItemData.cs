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
    }

    // 기획서 6.3 아이템 표와 6.5 방어구 표의 한 줄. 수치는 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    [CreateAssetMenu(fileName = "ItemData", menuName = "EscapeFromSpace/Item Data")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private ItemCategory _category;
        [SerializeField, Tooltip("단색 도형 단계에서 칸에 칠하는 색")] private Color _color = Color.white;

        [Header("가방")]
        [SerializeField, Tooltip("칸")] private int _width = 1;
        [SerializeField, Tooltip("칸")] private int _height = 1;
        [SerializeField, Tooltip("kg. 탄약은 기본 수량 한 묶음의 무게")] private float _weight;
        [SerializeField] private int _value;
        [SerializeField, Tooltip("한 칸에 쌓이는 최대 수량. 1이면 쌓이지 않는다")] private int _maxStack = 1;
        [SerializeField, Tooltip("루팅으로 나올 때의 수량 (탄약의 발 수)")] private int _defaultCount = 1;

        [Header("무기")]
        [SerializeField, Tooltip("무기 아이템이면 그 무기의 수치")] private WeaponData _weapon;

        [Header("방어구")]
        [SerializeField] private int _armor;
        [SerializeField, Tooltip("방탄복이 가방 아래에 붙이는 줄 수")] private int _extraBagRows;
        [SerializeField, Tooltip("초. 헬멧이 늘리는 산소")] private float _extraOxygenSeconds;

        [Header("사용 효과")]
        [SerializeField] private float _healAmount;
        [SerializeField, Tooltip("초")] private float _useTime;
        [SerializeField, Tooltip("초. 산소 캔이 채우는 산소")] private float _oxygenSeconds;

        public string DisplayName => _displayName;
        public ItemCategory Category => _category;
        public Color Color => _color;
        public int Width => _width;
        public int Height => _height;
        public float Weight => _weight;
        public int Value => _value;
        public int MaxStack => _maxStack;
        public int DefaultCount => _defaultCount;
        public WeaponData Weapon => _weapon;
        public int Armor => _armor;
        public int ExtraBagRows => _extraBagRows;
        public float ExtraOxygenSeconds => _extraOxygenSeconds;
        public float HealAmount => _healAmount;
        public float UseTime => _useTime;
        public float OxygenSeconds => _oxygenSeconds;
    }
}
