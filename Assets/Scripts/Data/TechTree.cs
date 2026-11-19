using System;
using UnityEngine;

namespace EscapeFromSpace
{
    // 강화로 오르는 능력치. 값의 단위는 TechTree.Describe를 본다.
    public enum TechStat
    {
        Damage,
        MaxHp,
        MaxWeight,
        ReloadSpeed,
        Oxygen,
        MoveSpeed,
        RollCooldown,
        LootSpeed,
        Armor,

        // 은신 갈래 (기획서 19.3, 2026-11-19 추가). 순서 번호가 저장되므로 뒤에 붙인다.
        SneakSpeed,
        DetectionReduction,
        CoverDetectionReduction,
        BackstabBonus,
    }

    // 레벨과 테크 트리 (기획서 12장, 2026-11-17 사용자 결정). 수치는 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    [CreateAssetMenu(fileName = "TechTree", menuName = "EscapeFromSpace/Tech Tree")]
    public class TechTree : ScriptableObject
    {
        [Serializable]
        public class Cost
        {
            [SerializeField] private ItemData _item;
            [SerializeField] private int _count = 1;

            public ItemData Item => _item;
            public int Count => _count;
        }

        [Serializable]
        public class Node
        {
            [SerializeField, Tooltip("저장 파일에 적히는 이름. 정한 뒤에는 바꾸지 않는다")] private string _id;
            [SerializeField] private string _displayName;
            [SerializeField] private TechStat _stat;
            [SerializeField, Tooltip("피해·재장전·이동·구르기·루팅은 %, 체력은 HP, 무게는 kg, 산소는 초, 방어는 방어력")] private float _amount;

            [Header("화면에서의 자리")]
            [SerializeField, Tooltip("왼쪽에서 몇 번째 열인지 (0부터)")] private int _column;
            [SerializeField, Tooltip("위에서 몇 번째 줄인지 (0부터)")] private int _row;

            [Header("조건과 비용")]
            [SerializeField, Tooltip("먼저 강화해야 하는 노드의 Id. 여러 개면 모두 필요하다")] private string[] _requires;
            [SerializeField] private int _requiredLevel = 1;
            [SerializeField] private int _pointCost = 1;
            [SerializeField] private Cost[] _costs;

            public string Id => _id;
            public string DisplayName => _displayName;
            public TechStat Stat => _stat;
            public float Amount => _amount;
            public int Column => _column;
            public int Row => _row;
            public string[] Requires => _requires;
            public int RequiredLevel => _requiredLevel;
            public int PointCost => _pointCost;
            public Cost[] Costs => _costs;
        }

        [Header("레벨")]
        [SerializeField] private int _maxLevel = 30;
        [SerializeField, Tooltip("레벨 1에서 2가 되는 데 필요한 경험치")] private int _xpForFirstLevel = 100;
        [SerializeField, Tooltip("레벨이 오를 때마다 다음 레벨에 필요한 경험치가 이만큼 늘어난다")] private int _xpGrowthPerLevel = 50;
        [SerializeField, Tooltip("레벨이 오를 때마다 받는 강화 포인트")] private int _pointsPerLevel = 1;
        [SerializeField, Tooltip("탈출에 성공하면 받는 경험치")] private int _escapeBonusXp = 50;

        [Header("노드")]
        [SerializeField] private Node[] _nodes;

        public int MaxLevel => _maxLevel;
        public int PointsPerLevel => _pointsPerLevel;
        public int EscapeBonusXp => _escapeBonusXp;
        public Node[] Nodes => _nodes;

        // 그 레벨에서 다음 레벨이 되는 데 필요한 경험치
        public int XpToNext(int level)
        {
            return Mathf.Max(1, _xpForFirstLevel + _xpGrowthPerLevel * (level - 1));
        }

        public Node Find(string id)
        {
            foreach (Node node in _nodes)
            {
                if (node.Id == id)
                {
                    return node;
                }
            }

            return null;
        }

        // 화면에 보이는 효과 설명
        public static string Describe(TechStat stat, float amount)
        {
            switch (stat)
            {
                case TechStat.Damage: return $"기본 피해 +{amount:0.#}%";
                case TechStat.MaxHp: return $"최대 체력 +{amount:0.#}";
                case TechStat.MaxWeight: return $"무게 상한 +{amount:0.#}kg";
                case TechStat.ReloadSpeed: return $"재장전 시간 −{amount:0.#}%";
                case TechStat.Oxygen: return $"산소 +{amount:0.#}초";
                case TechStat.MoveSpeed: return $"이동 속도 +{amount:0.#}%";
                case TechStat.RollCooldown: return $"구르기 쿨타임 −{amount:0.#}%";
                case TechStat.LootSpeed: return $"뒤지는 시간 −{amount:0.#}%";
                case TechStat.SneakSpeed: return $"살금 걷기 속도 +{amount:0.#}%";
                case TechStat.DetectionReduction: return $"발견 게이지 −{amount:0.#}%";
                case TechStat.CoverDetectionReduction: return $"엄폐 중 발견 게이지 −{amount:0.#}%";
                case TechStat.BackstabBonus: return $"뒤에서 베기 피해 +{amount:0.#}배";
                default: return $"방어력 +{amount:0.#}";
            }
        }
    }
}
