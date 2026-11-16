using UnityEngine;

namespace EscapeFromSpace
{
    // 근접 무기를 휘두른 범위를 부채꼴로 잠깐 보여 준다. 판정은 휘두른 순간에 끝나므로 보여 주기만 한다.
    // 사격 궤적 선처럼 휘두르기 시작한 쪽부터 흐려져서 휘두른 방향이 보인다.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SwingArc : MonoBehaviour
    {
        private const int SliceCount = 24;

        [SerializeField] private Color _color = new Color(0.55f, 0.9f, 1f, 0.6f);
        [SerializeField, Tooltip("초. 이 시간 동안 휘두르기 시작한 쪽부터 흐려지다 사라진다")] private float _lifetime = 0.12f;
        [SerializeField, Range(0.05f, 1f), Tooltip("흐려지는 구간의 폭 (부채꼴 전체 각 대비)")] private float _fadeLength = 0.4f;

        private readonly Color[] _colors = new Color[(SliceCount + 1) * 2];
        private Mesh _mesh;
        private float _age;

        // 오브젝트의 +X가 조준 방향이다. 왼쪽 끝에서 오른쪽 끝으로 휘두른다.
        public void Show(float radius, float angleDegrees)
        {
            var vertices = new Vector3[(SliceCount + 1) * 2];
            var triangles = new int[SliceCount * 6];
            float half = angleDegrees * 0.5f;

            for (int i = 0; i <= SliceCount; i++)
            {
                // 각도마다 색이 달라야 하므로 중심점도 조각마다 따로 둔다.
                float angle = Mathf.Lerp(half, -half, i / (float)SliceCount) * Mathf.Deg2Rad;
                vertices[i * 2] = Vector3.zero;
                vertices[i * 2 + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int i = 0; i < SliceCount; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 3;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }

            _mesh = new Mesh { vertices = vertices, triangles = triangles };
            GetComponent<MeshFilter>().sharedMesh = _mesh;
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

        private void OnDestroy()
        {
            // 코드로 만든 메시는 오브젝트와 함께 지워지지 않는다.
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        // progress 0~1. 투명한 구간이 휘두르기 시작한 쪽에서 끝난 쪽으로 밀려 간다.
        private void ApplyFade(float progress)
        {
            // 흐려지는 구간이 끝에 닿은 뒤에는 끝도 같이 옅어져야 마지막에 뚝 끊기지 않는다.
            float maxAlpha = _color.a * Mathf.Clamp01((1f - progress) / _fadeLength);

            for (int i = 0; i <= SliceCount; i++)
            {
                float position = i / (float)SliceCount;
                float alpha = Mathf.Min(maxAlpha, _color.a * Mathf.Clamp01((position - progress) / _fadeLength));
                var color = new Color(_color.r, _color.g, _color.b, alpha);
                _colors[i * 2] = color;
                _colors[i * 2 + 1] = color;
            }

            _mesh.colors = _colors;
        }
    }
}
