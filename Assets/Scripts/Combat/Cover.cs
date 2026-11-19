using UnityEngine;

namespace EscapeFromSpace
{
    // 엄폐 판정 (기획서 19.3). 적이 플레이어를 볼 수 있다면 둘 사이의 직선은 이미 비어 있으므로,
    // 직선 양옆으로 비스듬히 한 칸 안에 벽·엄폐물이 있는지 본다. 모서리나 상자 뒤에서 몸을 내민 상태다.
    public static class Cover
    {
        private static readonly float[] SideAngles = { -40f, 40f, -70f, 70f };

        // 엄폐 중이면 true와 함께 막아 주는 칸의 위치를 준다 (하얗게 빛낼 자리).
        public static bool Check(Vector2 player, Vector2 threat, LayerMask wallMask, float distance, out Vector2 coverPoint)
        {
            coverPoint = default;
            Vector2 toThreat = threat - player;
            if (toThreat.sqrMagnitude < 0.01f)
            {
                return false;
            }

            toThreat.Normalize();
            foreach (float angle in SideAngles)
            {
                Vector2 probe = player + (Vector2)(Quaternion.Euler(0f, 0f, angle) * toThreat) * distance;
                Collider2D wall = Physics2D.OverlapPoint(probe, wallMask);
                if (wall != null)
                {
                    coverPoint = new Vector2(Mathf.Floor(probe.x) + 0.5f, Mathf.Floor(probe.y) + 0.5f);
                    return true;
                }
            }

            return false;
        }

        public static bool IsInCover(Vector2 player, Vector2 threat, LayerMask wallMask, float distance)
        {
            return Check(player, threat, wallMask, distance, out _);
        }
    }
}
