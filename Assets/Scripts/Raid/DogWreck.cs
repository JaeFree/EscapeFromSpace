using UnityEngine;

namespace EscapeFromSpace
{
    // 채굴선의 부서진 로봇 개 (기획서 19.5). 이미 로봇 개가 있으면 판을 시작할 때 사라진다.
    public class DogWreck : MonoBehaviour
    {
        private void Start()
        {
            if (GameSession.Data != null && GameSession.Data.HasDog)
            {
                Destroy(gameObject);
            }
        }
    }
}
