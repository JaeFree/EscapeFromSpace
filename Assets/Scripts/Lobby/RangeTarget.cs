using UnityEngine;

namespace EscapeFromSpace
{
    // 사격장의 표적 로봇. 위아래로 오가기만 하고 공격하지 않는다. 부서지면 잠시 뒤 다시 생긴다.
    // HP와 색은 레이드의 로봇과 같은 RobotData에서 읽어서, 실제 로봇을 잡는 데 드는 발 수를 미리 볼 수 있다.
    [RequireComponent(typeof(Rigidbody2D))]
    public class RangeTarget : MonoBehaviour
    {
        [SerializeField] private RobotData _data;
        [SerializeField] private Health _health;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Collider2D _collider;
        [SerializeField, Tooltip("칸. 놓인 자리에서 위로 이만큼 갔다가 돌아온다. 음수면 아래로")] private float _travel = 3f;
        [SerializeField, Tooltip("칸/초")] private float _speed = 2f;
        [SerializeField, Tooltip("초. 부서지고 다시 생길 때까지")] private float _respawnDelay = 2f;

        [Header("머리 위 HP 바")]
        [SerializeField, Tooltip("칸. 가로, 세로")] private Vector2 _barSize = new Vector2(0.9f, 0.12f);
        [SerializeField, Tooltip("칸. 표적 중심에서 위로")] private float _barHeight = 0.65f;
        [SerializeField] private Color _barColor = new Color(0.9f, 0.15f, 0.15f);
        [SerializeField, Tooltip("바를 그리는 사각형 그림")] private Sprite _barSprite;

        private Rigidbody2D _rigidbody;
        private Vector2 _origin;
        private float _distance;
        private float _respawnTimeLeft;
        private Transform _bar;
        private Transform _barFill;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _origin = transform.position;
            // 표적은 사로 왼쪽의 사수를 보고 있다.
            _data.ApplyLook(_body, 180f);
            _health.Initialize(_data.MaxHp);
            BuildBar();
        }

        // HP 바는 사각형 그림 두 장으로 실행 중에 만든다: 어두운 바탕과 그 위의 빨간 바.
        private void BuildBar()
        {
            _bar = CreateBarPart("HpBar", transform, new Color(0.05f, 0.05f, 0.07f, 0.9f), 12);
            _bar.localPosition = new Vector3(0f, _barHeight, 0f);
            _bar.localScale = new Vector3(_barSize.x + 0.06f, _barSize.y + 0.06f, 1f);

            _barFill = CreateBarPart("Fill", transform, _barColor, 13);
            UpdateBar();
        }

        private Transform CreateBarPart(string partName, Transform parent, Color color, int sortingOrder)
        {
            var part = new GameObject(partName).transform;
            part.SetParent(parent, false);
            var renderer = part.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _barSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return part;
        }

        // 남은 HP만큼 왼쪽에서부터 채운다.
        private void UpdateBar()
        {
            float ratio = _health.MaxHp > 0f ? Mathf.Clamp01(_health.CurrentHp / _health.MaxHp) : 0f;
            float width = _barSize.x * ratio;
            _barFill.localScale = new Vector3(width, _barSize.y, 1f);
            _barFill.localPosition = new Vector3((width - _barSize.x) * 0.5f, _barHeight, 0f);
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void Update()
        {
            UpdateBar();
            if (!_health.IsDead)
            {
                return;
            }

            _respawnTimeLeft -= Time.deltaTime;
            if (_respawnTimeLeft <= 0f)
            {
                _health.Initialize(_data.MaxHp);
                SetVisible(true);
            }
        }

        // 충돌체가 함께 움직여야 탄이 맞으므로 물리 틱에서 옮긴다. 오간 거리로 재서 양 끝에서 방향을 바꾼다.
        private void FixedUpdate()
        {
            float length = Mathf.Abs(_travel);
            if (length <= 0f)
            {
                return;
            }

            _distance += _speed * Time.fixedDeltaTime;
            float offset = Mathf.PingPong(_distance, length) * Mathf.Sign(_travel);
            _rigidbody.MovePosition(_origin + Vector2.up * offset);
        }

        private void OnDied()
        {
            _respawnTimeLeft = _respawnDelay;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            _body.enabled = visible;
            _collider.enabled = visible;
            _bar.gameObject.SetActive(visible);
            _barFill.gameObject.SetActive(visible);
        }
    }
}
