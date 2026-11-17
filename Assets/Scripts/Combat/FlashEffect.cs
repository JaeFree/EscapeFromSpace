using UnityEngine;

namespace EscapeFromSpace
{
    // 총구 불빛, 명중 섬광, 폭발의 빛과 충격파처럼 잠깐 커지면서 사라지는 그림.
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlashEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("초")] private float _lifetime = 0.1f;
        [SerializeField, Tooltip("칸. 처음 크기 (그림의 긴 변)")] private float _startSize = 0.3f;
        [SerializeField, Tooltip("칸. 사라질 때의 크기")] private float _endSize = 0.6f;
        [SerializeField, Tooltip("켜면 놓일 때마다 무작위로 돌아가서 같은 모양이 되풀이되지 않는다")] private bool _randomRotation;

        private SpriteRenderer _renderer;
        private float _fullAlpha;
        private float _unitScale = 1f;
        private float _age;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _fullAlpha = _renderer.color.a;

            if (_renderer.sprite != null)
            {
                Vector2 size = _renderer.sprite.bounds.size;
                _unitScale = 1f / Mathf.Max(size.x, size.y);
            }

            if (_randomRotation)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }

            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _lifetime)
            {
                // 다른 효과(불꽃, 폭발)의 자식이면 끄기만 한다. 지우는 것은 그 효과의 뿌리가 한다.
                if (transform.parent == null)
                {
                    Destroy(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            Apply(_age / _lifetime);
        }

        // 빨리 커지고 천천히 사라져야 번쩍하는 느낌이 난다.
        private void Apply(float progress)
        {
            float grow = 1f - (1f - progress) * (1f - progress);
            float scale = Mathf.Lerp(_startSize, _endSize, grow) * _unitScale;

            // 부모의 크기에 상관없이 정한 크기로 보이게 한다.
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(
                scale / Mathf.Max(0.0001f, parentScale.x), scale / Mathf.Max(0.0001f, parentScale.y), 1f);

            Color color = _renderer.color;
            color.a = _fullAlpha * (1f - progress);
            _renderer.color = color;
        }
    }
}
