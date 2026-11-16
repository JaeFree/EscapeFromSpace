using UnityEngine;

namespace EscapeFromSpace
{
    // 탈출 포드 한 곳 (기획서 7장). 어느 포드가 작동하고 언제 열리는지는 RaidManager가 정한다.
    public class EscapePod : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField, Tooltip("칸. 이 크기의 구역 안에 있어야 카운트가 간다")] private Vector2 _zoneSize = new Vector2(2f, 2f);
        [SerializeField] private Color _closedColor = new Color(0.22f, 0.23f, 0.26f);
        [SerializeField] private Color _openColor = new Color(0.25f, 0.85f, 0.45f);

        public bool IsOpen { get; private set; }

        // 꺼진 포드와 아직 안 열린 포드는 똑같이 보인다. 열리기 전에는 어느 쪽이 작동하는지 알 수 없다.
        public void SetOpen(bool open)
        {
            IsOpen = open;
            _renderer.color = open ? _openColor : _closedColor;
        }

        public bool Contains(Vector2 position)
        {
            Vector2 offset = position - (Vector2)transform.position;
            return Mathf.Abs(offset.x) <= _zoneSize.x * 0.5f && Mathf.Abs(offset.y) <= _zoneSize.y * 0.5f;
        }

        private void Awake()
        {
            SetOpen(false);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _openColor;
            Gizmos.DrawWireCube(transform.position, _zoneSize);
        }
    }
}
