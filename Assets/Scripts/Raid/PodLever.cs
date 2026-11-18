using UnityEngine;

namespace EscapeFromSpace
{
    // 연구소 탈출 포드의 비상 레버 (2026-11-18 사용자 결정: 중요한 구역의 포드 1곳은 레버를 당겨야 열린다).
    // 가까이에서 E를 누르고 있으면 진행 바가 차고, 다 차면 그 판 동안 포드의 잠금이 풀린다.
    public class PodLever : MonoBehaviour
    {
        [SerializeField] private EscapePod _pod;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField, Tooltip("레버 위에 띄우는 E 글자와 진행 바")] private GameObject _prompt;
        [SerializeField] private Transform _progressFill;
        [SerializeField, Tooltip("칸")] private float _range = 1.4f;
        [SerializeField, Tooltip("초")] private float _holdTime = 1f;
        [SerializeField] private Color _lockedColor = new Color(1f, 0.35f, 0.25f);
        [SerializeField] private Color _pulledColor = new Color(0.3f, 1f, 0.5f);

        private float _held;

        public bool IsPulled { get; private set; }

        private void Start()
        {
            Refresh();
        }

        private void Update()
        {
            if (IsPulled)
            {
                return;
            }

            bool near = !_controller.IsDead && !_controller.IsBagOpen
                && Vector2.Distance(_controller.transform.position, transform.position) <= _range;
            _prompt.SetActive(near);
            if (near && _input.InteractHeld)
            {
                _held += Time.deltaTime;
                if (_held >= _holdTime)
                {
                    Pull();
                }
            }
            else
            {
                _held = 0f;
            }

            if (_progressFill != null)
            {
                _progressFill.localScale = new Vector3(Mathf.Clamp01(_held / _holdTime), 1f, 1f);
            }
        }

        private void Pull()
        {
            IsPulled = true;
            _pod.Unlock();
            _prompt.SetActive(false);
            Sfx.PlayAt(SfxId.PodOpen, transform.position);
            Refresh();
        }

        private void Refresh()
        {
            if (_renderer != null)
            {
                _renderer.color = IsPulled ? _pulledColor : _lockedColor;
                _renderer.flipY = IsPulled;
            }
        }
    }
}
