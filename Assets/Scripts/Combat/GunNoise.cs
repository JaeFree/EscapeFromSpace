using System;
using UnityEngine;

namespace EscapeFromSpace
{
    // 플레이어의 총소리를 로봇에게 알린다 (기획서 5.1: 총소리를 들으면 그 위치로 추격).
    // 쏘는 쪽과 듣는 쪽이 서로를 몰라도 되게 중간에 둔다.
    public static class GunNoise
    {
        public static event Action<Vector2> Fired;

        public static void Emit(Vector2 position)
        {
            Fired?.Invoke(position);
        }
    }
}
