using UnityEngine;

namespace EscapeFromSpace
{
    // 명중점 표시처럼 잠깐 보였다 사라지는 효과용.
    public class TimedDestroy : MonoBehaviour
    {
        [SerializeField, Tooltip("초")] private float _lifetime = 0.1f;

        private void Start()
        {
            Destroy(gameObject, _lifetime);
        }
    }
}
