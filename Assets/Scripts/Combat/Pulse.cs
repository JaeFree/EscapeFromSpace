using UnityEngine;

namespace EscapeFromSpace
{
    // 포탈이나 열린 탈출 포드의 빛처럼 계속 돌거나 숨 쉬듯 커졌다 작아지는 장식. 판정과는 상관없다.
    public class Pulse : MonoBehaviour
    {
        [SerializeField, Tooltip("도/초. 0이면 돌지 않는다")] private float _spinSpeed;
        [SerializeField, Tooltip("커졌다 작아지는 비율 (0.05 = 5%)")] private float _scaleAmount = 0.05f;
        [SerializeField, Tooltip("초. 한 번 커졌다 작아지는 데 걸리는 시간")] private float _period = 1.6f;

        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            if (_spinSpeed != 0f)
            {
                transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
            }

            if (_period > 0f)
            {
                float wave = Mathf.Sin(Time.time * 2f * Mathf.PI / _period);
                transform.localScale = _baseScale * (1f + wave * _scaleAmount);
            }
        }
    }
}
