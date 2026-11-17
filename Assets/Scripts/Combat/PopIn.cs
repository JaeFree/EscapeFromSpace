using UnityEngine;

namespace EscapeFromSpace
{
    // 켜질 때 튀어나오듯 커졌다가 제 크기로 돌아오는 장식. 로봇의 "!" 표시처럼 눈에 띄어야 하는 것에 쓴다.
    public class PopIn : MonoBehaviour
    {
        [SerializeField, Tooltip("초. 튀어나오는 데 걸리는 시간")] private float _duration = 0.22f;
        [SerializeField, Tooltip("가장 클 때의 비율 (1.4 = 제 크기의 1.4배)")] private float _overshoot = 1.4f;

        private Vector3 _baseScale;
        private float _age;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            _age = 0f;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            if (_age >= _duration)
            {
                return;
            }

            _age += Time.deltaTime;
            float progress = Mathf.Clamp01(_age / _duration);

            // 앞 60%에서 가장 크게 부풀고, 나머지에서 제 크기로 줄어든다.
            float scale = progress < 0.6f
                ? Mathf.Lerp(0f, _overshoot, progress / 0.6f)
                : Mathf.Lerp(_overshoot, 1f, (progress - 0.6f) / 0.4f);
            transform.localScale = _baseScale * scale;
        }
    }
}
