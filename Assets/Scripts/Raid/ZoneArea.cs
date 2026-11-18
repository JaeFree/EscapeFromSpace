using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 정거장 구역 하나의 범위 (기획서 15.1). 네임드 로봇의 탐색 범위, 방문 퀘스트, 전체 맵의 이름이 이것을 본다.
    public class ZoneArea : MonoBehaviour
    {
        private static readonly List<ZoneArea> All = new List<ZoneArea>();

        [SerializeField, Tooltip("퀘스트·저장에서 쓰는 이름. 정한 뒤에는 바꾸지 않는다")] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, Tooltip("칸. 왼쪽 아래 모서리와 크기 (월드 좌표)")] private Rect _area;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Rect Area => _area;

        public static IReadOnlyList<ZoneArea> Zones => All;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            All.Clear();
        }

        private void OnEnable()
        {
            All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public bool Contains(Vector2 position)
        {
            return _area.Contains(position);
        }

        // 그 점이 든 구역. 복도처럼 어느 구역에도 들지 않으면 null.
        public static ZoneArea At(Vector2 position)
        {
            foreach (ZoneArea zone in All)
            {
                if (zone.Contains(position))
                {
                    return zone;
                }
            }

            return null;
        }

        public static ZoneArea Find(string id)
        {
            foreach (ZoneArea zone in All)
            {
                if (zone.Id == id)
                {
                    return zone;
                }
            }

            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(_area.center, _area.size);
        }
    }
}
