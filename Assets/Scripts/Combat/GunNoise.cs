using System;
using UnityEngine;

namespace EscapeFromSpace
{
    // 플레이어의 총소리를 로봇에게 알린다 (기획서 5.1: 총소리를 들으면 그 위치로 추격).
    // 쏘는 쪽과 듣는 쪽이 서로를 몰라도 되게 중간에 둔다.
    public static class GunNoise
    {
        // 위치와 들리는 반경(칸). 소음기를 달면 반경이 줄어든다 (기획서 19.3).
        public static event Action<Vector2, float> Fired;

        public static void Emit(Vector2 position, float radius)
        {
            Fired?.Invoke(position, radius);
        }
    }

    // 발소리와 구르는 소리 (기획서 19.3). 총소리와 달리 들은 로봇은 추격하지 않고 의심만 한다.
    public static class Footsteps
    {
        public static event Action<Vector2, float> Made;

        public static void Emit(Vector2 position, float radius)
        {
            Made?.Invoke(position, radius);
        }
    }
}
