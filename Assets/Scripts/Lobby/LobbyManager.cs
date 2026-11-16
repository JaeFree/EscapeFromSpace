using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeFromSpace
{
    // 레이드 사이의 대기실 (기획서 7장). 창고와 가진 것을 저장 파일에서 불러오고, 바뀔 때마다 저장하고, 포탈로 레이드에 보낸다.
    // 대기실에서는 구를 수 없고, 사격은 사격장(ShootingRange)에서만 된다.
    public class LobbyManager : MonoBehaviour
    {
        [Serializable]
        private class StarterItem
        {
            [SerializeField] private ItemData _item;
            [SerializeField] private int _count = 1;

            public ItemData Item => _item;
            public int Count => _count;
        }

        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private PlayerOxygen _oxygen;
        [SerializeField] private InventoryUI _inventoryUi;
        [SerializeField] private Lootable _storage;
        [SerializeField] private ItemDatabase _items;

        [Header("포탈")]
        [SerializeField] private Transform _portal;
        [SerializeField, Tooltip("칸. 이 크기의 구역에 들어가면 출격할지 묻는다")] private Vector2 _portalSize = new Vector2(3f, 2f);
        [SerializeField] private string _raidSceneName = "Station";

        // 게임을 처음 시작할 때 한 번만 창고에 넣는다. 다시 지급하지 않는다.
        [Header("기본 세트")]
        [SerializeField] private StarterItem[] _starterSet;
        [SerializeField] private int _starterSetCount = 3;

        private bool _leaving;

        // 취소한 뒤에는 포탈 밖으로 나갔다가 다시 들어와야 또 묻는다.
        private bool _declined;

        // 출격할지 묻는 창이 떠 있는지. 창은 LobbyUI가 그린다.
        public bool IsConfirming { get; private set; }

        public Lootable Storage => _storage;
        public Transform Portal => _portal;

        // 창고와 가진 것을 모두 더한 가치. 점수 역할을 한다.
        public int TotalValue =>
            Mathf.RoundToInt(PlayerInventory.ValueIn(_storage.Grid)) + _inventory.TotalValue(true);

        private void Awake()
        {
            SaveData data = GameSession.EnterLobby(out bool isNew);
            _inventory.Restore(data, _items);
            SaveConverter.Fill(_storage.Grid, data.Storage, _items);

            if (isNew)
            {
                GrantStarterSets();
                Save();
            }

            _controller.TakesOverloadDamage = false;
            _roll.IsAllowed = false;
            _oxygen.IsDraining = false;
        }

        private void OnEnable()
        {
            _inventory.Changed += Save;
        }

        private void OnDisable()
        {
            _inventory.Changed -= Save;
        }

        private void Update()
        {
            if (_leaving)
            {
                return;
            }

            bool inPortal = PortalContains(_controller.transform.position);
            if (!inPortal)
            {
                _declined = false;
            }

            if (IsConfirming)
            {
                // 묻는 동안 가방을 열면 고르던 중으로 보고 창을 닫는다.
                if (!inPortal || _controller.IsBagOpen)
                {
                    CancelLaunch();
                }

                return;
            }

            // 가방을 연 채로 지나갈 때는 묻지 않는다.
            if (inPortal && !_declined && !_controller.IsBagOpen)
            {
                IsConfirming = true;
                _controller.IsMovementLocked = true;
            }
        }

        public void ConfirmLaunch()
        {
            if (!IsConfirming || _leaving)
            {
                return;
            }

            _leaving = true;
            IsConfirming = false;
            Save();
            GameSession.StartRaid();
            SceneManager.LoadScene(_raidSceneName);
        }

        // 대기실에 그대로 남는다. 포탈 위에 서 있어도 다시 묻지 않게 표시해 둔다.
        public void CancelLaunch()
        {
            IsConfirming = false;
            _declined = true;
            _controller.IsMovementLocked = false;
        }

        private bool PortalContains(Vector2 position)
        {
            Vector2 offset = position - (Vector2)_portal.position;
            return Mathf.Abs(offset.x) <= _portalSize.x * 0.5f && Mathf.Abs(offset.y) <= _portalSize.y * 0.5f;
        }

        private void GrantStarterSets()
        {
            for (int set = 0; set < _starterSetCount; set++)
            {
                foreach (StarterItem starter in _starterSet)
                {
                    if (starter.Item != null)
                    {
                        _storage.Grid.TryAdd(new ItemStack(starter.Item, starter.Count));
                    }
                }
            }
        }

        // 아이템을 옮길 때마다 저장한다. 끌고 있는 아이템은 어느 칸에도 없으므로 놓은 뒤에 저장한다.
        private void Save()
        {
            if (_inventoryUi.IsDragging)
            {
                return;
            }

            SaveData data = GameSession.Data;
            _inventory.Capture(data);
            SaveConverter.Capture(_storage.Grid, data.Storage);
            SaveSystem.Save(data);
        }

        private void OnDrawGizmos()
        {
            if (_portal != null)
            {
                Gizmos.color = new Color(0.6f, 0.4f, 1f);
                Gizmos.DrawWireCube(_portal.position, _portalSize);
            }
        }
    }
}
