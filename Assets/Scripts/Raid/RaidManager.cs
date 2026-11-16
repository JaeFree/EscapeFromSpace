using UnityEngine;

namespace EscapeFromSpace
{
    public enum RaidOutcome
    {
        InProgress,
        Escaped,
        Died,
    }

    // 한 판의 진행: 시간, 탈출 포드, 끝나는 조건 (기획서 3장, 7장).
    public class RaidManager : MonoBehaviour
    {
        [SerializeField] private RaidConfig _config;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private EscapePod[] _pods;

        private EscapePod _activePod;
        private float _countdownLeft;

        public RaidOutcome Outcome { get; private set; }

        // 레이드를 시작한 뒤 흐른 시간(초)
        public float ElapsedTime { get; private set; }

        // 플레이어가 서 있는 열린 포드. 카운트 중이 아니면 null.
        public EscapePod CountingPod { get; private set; }
        public float CountdownLeft => _countdownLeft;

        // 결과 화면에 쓰는 값. 끝나는 순간에 계산해 둔다.
        public int CarriedValue { get; private set; }
        public ItemStack SecuredItem { get; private set; }

        private void Start()
        {
            // 2곳 중 1곳만 작동한다.
            _activePod = _pods[Random.Range(0, _pods.Length)];
            _countdownLeft = _config.EscapeCountdown;
            _playerHealth.Died += OnPlayerDied;
        }

        private void OnDestroy()
        {
            _playerHealth.Died -= OnPlayerDied;
        }

        private void Update()
        {
            if (Outcome != RaidOutcome.InProgress)
            {
                return;
            }

            ElapsedTime += Time.deltaTime;

            bool open = _config.PodsAlwaysOpen || ElapsedTime >= _config.PodOpenDelay;
            if (open != _activePod.IsOpen)
            {
                _activePod.SetOpen(open);
            }

            // 포드 안에서 버티면 탈출한다. 밖으로 나가면 처음부터 다시 센다. 맞아도 계속 센다.
            if (_activePod.IsOpen && _activePod.Contains(_playerHealth.transform.position))
            {
                CountingPod = _activePod;
                _countdownLeft -= Time.deltaTime;
                if (_countdownLeft <= 0f)
                {
                    Finish(RaidOutcome.Escaped);
                }
            }
            else
            {
                CountingPod = null;
                _countdownLeft = _config.EscapeCountdown;
            }
        }

        private void OnPlayerDied()
        {
            if (Outcome == RaidOutcome.InProgress)
            {
                Finish(RaidOutcome.Died);
            }
        }

        // 탈출하면 가진 것 전부가 가지고 나온 가치, 죽으면 보관함을 뺀 전부가 잃은 가치다.
        private void Finish(RaidOutcome outcome)
        {
            Outcome = outcome;
            CountingPod = null;
            CarriedValue = _inventory.TotalValue(outcome == RaidOutcome.Escaped);
            SecuredItem = _inventory.SecureBox.Items.Count > 0 ? _inventory.SecureBox.Items[0] : null;
        }
    }
}
