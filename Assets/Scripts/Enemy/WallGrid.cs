using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace
{
    // 벽 타일맵을 칸 단위 격자로 읽어 로봇의 길을 찾는다 (A*).
    // 맵이 60×40칸이라 외부 길찾기 패키지 없이 직접 계산해도 가볍다.
    public class WallGrid : MonoBehaviour
    {
        private const float StraightCost = 1f;
        private const float DiagonalCost = 1.41421356f;

        [SerializeField] private Tilemap _walls;
        [SerializeField] private LayerMask _wallMask;
        [SerializeField, Tooltip("칸. 길을 곧게 펼 때 벽에 스치지 않는지 보는 몸 반지름")] private float _agentRadius = 0.4f;

        private readonly List<int> _open = new List<int>();
        private readonly List<Vector2> _cellPath = new List<Vector2>();
        private BoundsInt _bounds;
        private int _width;
        private int _height;
        private bool[] _blocked;
        private float[] _cost;
        private int[] _cameFrom;
        private bool[] _closed;

        private void Awake()
        {
            _walls.CompressBounds();
            _bounds = _walls.cellBounds;
            _width = _bounds.size.x;
            _height = _bounds.size.y;

            int count = _width * _height;
            _blocked = new bool[count];
            _cost = new float[count];
            _cameFrom = new int[count];
            _closed = new bool[count];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _blocked[y * _width + x] = _walls.HasTile(new Vector3Int(_bounds.xMin + x, _bounds.yMin + y, 0));
                }
            }
        }

        // from에서 to까지 지나갈 점들을 path에 채운다 (from은 넣지 않는다). 길이 없으면 false.
        public bool FindPath(Vector2 from, Vector2 to, List<Vector2> path)
        {
            path.Clear();

            int start = FreeCellNear(from);
            int goal = FreeCellNear(to);
            if (start < 0 || goal < 0)
            {
                return false;
            }

            if (!Search(start, goal))
            {
                return false;
            }

            // 도착 칸에서 거꾸로 따라가 칸 중심의 목록을 만든다.
            _cellPath.Clear();
            for (int cell = goal; cell != start; cell = _cameFrom[cell])
            {
                _cellPath.Add(CellCenter(cell));
            }

            _cellPath.Reverse();

            // 목적지가 벽 안이 아니면 마지막은 칸 중심이 아니라 정확한 목적지로 간다.
            if (CellAt(to) == goal)
            {
                if (_cellPath.Count > 0)
                {
                    _cellPath[_cellPath.Count - 1] = to;
                }
                else
                {
                    _cellPath.Add(to);
                }
            }

            Straighten(from, path);
            return true;
        }

        private bool Search(int start, int goal)
        {
            System.Array.Clear(_closed, 0, _closed.Length);
            for (int i = 0; i < _cost.Length; i++)
            {
                _cost[i] = float.MaxValue;
            }

            _open.Clear();
            _open.Add(start);
            _cost[start] = 0f;
            _cameFrom[start] = start;

            int goalX = goal % _width;
            int goalY = goal / _width;

            while (_open.Count > 0)
            {
                // 열린 칸이 많아야 수십 개라 정렬된 자료구조 없이 그냥 훑는다.
                int bestIndex = 0;
                float bestScore = float.MaxValue;
                for (int i = 0; i < _open.Count; i++)
                {
                    int candidate = _open[i];
                    float score = _cost[candidate] + Estimate(candidate % _width, candidate / _width, goalX, goalY);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                int current = _open[bestIndex];
                _open.RemoveAt(bestIndex);
                if (current == goal)
                {
                    return true;
                }

                _closed[current] = true;
                int cx = current % _width;
                int cy = current / _width;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (IsBlocked(nx, ny))
                        {
                            continue;
                        }

                        // 대각선은 양옆 칸이 모두 비어 있을 때만 간다. 아니면 벽 모서리를 뚫고 지나가게 된다.
                        bool diagonal = dx != 0 && dy != 0;
                        if (diagonal && (IsBlocked(cx + dx, cy) || IsBlocked(cx, cy + dy)))
                        {
                            continue;
                        }

                        int next = ny * _width + nx;
                        if (_closed[next])
                        {
                            continue;
                        }

                        float cost = _cost[current] + (diagonal ? DiagonalCost : StraightCost);
                        if (cost >= _cost[next])
                        {
                            continue;
                        }

                        if (_cost[next] == float.MaxValue)
                        {
                            _open.Add(next);
                        }

                        _cost[next] = cost;
                        _cameFrom[next] = current;
                    }
                }
            }

            return false;
        }

        private static float Estimate(int x, int y, int goalX, int goalY)
        {
            int dx = Mathf.Abs(x - goalX);
            int dy = Mathf.Abs(y - goalY);
            return StraightCost * (dx + dy) + (DiagonalCost - 2f * StraightCost) * Mathf.Min(dx, dy);
        }

        // 칸 중심만 따라가면 계단처럼 꺾여 걷는다. 벽에 스치지 않고 곧장 갈 수 있는 가장 먼 점으로 건너뛴다.
        private void Straighten(Vector2 from, List<Vector2> path)
        {
            Vector2 anchor = from;
            int index = 0;
            while (index < _cellPath.Count)
            {
                int farthest = _cellPath.Count - 1;
                while (farthest > index && !IsWalkClear(anchor, _cellPath[farthest]))
                {
                    farthest--;
                }

                path.Add(_cellPath[farthest]);
                anchor = _cellPath[farthest];
                index = farthest + 1;
            }
        }

        private bool IsWalkClear(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance < 0.001f)
            {
                return true;
            }

            return Physics2D.CircleCast(from, _agentRadius, delta / distance, distance, _wallMask).collider == null;
        }

        // 위치가 벽 칸이면(벽에 밀려 살짝 걸친 경우) 둘레의 빈칸을 쓴다. 없으면 -1.
        private int FreeCellNear(Vector2 position)
        {
            Vector3Int cell = _walls.WorldToCell(position);
            int x = cell.x - _bounds.xMin;
            int y = cell.y - _bounds.yMin;
            if (!IsBlocked(x, y))
            {
                return y * _width + x;
            }

            int best = -1;
            float bestDistance = float.MaxValue;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (IsBlocked(x + dx, y + dy))
                    {
                        continue;
                    }

                    int candidate = (y + dy) * _width + x + dx;
                    float distance = (CellCenter(candidate) - position).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = candidate;
                    }
                }
            }

            return best;
        }

        private int CellAt(Vector2 position)
        {
            Vector3Int cell = _walls.WorldToCell(position);
            int x = cell.x - _bounds.xMin;
            int y = cell.y - _bounds.yMin;
            return IsBlocked(x, y) ? -1 : y * _width + x;
        }

        private bool IsBlocked(int x, int y)
        {
            return x < 0 || y < 0 || x >= _width || y >= _height || _blocked[y * _width + x];
        }

        private Vector2 CellCenter(int cell)
        {
            return _walls.GetCellCenterWorld(new Vector3Int(_bounds.xMin + cell % _width, _bounds.yMin + cell / _width, 0));
        }
    }
}
