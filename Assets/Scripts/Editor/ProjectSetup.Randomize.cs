using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace.EditorTools
{
    // 판마다 로봇·컨테이너 위치를 섞는 오브젝트를 정거장 씬에 둔다 (기획서 19.2).
    public static partial class ProjectSetup
    {
        private static void EnsureRaidLayout()
        {
            GameObject layout = GameObject.Find("RaidLayout");
            if (layout == null)
            {
                layout = new GameObject("RaidLayout");
            }

            EnsureComponent(layout, out RaidLayoutRandomizer randomizer);
            SetReferenceIfEmpty(randomizer, "_walls", GameObject.Find("Grid").transform.Find("Walls").GetComponent<Tilemap>());
            GameObject containers = GameObject.Find("Containers");
            if (containers != null)
            {
                SetReferenceIfEmpty(randomizer, "_containers", containers.transform);
            }
        }
    }
}
