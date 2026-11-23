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

        private float _holdTime;

        // 사격이나 구르기로 취소한 뒤에는 탈출 키를 뗐다가 다시 눌러야 한다.
        private bool _needsRelease;

        public RaidOutcome Outcome { get; private set; }

        // 레이드를 시작한 뒤 흐른 시간(초)
        public float ElapsedTime { get; private set; }

        // 플레이어가 서 있는 열린 포드. 여기서만 탈출 키가 듣는다. 없으면 null.
        public EscapePod ReachablePod { get; private set; }

        // 플레이어가 가까이 있는 고장 난 비콘. 화면이 그 위에 '고장'을 띄운다. 없으면 null.
        public EscapePod NearBrokenPod { get; private set; }

        // 탈출 키를 누르고 있는 진행도 0~1
        public float EscapeProgress => _config.EscapeHoldTime > 0f ? Mathf.Clamp01(_holdTime / _config.EscapeHoldTime) : 0f;

        // 결과 화면에 쓰는 값. 끝나는 순간에 계산해 둔다.
        public int CarriedValue { get; private set; }

        // 이번 판에 얻은 경험치 (로봇 처치 + 탈출 보너스)
        public int XpGained { get; private set; }
        public ItemStack SecuredItem { get; private set; }

        // 이번 판에 데이터 칩으로 새로 등록한 설계도 (결과 화면에 보여 준다)
        public readonly System.Collections.Generic.List<string> NewBlueprints = new System.Collections.Generic.List<string>();

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
            // 2026-11-18 사용자 결정: 포드는 모두 작동한다. 레버가 필요한 포드는 레버를 당긴 뒤에 열린다.
            _playerHealth.Died += OnPlayerDied;
            _playerHealth.Damaged += OnPlayerDamaged;
            BreakPods();
        }

        // 판마다 비콘 몇 곳을 고장 낸다 (기획서 20.4). 탈출 경로를 그때그때 판단하게 하려는 것이다.
        // 레버가 필요 없는 비콘이 모두 고장 나면 시작 장비로는 나갈 길이 막히므로 하나는 남긴다.
        private void BreakPods()
        {
            if (_config == null || _pods == null)
            {
                return;
            }

            var candidates = new System.Collections.Generic.List<EscapePod>(_pods);
            int broken = 0;
            while (broken < _config.BrokenPodCount && candidates.Count > 1)
            {
                EscapePod pod = candidates[Random.Range(0, candidates.Count)];
                candidates.Remove(pod);

                bool leverFreeLeft = false;
                foreach (EscapePod other in candidates)
                {
                    leverFreeLeft |= !other.RequiresLever;
                }

                if (!pod.RequiresLever && !leverFreeLeft)
                {
                    continue;
                }

                pod.IsBroken = true;
                broken++;
                Debug.Log($"[RaidManager] 고장 난 비콘: {pod.name}");
            }
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

            bool timeOpen = _config.PodsAlwaysOpen || ElapsedTime >= _config.PodOpenDelay;
            bool opened = false;
            foreach (EscapePod pod in _pods)
            {
                bool open = timeOpen && pod.IsUnlocked && !pod.IsBroken;
                if (open != pod.IsOpen)
                {
                    pod.SetOpen(open);
                    opened |= open;
                }
            }

            if (opened)
            {
                // 어느 포드인지 소리로 알 수 없게 위치 없이 낸다.
                Sfx.Play(SfxId.PodOpen);
            }

            UpdateEscape();
        }

        // 열린 포드에서 탈출 키를 누르고 있어 바가 다 차면 탈출한다 (2026-11-16 사용자 결정).
        // 키를 떼거나 포드 밖으로 나가거나 맞으면 처음부터, 사격하거나 구르면 취소된다. 누르는 동안에는 걷지 못한다.
        private void UpdateEscape()
        {
            ReachablePod = null;
            NearBrokenPod = null;
            foreach (EscapePod pod in _pods)
            {
                if (pod.IsBroken && Vector2.Distance(pod.transform.position, _playerHealth.transform.position) <= _config.BrokenPodHintDistance)
                {
                    NearBrokenPod = pod;
                }

                if (pod.IsOpen && pod.Contains(_playerHealth.transform.position))
                {
                    ReachablePod = pod;
                }
            }

            bool inPod = ReachablePod != null;

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

        // 데이터 칩을 가지고 탈출하면 칩 하나마다 아직 없는 설계도 하나를 무작위로 등록하고 칩은 사라진다 (기획서 19.6).
        // 설계도를 다 모았으면 칩은 남는다 (팔 수 있다).
        private void RegisterBlueprints(SaveData data)
        {
            if (_config == null || _config.DataChip == null || _config.Blueprints == null)
            {
                return;
            }

            foreach (InventoryGrid grid in new[] { _inventory.Bag, _inventory.SecureBox, _inventory.DogBag })
            {
                if (grid == null)
                {
                    continue;
                }

                foreach (ItemStack stack in new System.Collections.Generic.List<ItemStack>(grid.Items))
                {
                    while (stack.Data == _config.DataChip && stack.Count > 0)
                    {
                        var missing = new System.Collections.Generic.List<ItemData>();
                        foreach (ItemData blueprint in _config.Blueprints)
                        {
                            if (blueprint != null && !data.Blueprints.Contains(blueprint.name))
                            {
                                missing.Add(blueprint);
                            }
                        }

                        if (missing.Count == 0)
                        {
                            return;
                        }

                        ItemData chosen = missing[Random.Range(0, missing.Count)];
                        data.Blueprints.Add(chosen.name);
                        NewBlueprints.Add(chosen.DisplayName);
                        stack.Count--;
                        if (stack.Count <= 0)
                        {
                            grid.Remove(stack);
                        }
                    }
                }
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

            // 탈출하면 탈출·방문 퀘스트를 센다 (기획서 14.3). 저장하기 전에 한다.
            if (outcome == RaidOutcome.Escaped && QuestLog.Current != null)
            {
                QuestLog.Current.OnEscaped();
            }

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
                if (outcome == RaidOutcome.Escaped)
                {
                    RegisterBlueprints(GameSession.Data);
                }

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
