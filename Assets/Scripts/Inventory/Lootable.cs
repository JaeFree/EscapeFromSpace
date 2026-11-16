using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 뒤질 수 있는 것: 컨테이너, 로봇 잔해, 바닥에 버린 아이템 (기획서 6.2).
    public class Lootable : MonoBehaviour
    {
        private static readonly List<Lootable> All = new List<Lootable>();

        [SerializeField, Tooltip("루팅 칸 가로")] private int _width = 3;
        [SerializeField, Tooltip("루팅 칸 세로")] private int _height = 2;
        [SerializeField] private LootTable _table;
        [SerializeField, Tooltip("초. E를 누르고 있어야 하는 시간")] private float _openTime = 1f;
        [SerializeField, Tooltip("초. 0이 아니면 이 시간이 지나 사라진다 (바닥에 버린 아이템)")] private float _lifetime;
        [SerializeField, Tooltip("비면 사라진다 (바닥에 버린 아이템)")] private bool _destroyWhenEmpty;

        private bool _filled;
        private float _age;

        public InventoryGrid Grid { get; private set; }
        public float OpenTime => _openTime;

        // 화면에서 아이템을 끌고 있는 동안에는 비어도 사라지지 않는다. 못 놓았을 때 돌아올 자리다.
        public bool KeepAlive { get; set; }

        // 한 번 연 것은 다음부터 E를 누르면 바로 열린다.
        public bool IsOpened { get; set; }

        // 플레이어 근처에서 가장 가까운 것. 없으면 null.
        public static Lootable FindNearest(Vector2 position, float range)
        {
            Lootable nearest = null;
            float best = range;
            foreach (Lootable lootable in All)
            {
                float distance = Vector2.Distance(position, lootable.transform.position);
                if (distance <= best)
                {
                    best = distance;
                    nearest = lootable;
                }
            }

            return nearest;
        }

        // 로봇 잔해처럼 만든 쪽이 내용물 표를 정해 줄 때 쓴다. Start보다 먼저 불러야 한다.
        public void SetTable(LootTable table)
        {
            _table = table;
        }

        // 내용물을 직접 넣는다 (바닥에 버린 아이템).
        public void SetContents(ItemStack stack)
        {
            EnsureGrid();
            Grid.TryAdd(stack);
            _filled = true;
        }

        private void Awake()
        {
            EnsureGrid();
        }

        private void OnEnable()
        {
            All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void Start()
        {
            if (!_filled && _table != null)
            {
                _table.Fill(Grid);
                _filled = true;
            }
        }

        private void Update()
        {
            if (_destroyWhenEmpty && Grid.Items.Count == 0 && !KeepAlive)
            {
                Destroy(gameObject);
                return;
            }

            if (_lifetime > 0f)
            {
                _age += Time.deltaTime;
                if (_age >= _lifetime)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void EnsureGrid()
        {
            if (Grid == null)
            {
                Grid = new InventoryGrid(_width, _height);
            }
        }
    }
}
