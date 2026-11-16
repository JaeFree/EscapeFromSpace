using UnityEngine;

namespace EscapeFromSpace
{
    // 총구에서 탄이 닿은 곳까지 잠깐 보이는 궤적 선. 판정은 히트스캔이라 보여 주기만 한다.
    [RequireComponent(typeof(LineRenderer))]
    public class BulletTracer : MonoBehaviour
    {
        [SerializeField, Tooltip("초. 이 시간 동안 흐려지다 사라진다")] private float _lifetime = 0.12f;

        private LineRenderer _line;
        private Color _startColor;
        private Color _endColor;
        private float _age;

        public void Show(Vector2 from, Vector2 to)
        {
            _line = GetComponent<LineRenderer>();
            _startColor = _line.startColor;
            _endColor = _line.endColor;
            _line.positionCount = 2;
            _line.SetPosition(0, from);
            _line.SetPosition(1, to);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float fade = 1f - _age / _lifetime;
            _line.startColor = new Color(_startColor.r, _startColor.g, _startColor.b, _startColor.a * fade);
            _line.endColor = new Color(_endColor.r, _endColor.g, _endColor.b, _endColor.a * fade);
        }
    }
}
