using UnityEngine;

namespace EscapeFromSpace
{
    // 총구에서 탄이 닿은 곳까지 잠깐 보이는 궤적 선. 판정은 히트스캔이라 보여 주기만 한다.
    // 총구 쪽부터 흐려져서 탄이 날아간 방향이 보인다.
    [RequireComponent(typeof(LineRenderer))]
    public class BulletTracer : MonoBehaviour
    {
        [SerializeField, Tooltip("초. 이 시간 동안 총구 쪽부터 흐려지다 사라진다")] private float _lifetime = 0.12f;
        [SerializeField, Range(0.05f, 1f), Tooltip("흐려지는 구간의 길이 (선 전체 길이 대비)")] private float _fadeLength = 0.4f;

        private const int PointCount = 21;

        private readonly GradientAlphaKey[] _alphaKeys = new GradientAlphaKey[2];
        private LineRenderer _line;
        private Gradient _gradient;
        private GradientColorKey[] _colorKeys;
        private float _fullAlpha;
        private float _age;

        public void Show(Vector2 from, Vector2 to)
        {
            _line = GetComponent<LineRenderer>();

            // 색은 프리팹에서 정한 끝 색(맞은 쪽)을 쓴다.
            Color color = _line.endColor;
            _fullAlpha = color.a;
            _colorKeys = new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) };
            _gradient = new Gradient();

            // 선은 점이 있는 곳에서만 색을 읽는다. 양 끝 두 점뿐이면 중간부터 흐려지는 모양을 못 그리므로 잘게 나눈다.
            _line.positionCount = PointCount;
            for (int i = 0; i < PointCount; i++)
            {
                _line.SetPosition(i, Vector2.Lerp(from, to, i / (PointCount - 1f)));
            }

            ApplyFade(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            ApplyFade(_age / _lifetime);
        }

        // progress 0~1. 투명한 구간이 총구에서 맞은 쪽으로 밀려 간다.
        private void ApplyFade(float progress)
        {
            float head = Mathf.Min(1f, progress + _fadeLength);

            // 흐려지는 구간이 끝에 닿은 뒤에는 끝도 같이 옅어져야 마지막에 뚝 끊기지 않는다.
            float headAlpha = _fullAlpha * Mathf.Clamp01((1f - progress) / _fadeLength);

            _alphaKeys[0] = new GradientAlphaKey(0f, progress);
            _alphaKeys[1] = new GradientAlphaKey(headAlpha, head);
            _gradient.SetKeys(_colorKeys, _alphaKeys);
            _line.colorGradient = _gradient;
        }
    }
}
