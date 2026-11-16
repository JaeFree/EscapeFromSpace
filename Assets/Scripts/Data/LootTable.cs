using System;
using UnityEngine;

namespace EscapeFromSpace
{
    // 컨테이너와 로봇 잔해에서 무엇이 나오는지. 기획서에는 나오는 곳만 있어서 비율과 개수는 여기서 정한다.
    [CreateAssetMenu(fileName = "LootTable", menuName = "EscapeFromSpace/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private ItemData _item;
            [SerializeField, Tooltip("개수만큼 뽑을 때는 비율, 줄마다 따로 굴릴 때는 확률(%)")] private float _weight;

            public ItemData Item => _item;
            public float Weight => _weight;
        }

        [SerializeField, Tooltip("켜면 줄마다 확률(%)을 따로 굴린다. 끄면 아래 개수만큼 비율대로 뽑는다")] private bool _rollEachEntry;
        [SerializeField] private int _minCount;
        [SerializeField] private int _maxCount;
        [SerializeField] private Entry[] _entries;

        // 뽑은 아이템을 빈자리에 놓는다. 자리가 없으면 그 아이템은 건너뛴다.
        public void Fill(InventoryGrid grid)
        {
            if (_entries == null || _entries.Length == 0)
            {
                return;
            }

            if (_rollEachEntry)
            {
                foreach (Entry entry in _entries)
                {
                    if (entry.Item != null && UnityEngine.Random.value * 100f < entry.Weight)
                    {
                        grid.TryAdd(new ItemStack(entry.Item));
                    }
                }

                return;
            }

            float total = 0f;
            foreach (Entry entry in _entries)
            {
                total += entry.Weight;
            }

            int count = UnityEngine.Random.Range(_minCount, _maxCount + 1);
            for (int i = 0; i < count; i++)
            {
                float roll = UnityEngine.Random.value * total;
                foreach (Entry entry in _entries)
                {
                    roll -= entry.Weight;
                    if (roll <= 0f)
                    {
                        if (entry.Item != null)
                        {
                            grid.TryAdd(new ItemStack(entry.Item));
                        }

                        break;
                    }
                }
            }
        }
    }
}
