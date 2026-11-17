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
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private EscapePod[] _pods;
        [SerializeField] private ItemDatabase _items;
        [SerializeField] private MapExplorer _map;

        private EscapePod _activePod;
        private float _holdTime;

        // 사격이나 구르기로 취소한 뒤에는 탈출 키를 뗐다가 다시 눌러야 한다.
        private bool _needsRelease;

        public RaidOutcome Outcome { get; private set; }

        // 레이드를 시작한 뒤 흐른 시간(초)
        public float ElapsedTime { get; private set; }

        // 플레이어가 서 있는 열린 포드. 여기서만 탈출 키가 듣는다. 없으면 null.
        public EscapePod ReachablePod { get; private set; }

        // 탈출 키를 누르고 있는 진행도 0~1
        public float EscapeProgress => _config.EscapeHoldTime > 0f ? Mathf.Clamp01(_holdTime / _config.EscapeHoldTime) : 0f;

        // 결과 화면에 쓰는 값. 끝나는 순간에 계산해 둔다.
        public int CarriedValue { get; private set; }

        // 이번 판에 얻은 경험치 (로봇 처치 + 탈출 보너스)
        public int XpGained { get; private set; }
        public ItemStack SecuredItem { get; private set; }

        // 대기실에서 들어왔으면 거기서 고른 장비로 시작한다. 씬을 바로 Play했으면 가방의 테스트용 장비를 쓴다.
        private void Awake()
        {
            if (GameSession.RaidFromLobby)
            {
                _inventory.Restore(GameSession.Data, _items);
            }
        }

        private void Start()
        {
            // 2곳 중 1곳만 작동한다.
            _activePod = _pods[Random.Range(0, _pods.Length)];
            _playerHealth.Died += OnPlayerDied;
            _playerHealth.Damaged += OnPlayerDamaged;
        }

        private void OnDestroy()
        {
            _playerHealth.Died -= OnPlayerDied;
            _playerHealth.Damaged -= OnPlayerDamaged;
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
                if (open)
                {
                    // 어느 포드인지 소리로 알 수 없게 위치 없이 낸다.
                    Sfx.Play(SfxId.PodOpen);
                }
            }

            UpdateEscape();
        }

        // 열린 포드에서 탈출 키를 누르고 있어 바가 다 차면 탈출한다 (2026-11-16 사용자 결정).
        // 키를 떼거나 포드 밖으로 나가거나 맞으면 처음부터, 사격하거나 구르면 취소된다. 누르는 동안에는 걷지 못한다.
        private void UpdateEscape()
        {
            bool inPod = _activePod.IsOpen && _activePod.Contains(_playerHealth.transform.position);
            ReachablePod = inPod ? _activePod : null;

            bool keyHeld = _input.EscapeHeld;
            if (!keyHeld)
            {
                _needsRelease = false;
            }

            bool interrupting = (_input.FireHeld && !_controller.IsBagOpen) || _roll.IsRolling;
            if (inPod && keyHeld && interrupting)
            {
                _needsRelease = true;
            }

            bool holding = inPod && keyHeld && !_needsRelease;
            _controller.IsMovementLocked = holding;
            if (!holding)
            {
                _holdTime = 0f;
                return;
            }

            _holdTime += Time.deltaTime;
            if (_holdTime >= _config.EscapeHoldTime)
            {
                Finish(RaidOutcome.Escaped);
            }
        }

        private void OnPlayerDamaged(float damage, Vector2 hitPoint)
        {
            _holdTime = 0f;
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
            Sfx.Play(outcome == RaidOutcome.Escaped ? SfxId.EscapeSuccess : SfxId.PlayerDeath);
            ReachablePod = null;
            _holdTime = 0f;
            _controller.IsMovementLocked = false;
            CarriedValue = _inventory.TotalValue(outcome == RaidOutcome.Escaped);
            SecuredItem = _inventory.SecureBox.Items.Count > 0 ? _inventory.SecureBox.Items[0] : null;

            // 탈출하면 경험치를 더 받는다. 죽어도 로봇을 부숴 얻은 경험치는 남는다.
            PlayerProgress progress = PlayerProgress.Current;
            if (progress != null)
            {
                if (outcome == RaidOutcome.Escaped)
                {
                    progress.AddXp(progress.Tree.EscapeBonusXp);
                }

                XpGained = progress.XpGained;
            }

            // 대기실에서 들어온 판만 저장한다. 죽었으면 보관함만 남는다.
            if (GameSession.RaidFromLobby)
            {
                _inventory.Capture(GameSession.Data);
                if (_map != null)
                {
                    _map.SaveTo(GameSession.Data);
                }

                GameSession.EndRaid(outcome == RaidOutcome.Escaped);
            }
        }
    }
}
