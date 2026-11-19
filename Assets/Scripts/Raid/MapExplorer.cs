using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace
{
    // 전체 맵에 보일 "가 본 곳"을 칸 단위로 기록한다 (기획서 9.2). 그리는 일은 MapUI가 한다.
    public class MapExplorer : MonoBehaviour
    {
        [SerializeField] private Tilemap _walls;
        [SerializeField] private LayerMask _wallMask;
        [SerializeField] private Transform _player;
        [SerializeField, Tooltip("칸. 이 거리 안에서 벽에 가리지 않은 칸이 가 본 곳으로 기록된다")] private float _revealRadius = 7f;
        [SerializeField, Tooltip("초. 기록을 갱신하는 간격")] private float _interval = 0.2f;

        private BoundsInt _bounds;
        private bool[] _isWall;
        private bool[] _explored;
        private float _timeLeft;

        public int Width { get; private set; }
        public int Height { get; private set; }

        // 기록이 바뀔 때마다 올라간다. 화면은 이 값이 달라졌을 때만 다시 그린다.
        public int Version { get; private set; }

        public bool IsWall(int x, int y)
        {
            return _isWall[y * Width + x];
        }

        public bool IsExplored(int x, int y)
        {
            return _explored[y * Width + x];
        }

        // 월드 좌표를 맵 왼쪽 아래 (0, 0) ~ 오른쪽 위 (1, 1)의 비율로 바꾼다.
        public Vector2 Normalize(Vector2 world)
        {
            Vector3 min = _walls.CellToWorld(_bounds.min);
            Vector3 max = _walls.CellToWorld(_bounds.max);
            return new Vector2(Mathf.InverseLerp(min.x, max.x, world.x), Mathf.InverseLerp(min.y, max.y, world.y));
        }

        // 칸 (x, y)의 가운데 월드 좌표. 전체 맵이 칸마다 어느 구역인지 정할 때 쓴다.
        public Vector2 CellCenter(int x, int y)
        {
            return _walls.GetCellCenterWorld(new Vector3Int(_bounds.xMin + x, _bounds.yMin + y, 0));
        }

        public bool IsExploredAt(Vector2 world)
        {
            Vector3Int cell = _walls.WorldToCell(world);
            int x = cell.x - _bounds.xMin;
            int y = cell.y - _bounds.yMin;
            return x >= 0 && y >= 0 && x < Width && y < Height && IsExplored(x, y);
        }

        // 가 본 곳은 다음 판에도 보인다. 레이드가 끝날 때 저장 내용에 담는다.
        public void SaveTo(SaveData data)
        {
            var text = new StringBuilder(_explored.Length);
            foreach (bool explored in _explored)
            {
                text.Append(explored ? '1' : '0');
            }

            data.MapWidth = Width;
            data.MapHeight = Height;
            data.Explored = text.ToString();
        }

        private void Awake()
        {
            _walls.CompressBounds();
            _bounds = _walls.cellBounds;
            Width = _bounds.size.x;
            Height = _bounds.size.y;
            _isWall = new bool[Width * Height];
            _explored = new bool[Width * Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _isWall[y * Width + x] = _walls.HasTile(new Vector3Int(_bounds.xMin + x, _bounds.yMin + y, 0));
                }
            }

            // 맵 크기가 저장할 때와 다르면(맵을 고침) 칸이 어긋나므로 처음부터 다시 기록한다.
            SaveData data = GameSession.RaidFromLobby ? GameSession.Data : null;
            if (data != null && data.MapWidth == Width && data.MapHeight == Height && data.Explored.Length == _explored.Length)
            {
                for (int i = 0; i < _explored.Length; i++)
                {
                    _explored[i] = data.Explored[i] == '1';
                }
            }
        }

        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft > 0f)
            {
                return;
            }

            _timeLeft = _interval;
            Reveal(_player.position);
        }

        private void Reveal(Vector2 position)
        {
            Vector3Int center = _walls.WorldToCell(position);
            int reach = Mathf.CeilToInt(_revealRadius);
            bool changed = false;

            for (int cy = center.y - reach; cy <= center.y + reach; cy++)
            {
                for (int cx = center.x - reach; cx <= center.x + reach; cx++)
                {
                    int x = cx - _bounds.xMin;
                    int y = cy - _bounds.yMin;
                    if (x < 0 || y < 0 || x >= Width || y >= Height || _explored[y * Width + x])
                    {
                        continue;
                    }

                    Vector2 cellCenter = _walls.GetCellCenterWorld(new Vector3Int(cx, cy, 0));
                    if (Vector2.Distance(position, cellCenter) > _revealRadius)
                    {
                        continue;
                    }

                    // 벽 너머의 칸은 기록하지 않는다. 처음 막힌 곳이 그 칸 자신이면(보이는 벽면) 기록한다.
                    RaycastHit2D hit = Physics2D.Linecast(position, cellCenter, _wallMask);
                    if (hit.collider == null || (hit.point - cellCenter).sqrMagnitude < 0.75f * 0.75f)
                    {
                        _explored[y * Width + x] = true;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                Version++;
            }
        }
    }
}
