using UnityEngine;

namespace EscapeFromSpace
{
    // 강한 공격 전에 보여 주는 예고 표시 (기획서 5장). 저격은 조준선, 샷건은 쏠 범위의 부채꼴.
    public class AttackTelegraph : MonoBehaviour
    {
        private const int FanSliceCount = 12;

        [SerializeField] private LineRenderer _line;
        [SerializeField, Tooltip("조준선이 닿는 곳에 찍히는 점. 비우면 없다")] private SpriteRenderer _endDot;
        [SerializeField] private MeshFilter _fanFilter;
        [SerializeField] private MeshRenderer _fanRenderer;
        [SerializeField] private Color _color = new Color(1f, 0.2f, 0.2f, 1f);

        [Header("조준선")]
        [SerializeField, Tooltip("따라오는 동안의 굵기와 진하기")] private float _trackingWidth = 0.03f;
        [SerializeField, Range(0f, 1f)] private float _trackingAlpha = 0.45f;
        [SerializeField, Tooltip("방향이 고정된 뒤의 굵기")] private float _lockedWidth = 0.07f;
        [SerializeField, Tooltip("닿는 곳의 점 크기 (따라오는 동안)")] private float _trackingDotSize = 0.3f;
        [SerializeField, Tooltip("닿는 곳의 점 크기 (고정된 뒤)")] private float _lockedDotSize = 0.55f;

        [Header("충전 범위")]
        [SerializeField, Range(0f, 1f)] private float _fanStartAlpha = 0.08f;
        [SerializeField, Range(0f, 1f)] private float _fanEndAlpha = 0.45f;

        // 네임드 패턴의 예고 선이 같은 재질을 쓴다 (빌드에서 셰이더를 찾지 않아도 되게).
        public Material LineMaterial => _line.sharedMaterial;

        private Mesh _fanMesh;
        private Color[] _fanColors;
        private float _fanAngle;
        private float _fanRadius;

        private void Awake()
        {
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            Hide();
        }

        private void OnDestroy()
        {
            if (_fanMesh != null)
            {
                Destroy(_fanMesh);
            }
        }

        // 방향이 고정되면 굵고 진해진다. 이때부터는 피해도 따라오지 않는다는 신호다.
        public void ShowLine(Vector2 from, Vector2 to, bool locked)
        {
            _fanRenderer.enabled = false;
            _line.enabled = true;
            _line.SetPosition(0, from);
            _line.SetPosition(1, to);

            float width = locked ? _lockedWidth : _trackingWidth;
            var color = new Color(_color.r, _color.g, _color.b, locked ? 1f : _trackingAlpha);
            _line.startWidth = width;
            _line.endWidth = width;
            _line.startColor = color;
            _line.endColor = color;

            if (_endDot != null)
            {
                float size = locked ? _lockedDotSize : _trackingDotSize;
                _endDot.enabled = true;
                _endDot.color = color;
                _endDot.transform.position = to;
                _endDot.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        // progress 0~1. 충전이 찰수록 진해진다.
        public void ShowFan(Vector2 direction, float angleDegrees, float radius, float progress)
        {
            _line.enabled = false;
            HideDot();

            if (_fanMesh == null || !Mathf.Approximately(angleDegrees, _fanAngle) || !Mathf.Approximately(radius, _fanRadius))
            {
                BuildFan(angleDegrees, radius);
            }

            _fanFilter.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            var color = new Color(_color.r, _color.g, _color.b, Mathf.Lerp(_fanStartAlpha, _fanEndAlpha, progress));
            for (int i = 0; i < _fanColors.Length; i++)
            {
                _fanColors[i] = color;
            }

            _fanMesh.colors = _fanColors;
            _fanRenderer.enabled = true;
        }

        public void Hide()
        {
            _line.enabled = false;
            _fanRenderer.enabled = false;
            HideDot();
        }

        private void HideDot()
        {
            if (_endDot != null)
            {
                _endDot.enabled = false;
            }
        }

        // 부채꼴 오브젝트의 +X가 쏘는 방향이다.
        private void BuildFan(float angleDegrees, float radius)
        {
            if (_fanMesh != null)
            {
                Destroy(_fanMesh);
            }

            _fanAngle = angleDegrees;
            _fanRadius = radius;

            var vertices = new Vector3[FanSliceCount + 2];
            var triangles = new int[FanSliceCount * 3];
            float half = angleDegrees * 0.5f;

            vertices[0] = Vector3.zero;
            for (int i = 0; i <= FanSliceCount; i++)
            {
                float angle = Mathf.Lerp(half, -half, i / (float)FanSliceCount) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int i = 0; i < FanSliceCount; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            _fanMesh = new Mesh { vertices = vertices, triangles = triangles };
            _fanColors = new Color[vertices.Length];
            _fanFilter.sharedMesh = _fanMesh;
        }
    }
}
