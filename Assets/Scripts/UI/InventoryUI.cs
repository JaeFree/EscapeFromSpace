using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 가방 화면과 루팅 칸 (기획서 6.1, 6.2). 칸과 아이템은 실행 중에 코드로 그린다.
    // 마우스 판정은 직접 계산한다. 좌클릭과 R이 사격·재장전과 같은 입력이라 PlayerInputReader 하나로 읽기 위해서다.
    public class InventoryUI : MonoBehaviour
    {
        private const float CellGap = 6f;
        private const float Padding = 18f;
        private const int MaxBagRows = 7;

        // 판과 칸의 색은 맵의 벽·바닥과 겹치지 않게 UiTheme에서 정한다 (2026-11-17 사용자 결정).
        private static readonly Color CellColor = UiTheme.Cell;
        private static readonly Color SecureCellColor = UiTheme.SecureCell;
        private static readonly Color CanDropColor = new Color(0.3f, 1f, 0.4f, 0.35f);
        private static readonly Color CannotDropColor = new Color(1f, 0.3f, 0.3f, 0.35f);
        private static readonly string[] EquipLabels = { "헬멧", "방탄복", "무기" };

        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField, Tooltip("칸. 루팅 칸을 대상보다 이만큼 위에 띄운다")] private float _lootPanelOffset = 0.9f;
        [SerializeField, Tooltip("가방 화면 밖에 놓으면 바닥에 버린다. 대기실에서는 끈다")] private bool _canDropToWorld = true;

        // 크기는 1920×1080 화면 기준의 픽셀이다. Play를 다시 시작하면 반영된다.
        [Header("크기")]
        [SerializeField, Tooltip("칸 한 변. 7줄 가방이 화면에 다 들어가려면 118 이하")] private float _cellSize = 108f;
        [SerializeField, Tooltip("아이템 이름 글자 크기")] private int _itemFontSize = 19;
        [SerializeField, Tooltip("수량 글자 크기")] private int _countFontSize = 22;
        [SerializeField, Tooltip("칸 이름(헬멧, 가방 등) 글자 크기")] private int _labelFontSize = 22;

        private enum TargetKind
        {
            None,
            Outside,
            Grid,
            Equip,
        }

        private struct DropTarget
        {
            public TargetKind Kind;
            public GridView View;
            public int X;
            public int Y;
            public EquipSlot Slot;
        }

        // 격자 하나의 화면 쪽. 칸 배경과 그 위의 아이템을 그린다.
        private class GridView
        {
            public InventoryGrid Grid;
            public RectTransform Root;
            public Color CellTint;
            public int BuiltHeight = -1;
            public Lootable Owner;
        }

        private Font _font;
        private Canvas _canvas;
        private Camera _camera;
        private RectTransform _bagPanel;
        private RectTransform _lootPanel;
        private RectTransform _itemLayer;
        private RectTransform _highlight;
        private RectTransform _heldView;
        private RectTransform _hint;
        private Image _hintFill;
        private GridView _bagView;
        private GridView _secureView;
        private GridView _lootView;
        private readonly RectTransform[] _equipCells = new RectTransform[3];
        private readonly List<GameObject> _itemViews = new List<GameObject>();

        private bool _isOpen;
        private Lootable _shownLootable;
        private bool _dirty = true;

        // 끌고 있는 아이템과, 못 놓았을 때 돌아갈 원래 자리
        private ItemStack _held;
        private GridView _originView;
        private int _originX;
        private int _originY;
        private bool _originRotated;
        private bool _originIsEquip;
        private EquipSlot _originSlot;

        // 묶음 나누기 (2026-11-17 사용자 결정). 우클릭하면 수량을 고르는 창이 뜨고, 고른 만큼이 마우스에 붙는다.
        // 나눈 것은 버튼을 누르고 있지 않아도 붙어 있고, 다음 좌클릭으로 놓는다.
        private RectTransform _splitPanel;
        private RectTransform _splitBar;
        private Image _splitFill;
        private Text _splitTitle;
        private Text _splitCountText;
        private RectTransform _splitMinus;
        private RectTransform _splitPlus;
        private RectTransform _splitConfirm;
        private RectTransform _splitCancel;
        private ItemStack _splitSource;
        private GridView _splitView;
        private int _splitCount;
        private bool _splitBarDragging;

        // 나눈 것을 들고 있는 동안, 못 놓으면 도로 합칠 원래 묶음
        private ItemStack _mergeBack;
        private bool _carried;

        public bool IsOpen => _isOpen;

        // 대기실 창고처럼 큰 루팅 칸이 가방 화면 오른쪽에 붙어 있는지. 화면 아래까지 덮으므로 HUD가 이 값을 보고 비킨다.
        public bool IsLootDocked { get; private set; }

        // 가방 화면(과 거기 붙은 루팅 칸)이 차지한 왼쪽 폭. 닫혀 있으면 0. HUD의 게이지가 이 오른쪽으로 옮겨 간다.
        public float LeftPanelWidth
        {
            get
            {
                if (!_isOpen)
                {
                    return 0f;
                }

                float right = _bagPanel.anchoredPosition.x + _bagPanel.sizeDelta.x;
                if (IsLootDocked)
                {
                    right = _lootPanel.anchoredPosition.x + _lootPanel.sizeDelta.x * _lootPanel.localScale.x * 0.5f;
                }

                return right;
            }
        }

        // 끌고 있는 아이템은 어느 칸에도 없다. 그 사이에 저장하면 빠지므로 대기실이 이 값을 본다.
        public bool IsDragging => _held != null;

        private float CellPitch => _cellSize + CellGap;
        private float LabelHeight => _labelFontSize + 14f;
        private float CountHeight => _countFontSize + 8f;

        // 캔버스가 화면에 바로 그려지면(Overlay) 카메라 없이 화면 좌표를 그대로 쓴다.
        private Camera UiCamera => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        private void Awake()
        {
            // 기본 글꼴에는 한글이 없다. 단색 도형 단계에서는 Windows에 깔린 글꼴을 불러 쓴다.
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 16);
            _camera = Camera.main;
            _canvas = GetComponent<Canvas>();
            Build();
        }

        // 가방은 PlayerInventory의 Awake에서 만들어진다. 어느 Awake가 먼저 돌지 모르므로 Start에서 잇는다.
        private void Start()
        {
            _bagView.Grid = _inventory.Bag;
            _secureView.Grid = _inventory.SecureBox;
        }

        private void OnEnable()
        {
            _inventory.Changed += MarkDirty;
        }

        private void OnDisable()
        {
            _inventory.Changed -= MarkDirty;
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void Update()
        {
            UpdateOpenState();
            UpdateHint();

            _bagPanel.gameObject.SetActive(_isOpen);
            _lootPanel.gameObject.SetActive(_isOpen && _shownLootable != null);
            _controller.IsBagOpen = _isOpen;

            if (!_isOpen)
            {
                IsLootDocked = false;
                return;
            }

            PositionLootPanel();
            UpdateDrag();

            if (_dirty)
            {
                // 루팅 칸의 크기는 다시 그릴 때 정해진다. 바뀐 크기로 자리를 다시 잡는다.
                Redraw();
                PositionLootPanel();
            }
        }

        // ---------- 열고 닫기 ----------

        private void UpdateOpenState()
        {
            if (_controller.IsDead)
            {
                CloseAll();
                return;
            }

            // 사라진 대상(다 주운 버린 아이템)은 닫힌 것으로 본다. 이때는 가방을 닫지 않는다.
            Lootable opened = _interactor.Opened;
            if (opened == null)
            {
                opened = null;
            }

            bool shownWasDestroyed = !ReferenceEquals(_shownLootable, null) && _shownLootable == null;

            if (_input.ToggleBagPressedThisFrame)
            {
                if (_isOpen)
                {
                    CloseAll();
                    return;
                }

                _isOpen = true;
                _dirty = true;
                Sfx.Play(SfxId.BagOpen);
            }

            if (!ReferenceEquals(opened, _shownLootable))
            {
                // 컨테이너를 열면 가방도 함께 열리고, 범위를 벗어나 닫히면 가방도 함께 닫힌다.
                CancelDrag();
                bool hadLoot = !ReferenceEquals(_shownLootable, null) && !shownWasDestroyed;
                _shownLootable = opened;
                _lootView.Grid = opened != null ? opened.Grid : null;
                _lootView.Owner = opened;
                _lootView.BuiltHeight = -1;
                _dirty = true;

                if (opened != null)
                {
                    _isOpen = true;
                }
                else if (hadLoot)
                {
                    _isOpen = false;
                }
            }
        }

        private void CloseAll()
        {
            if (!_isOpen && _shownLootable == null)
            {
                return;
            }

            CancelDrag();
            _interactor.Close();
            _shownLootable = null;
            _lootView.Grid = null;
            _lootView.Owner = null;
            if (_isOpen)
            {
                Sfx.Play(SfxId.BagClose);
            }

            _isOpen = false;
        }

        // 열 수 있는 대상 위에 "E"를, 누르고 있는 동안에는 그 아래에 진행 바를 보여 준다.
        private void UpdateHint()
        {
            Lootable target = _interactor.Target;
            bool show = target != null && !_isOpen;
            _hint.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            MoveToScreenPoint(_hint, _camera.WorldToScreenPoint(target.transform.position + Vector3.up * _lootPanelOffset));
            float progress = _interactor.HoldProgress;
            _hintFill.transform.parent.gameObject.SetActive(progress > 0f);
            _hintFill.fillAmount = progress;
        }

        private void PositionLootPanel()
        {
            IsLootDocked = false;
            if (_shownLootable == null)
            {
                return;
            }

            // 대기실 창고처럼 큰 칸은 대상 위에 띄우면 화면을 벗어난다. 가방 화면 오른쪽에 붙이고 화면에 맞게 줄인다.
            Rect screen = ((RectTransform)transform).rect;
            Vector2 size = _lootPanel.sizeDelta;
            if (size.y > screen.height * 0.6f || size.x > screen.width * 0.5f)
            {
                float left = _bagPanel.anchoredPosition.x + _bagPanel.sizeDelta.x + 24f;
                float scale = Mathf.Min(1f, (screen.height - 40f) / size.y, (screen.width - left - 24f) / size.x);
                _lootPanel.localScale = new Vector3(scale, scale, 1f);
                _lootPanel.anchoredPosition = new Vector2(left + size.x * scale * 0.5f, (screen.height - size.y * scale) * 0.5f);
                IsLootDocked = true;
                return;
            }

            _lootPanel.localScale = Vector3.one;
            MoveToScreenPoint(
                _lootPanel, _camera.WorldToScreenPoint(_shownLootable.transform.position + Vector3.up * _lootPanelOffset));
        }

        // ---------- 드래그 ----------

        private void UpdateDrag()
        {
            Vector2 mouse = _input.AimScreenPosition;
            Mouse pointer = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            bool rightClicked = pointer != null && pointer.rightButton.wasPressedThisFrame;

            if (_splitSource != null)
            {
                _highlight.gameObject.SetActive(false);
                UpdateSplit(mouse, pointer, keyboard);
                return;
            }

            if (_held == null)
            {
                _highlight.gameObject.SetActive(false);
                if (rightClicked)
                {
                    TryOpenSplit(FindTarget(mouse), mouse);
                }
                else if (_input.FirePressedThisFrame)
                {
                    TryPickUp(FindTarget(mouse));
                }

                return;
            }

            MoveToScreenPoint(_heldView, mouse);

            // 나눈 것을 들고 있을 때 우클릭이나 Esc는 취소다. 원래 묶음에 도로 합친다.
            if (_carried && (rightClicked || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)))
            {
                Sfx.Play(SfxId.ItemPlace);
                ReturnToOrigin();
                return;
            }

            // 가방을 연 상태에서 R은 회전이다.
            if (_input.ReloadPressedThisFrame && _held.Data.Width != _held.Data.Height)
            {
                _held.Rotated = !_held.Rotated;
                RebuildHeldView();
            }

            DropTarget target = FindTarget(mouse);
            ShowHighlight(target);

            if (_carried ? _input.FirePressedThisFrame : !_input.FireHeld)
            {
                Drop(target);
            }
        }

        // ---------- 묶음 나누기 ----------

        private void TryOpenSplit(DropTarget target, Vector2 mouse)
        {
            if (target.Kind != TargetKind.Grid)
            {
                return;
            }

            // 2개 이상 쌓인 묶음만 나눌 수 있다.
            ItemStack stack = target.View.Grid.GetAt(target.X, target.Y);
            if (stack == null || !stack.IsStackable || stack.Count < 2)
            {
                if (stack != null)
                {
                    Sfx.Play(SfxId.ItemRefuse);
                }

                return;
            }

            _splitSource = stack;
            _splitView = target.View;
            _splitCount = stack.Count / 2;
            _splitBarDragging = false;
            _splitTitle.text = $"{stack.Data.DisplayName} 나누기";
            _splitPanel.gameObject.SetActive(true);
            _splitPanel.SetAsLastSibling();

            // 마우스 오른쪽에 띄우되 화면 밖으로 나가지 않게 한다.
            MoveToScreenPoint(_splitPanel, mouse);
            Rect screen = ((RectTransform)transform).rect;
            Vector2 size = _splitPanel.sizeDelta;
            Vector3 position = _splitPanel.localPosition;
            position.x = Mathf.Clamp(position.x + 20f, screen.xMin + 10f, screen.xMax - size.x - 10f);
            position.y = Mathf.Clamp(position.y, screen.yMin + size.y * 0.5f + 10f, screen.yMax - size.y * 0.5f - 10f);
            _splitPanel.localPosition = position;

            RefreshSplit();
            Sfx.Play(SfxId.UiPrompt);
        }

        private void UpdateSplit(Vector2 mouse, Mouse pointer, Keyboard keyboard)
        {
            // 창이 떠 있는 사이에 묶음이 사라지거나 줄었으면(재장전 등) 닫는다.
            if (_splitView.Grid == null || !_splitView.Grid.Contains(_splitSource) || _splitSource.Count < 2)
            {
                CloseSplit();
                return;
            }

            int max = _splitSource.Count - 1;
            int count = Mathf.Clamp(_splitCount, 1, max);
            bool clicked = _input.FirePressedThisFrame;

            // 막대를 누르거나 끌어서 수량을 고른다.
            if (clicked && Contains(_splitBar, mouse))
            {
                _splitBarDragging = true;
            }

            if (!_input.FireHeld)
            {
                _splitBarDragging = false;
            }

            if (_splitBarDragging
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(_splitBar, mouse, UiCamera, out Vector2 local))
            {
                float ratio = Mathf.Clamp01(local.x / _splitBar.sizeDelta.x);
                count = Mathf.Clamp(Mathf.RoundToInt(ratio * _splitSource.Count), 1, max);
            }

            // 한 개씩: − / + 버튼, 마우스 휠, 좌우 화살표
            int step = 0;
            if (clicked && Contains(_splitMinus, mouse))
            {
                step = -1;
            }
            else if (clicked && Contains(_splitPlus, mouse))
            {
                step = 1;
            }

            if (pointer != null)
            {
                float wheel = pointer.scroll.ReadValue().y;
                step += wheel > 0f ? 1 : wheel < 0f ? -1 : 0;
            }

            if (keyboard != null)
            {
                step += keyboard.rightArrowKey.wasPressedThisFrame ? 1 : 0;
                step -= keyboard.leftArrowKey.wasPressedThisFrame ? 1 : 0;
            }

            count = Mathf.Clamp(count + step, 1, max);
            if (count != _splitCount)
            {
                _splitCount = count;
                RefreshSplit();
            }

            bool enter = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
            bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool rightClicked = pointer != null && pointer.rightButton.wasPressedThisFrame;

            if (enter || (clicked && Contains(_splitConfirm, mouse)))
            {
                ConfirmSplit();
            }
            else if (escape || rightClicked || (clicked && (Contains(_splitCancel, mouse) || !Contains(_splitPanel, mouse))))
            {
                Sfx.Play(SfxId.UiCancel);
                CloseSplit();
            }
        }

        private void RefreshSplit()
        {
            _splitCountText.text = $"{_splitCount} / {_splitSource.Count}";
            _splitFill.fillAmount = _splitCount / (float)_splitSource.Count;
        }

        // 고른 만큼을 떼어 마우스에 붙인다. 원래 묶음은 제자리에 남는다.
        private void ConfirmSplit()
        {
            ItemStack source = _splitSource;
            GridView view = _splitView;
            int count = Mathf.Clamp(_splitCount, 1, source.Count - 1);
            CloseSplit();

            (int x, int y) = view.Grid.PositionOf(source);
            source.Count -= count;

            _held = new ItemStack(source.Data, count) { Rotated = source.Rotated };
            _originView = view;
            _originX = x;
            _originY = y;
            _originRotated = source.Rotated;
            _originIsEquip = false;
            _mergeBack = source;
            _carried = true;

            RebuildHeldView();
            _dirty = true;
            Sfx.Play(SfxId.ItemPick);
        }

        private void CloseSplit()
        {
            _splitSource = null;
            _splitView = null;
            _splitBarDragging = false;
            _splitPanel.gameObject.SetActive(false);
        }

        private bool Contains(RectTransform rect, Vector2 screenPoint)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, UiCamera);
        }

        private void TryPickUp(DropTarget target)
        {
            if (target.Kind == TargetKind.Grid)
            {
                ItemStack stack = target.View.Grid.GetAt(target.X, target.Y);
                if (stack == null)
                {
                    return;
                }

                (int x, int y) = target.View.Grid.PositionOf(stack);
                _held = stack;
                _originView = target.View;
                _originX = x;
                _originY = y;
                _originRotated = stack.Rotated;
                _originIsEquip = false;
                target.View.Grid.Remove(stack);

                // 버린 아이템은 비면 사라진다. 끌고 있는 동안에는 돌아갈 자리로 남겨 둔다.
                if (target.View.Owner != null)
                {
                    target.View.Owner.KeepAlive = true;
                }
            }
            else if (target.Kind == TargetKind.Equip)
            {
                ItemStack stack = _inventory.GetEquipped(target.Slot);
                if (stack == null)
                {
                    return;
                }

                // 추가 줄에 아이템이 있으면 방탄복을 벗을 수 없다.
                if (target.Slot == EquipSlot.Vest && !_inventory.CanSetVest(null))
                {
                    return;
                }

                _held = stack;
                _originIsEquip = true;
                _originSlot = target.Slot;
                _originRotated = stack.Rotated;
                _inventory.SetEquipped(target.Slot, null);
            }
            else
            {
                return;
            }

            RebuildHeldView();
            _dirty = true;
            Sfx.Play(SfxId.ItemPick);
        }

        private bool CanDrop(DropTarget target)
        {
            switch (target.Kind)
            {
                case TargetKind.Outside:
                    return _canDropToWorld;

                case TargetKind.Grid:
                    ItemStack existing = target.View.Grid.GetAt(target.X, target.Y);
                    if (existing != null && existing.Data == _held.Data && _held.IsStackable)
                    {
                        return existing.Count < existing.Data.MaxStack;
                    }

                    return target.View.Grid.CanPlace(_held, target.X, target.Y);

                case TargetKind.Equip:
                    if (!PlayerInventory.Accepts(target.Slot, _held))
                    {
                        return false;
                    }

                    if (target.Slot == EquipSlot.Vest && !_inventory.CanSetVest(_held))
                    {
                        return false;
                    }

                    // 이미 장비가 있으면 맞바꾼다. 원래 장비가 끌어온 자리에 들어가야 한다.
                    ItemStack old = _inventory.GetEquipped(target.Slot);
                    return old == null || (!_originIsEquip && _originView.Grid.CanPlace(old, _originX, _originY));

                default:
                    return false;
            }
        }

        private void Drop(DropTarget target)
        {
            if (!CanDrop(target))
            {
                // 제자리에 도로 놓은 것은 거절이 아니다.
                bool sameSpot = target.Kind == TargetKind.Grid && !_originIsEquip && target.View == _originView
                    && target.X == _originX && target.Y == _originY;
                Sfx.Play(sameSpot || target.Kind == TargetKind.None ? SfxId.ItemPlace : SfxId.ItemRefuse);
                ReturnToOrigin();
                return;
            }

            ItemStack held = _held;

            switch (target.Kind)
            {
                case TargetKind.Outside:
                    held.Rotated = false;
                    _interactor.DropToWorld(held);
                    Sfx.Play(SfxId.ItemDropToWorld);
                    break;

                case TargetKind.Grid:
                    ItemStack existing = target.View.Grid.GetAt(target.X, target.Y);
                    if (existing != null)
                    {
                        // 같은 탄약 위에 놓으면 최대치까지 합치고, 남는 것은 원래 자리로 돌아간다.
                        int moved = Mathf.Min(existing.Data.MaxStack - existing.Count, held.Count);
                        existing.Count += moved;
                        held.Count -= moved;
                        if (held.Count > 0)
                        {
                            ReturnToOrigin();
                            return;
                        }
                    }
                    else
                    {
                        target.View.Grid.Place(held, target.X, target.Y);
                    }

                    break;

                case TargetKind.Equip:
                    ItemStack old = _inventory.GetEquipped(target.Slot);
                    _inventory.SetEquipped(target.Slot, held);
                    if (old != null)
                    {
                        _originView.Grid.Place(old, _originX, _originY);
                    }

                    break;
            }

            if (target.Kind != TargetKind.Outside)
            {
                Sfx.Play(SfxId.ItemPlace);
            }

            EndDrag();
        }

        private void ReturnToOrigin()
        {
            if (_held == null)
            {
                return;
            }

            _held.Rotated = _originRotated;

            // 나눈 것은 원래 묶음에 도로 합친다. 그 묶음이 사라졌으면 아래의 보통 경로로 간다.
            if (_mergeBack != null && _originView.Grid != null && _originView.Grid.Contains(_mergeBack))
            {
                _mergeBack.Count += _held.Count;
            }
            else if (_originIsEquip)
            {
                _inventory.SetEquipped(_originSlot, _held);
            }
            else if (_originView.Grid != null && _originView.Grid.CanPlace(_held, _originX, _originY))
            {
                _originView.Grid.Place(_held, _originX, _originY);
            }
            else
            {
                // 돌아갈 자리가 없어졌다 (루팅 대상이 사라짐). 잃어버리지 않게 발밑에 둔다.
                _interactor.DropToWorld(_held);
            }

            EndDrag();
        }

        private void CancelDrag()
        {
            CloseSplit();
            ReturnToOrigin();
        }

        private void EndDrag()
        {
            if (_originView != null && _originView.Owner != null)
            {
                _originView.Owner.KeepAlive = false;
            }

            _held = null;
            _originView = null;
            _mergeBack = null;
            _carried = false;
            _heldView.gameObject.SetActive(false);
            _highlight.gameObject.SetActive(false);
            _inventory.NotifyChanged();
            _dirty = true;
        }

        // ---------- 마우스 판정 ----------

        // 화면 좌표의 점으로 옮긴다. 캔버스 방식(Overlay / Camera)에 상관없이 맞도록 캔버스 기준 좌표로 바꿔 넣는다.
        private void MoveToScreenPoint(RectTransform rect, Vector2 screenPoint)
        {
            var root = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, UiCamera, out Vector2 local))
            {
                rect.localPosition = local;
            }
        }

        private DropTarget FindTarget(Vector2 mouse)
        {
            var target = new DropTarget { Kind = TargetKind.None };

            for (int i = 0; i < _equipCells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_equipCells[i], mouse, UiCamera))
                {
                    target.Kind = TargetKind.Equip;
                    target.Slot = (EquipSlot)i;
                    return target;
                }
            }

            if (TryFindCell(_bagView, mouse, ref target) || TryFindCell(_secureView, mouse, ref target)
                || TryFindCell(_lootView, mouse, ref target))
            {
                return target;
            }

            bool overPanel = RectTransformUtility.RectangleContainsScreenPoint(_bagPanel, mouse, UiCamera)
                || (_shownLootable != null && RectTransformUtility.RectangleContainsScreenPoint(_lootPanel, mouse, UiCamera));
            target.Kind = overPanel ? TargetKind.None : TargetKind.Outside;
            return target;
        }

        private bool TryFindCell(GridView view, Vector2 mouse, ref DropTarget target)
        {
            if (view.Grid == null || !view.Root.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Root, mouse, UiCamera, out Vector2 local))
            {
                return false;
            }

            // 격자의 원점은 왼쪽 위다. 아래로 갈수록 y칸이 커진다.
            int x = Mathf.FloorToInt(local.x / CellPitch);
            int y = Mathf.FloorToInt(-local.y / CellPitch);
            if (local.x < 0f || local.y > 0f || x >= view.Grid.Width || y >= view.Grid.Height)
            {
                return false;
            }

            target.Kind = TargetKind.Grid;
            target.View = view;
            target.X = x;
            target.Y = y;
            return true;
        }

        private void ShowHighlight(DropTarget target)
        {
            if (target.Kind != TargetKind.Grid && target.Kind != TargetKind.Equip)
            {
                _highlight.gameObject.SetActive(false);
                return;
            }

            _highlight.gameObject.SetActive(true);
            _highlight.GetComponent<Image>().color = CanDrop(target) ? CanDropColor : CannotDropColor;

            if (target.Kind == TargetKind.Equip)
            {
                _highlight.SetParent(_equipCells[(int)target.Slot], false);
                _highlight.anchoredPosition = Vector2.zero;
                _highlight.sizeDelta = new Vector2(_cellSize, _cellSize);
            }
            else
            {
                _highlight.SetParent(target.View.Root, false);
                _highlight.anchoredPosition = new Vector2(target.X * CellPitch, -target.Y * CellPitch);
                _highlight.sizeDelta = ItemSize(_held);
            }

            _highlight.SetAsLastSibling();
        }

        // ---------- 그리기 ----------

        private void Build()
        {
            var root = (RectTransform)transform;

            // 가방 화면: 화면 왼쪽에 세로로. 위에서부터 장비 칸, 가방, 가방 오른쪽에 보관함.
            float width = Padding * 2f + CellPitch * 4f + 12f;
            float height = Padding * 2f + LabelHeight * 2f + CellPitch * (1 + MaxBagRows) + 20f;
            _bagPanel = CreateRect("BagPanel", root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f),
                new Vector2(width, height));
            UiBuilder.Window(_bagPanel);

            float y = -Padding;
            for (int i = 0; i < 3; i++)
            {
                float x = Padding + i * CellPitch;
                CreateText(EquipLabels[i], _bagPanel, new Vector2(x, y), new Vector2(_cellSize, LabelHeight), _labelFontSize, TextAnchor.MiddleCenter);
                _equipCells[i] = CreateRect($"Equip {EquipLabels[i]}", _bagPanel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, y - LabelHeight), new Vector2(_cellSize, _cellSize));
                UiBuilder.Cell(_equipCells[i], CellColor);
            }

            y -= LabelHeight + CellPitch + 20f;
            CreateText("가방", _bagPanel, new Vector2(Padding, y), new Vector2(CellPitch * 3f, LabelHeight), _labelFontSize, TextAnchor.MiddleLeft);
            CreateText("보관함", _bagPanel, new Vector2(Padding + CellPitch * 3f + 12f, y), new Vector2(_cellSize, LabelHeight),
                _labelFontSize, TextAnchor.MiddleCenter);
            y -= LabelHeight;

            _bagView = new GridView
            {
                CellTint = CellColor,
                Root = CreateRect("BagGrid", _bagPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Padding, y), Vector2.zero),
            };
            _secureView = new GridView
            {
                CellTint = SecureCellColor,
                Root = CreateRect("SecureGrid", _bagPanel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(Padding + CellPitch * 3f + 12f, y), Vector2.zero),
            };

            // 보관함 아래의 빈 자리에 조작 안내를 둔다.
            Text help = CreateText("우클릭\n묶음 나누기", _bagPanel, new Vector2(Padding + CellPitch * 3f + 12f, y - CellPitch - 4f),
                new Vector2(_cellSize, 60f), 18, TextAnchor.UpperCenter);
            help.color = UiTheme.TextDim;

            // 루팅 칸: 대상의 위쪽에 뜬다. 크기는 열 때 대상에 맞춘다.
            _lootPanel = CreateRect("LootPanel", root, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            _lootPanel.anchorMax = Vector2.zero;
            UiBuilder.Window(_lootPanel);
            _lootView = new GridView
            {
                CellTint = CellColor,
                Root = CreateRect("LootGrid", _lootPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Padding, -Padding), Vector2.zero),
            };

            _highlight = CreateRect("Highlight", _bagView.Root, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
                new Vector2(_cellSize, _cellSize));
            _highlight.gameObject.AddComponent<Image>().raycastTarget = false;
            _highlight.gameObject.SetActive(false);

            // "E" 안내와 그 아래의 진행 바
            _hint = CreateRect("InteractHint", root, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(60f, 60f));
            _hint.anchorMax = Vector2.zero;
            UiBuilder.Panel(_hint, UiTheme.PanelFill, UiTheme.PanelBorder);
            CreateText("E", _hint, Vector2.zero, new Vector2(60f, 60f), 32, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            _hintFill = UiBuilder.Bar("HoldProgress", _hint, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -6f),
                new Vector2(120f, 18f), new Color(1f, 0.9f, 0.3f));
            _hint.gameObject.SetActive(false);

            // 끌고 있는 아이템은 모든 것 위에 그린다.
            _itemLayer = CreateRect("DragLayer", root, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _itemLayer.anchorMax = Vector2.zero;
            _heldView = CreateRect("HeldItem", _itemLayer, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _heldView.anchorMax = Vector2.zero;
            _heldView.gameObject.SetActive(false);

            BuildSplitPanel(root);

            _bagPanel.gameObject.SetActive(false);
            _lootPanel.gameObject.SetActive(false);
        }

        // 나눌 수량을 고르는 작은 창: 이름, 수량 막대, − 수량 +, 확인과 취소
        private void BuildSplitPanel(RectTransform root)
        {
            var size = new Vector2(380f, 262f);
            _splitPanel = CreateRect("SplitPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, size);
            UiBuilder.Window(_splitPanel);

            _splitTitle = CreateText(string.Empty, _splitPanel, new Vector2(20f, -12f), new Vector2(size.x - 40f, 36f), 24, TextAnchor.MiddleCenter);
            _splitTitle.color = UiTheme.Accent;
            _splitTitle.fontStyle = FontStyle.Bold;

            _splitFill = UiBuilder.Bar("Amount", _splitPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f),
                new Vector2(size.x - 40f, 34f), UiTheme.Accent);
            _splitBar = (RectTransform)_splitFill.transform.parent;

            var stepSize = new Vector2(56f, 46f);
            _splitMinus = UiBuilder.Button("−", _splitPanel, new Vector2(0.5f, 1f), new Vector2(-130f, -108f), stepSize, false);
            _splitPlus = UiBuilder.Button("+", _splitPanel, new Vector2(0.5f, 1f), new Vector2(130f, -108f), stepSize, false);
            _splitCountText = CreateText(string.Empty, _splitPanel, new Vector2(size.x * 0.5f - 95f, -108f), new Vector2(190f, 46f), 28,
                TextAnchor.MiddleCenter);
            _splitCountText.fontStyle = FontStyle.Bold;

            var buttonSize = new Vector2(160f, 50f);
            _splitConfirm = UiBuilder.Button("확인", _splitPanel, new Vector2(0.5f, 0f), new Vector2(-88f, 26f), buttonSize, true);
            _splitCancel = UiBuilder.Button("취소", _splitPanel, new Vector2(0.5f, 0f), new Vector2(88f, 26f), buttonSize, false);

            _splitPanel.gameObject.SetActive(false);
        }

        private void Redraw()
        {
            _dirty = false;

            foreach (GameObject view in _itemViews)
            {
                Destroy(view);
            }

            _itemViews.Clear();

            BuildCells(_bagView);
            BuildCells(_secureView);
            DrawItems(_bagView);
            DrawItems(_secureView);

            if (_lootView.Grid != null)
            {
                BuildCells(_lootView);
                DrawItems(_lootView);
                _lootPanel.sizeDelta = new Vector2(
                    Padding * 2f + _lootView.Grid.Width * CellPitch - CellGap,
                    Padding * 2f + _lootView.Grid.Height * CellPitch - CellGap);
            }

            for (int i = 0; i < _equipCells.Length; i++)
            {
                ItemStack stack = _inventory.GetEquipped((EquipSlot)i);
                if (stack != null)
                {
                    _itemViews.Add(CreateItemView(stack, _equipCells[i], Vector2.zero, new Vector2(_cellSize, _cellSize)).gameObject);
                }
            }
        }

        // 칸 배경. 줄 수가 바뀌었을 때만(방탄복) 다시 만든다.
        private void BuildCells(GridView view)
        {
            if (view.BuiltHeight == view.Grid.Height)
            {
                return;
            }

            for (int i = view.Root.childCount - 1; i >= 0; i--)
            {
                Transform child = view.Root.GetChild(i);
                if (child != _highlight)
                {
                    Destroy(child.gameObject);
                }
            }

            for (int y = 0; y < view.Grid.Height; y++)
            {
                for (int x = 0; x < view.Grid.Width; x++)
                {
                    RectTransform cell = CreateRect("Cell", view.Root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(x * CellPitch, -y * CellPitch), new Vector2(_cellSize, _cellSize));
                    UiBuilder.Cell(cell, view.CellTint);
                }
            }

            view.BuiltHeight = view.Grid.Height;
        }

        private void DrawItems(GridView view)
        {
            foreach (ItemStack stack in view.Grid.Items)
            {
                (int x, int y) = view.Grid.PositionOf(stack);
                RectTransform item = CreateItemView(stack, view.Root, new Vector2(x * CellPitch, -y * CellPitch), ItemSize(stack));
                _itemViews.Add(item.gameObject);
            }
        }

        private void RebuildHeldView()
        {
            for (int i = _heldView.childCount - 1; i >= 0; i--)
            {
                Destroy(_heldView.GetChild(i).gameObject);
            }

            Vector2 size = ItemSize(_held);
            RectTransform view = CreateItemView(_held, _heldView, new Vector2(-size.x * 0.5f, size.y * 0.5f), size);
            view.GetComponent<Image>().color = new Color(_held.Data.Color.r, _held.Data.Color.g, _held.Data.Color.b, 0.8f);
            _heldView.gameObject.SetActive(true);
        }

        private Vector2 ItemSize(ItemStack stack)
        {
            return new Vector2(stack.Width * CellPitch - CellGap, stack.Height * CellPitch - CellGap);
        }

        // 단색 도형 단계의 아이템 모양: 종류별 색 사각형 + 이름 + 수량
        private RectTransform CreateItemView(ItemStack stack, RectTransform parent, Vector2 position, Vector2 size)
        {
            RectTransform view = CreateRect(stack.Data.DisplayName, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
            UiBuilder.Cell(view, stack.Data.Color);

            // 그림은 이름과 수량 아래에 깔린다.
            Image icon = UiBuilder.Icon(view, Mathf.Min(size.x, size.y) * 0.62f);
            UiBuilder.SetIcon(icon, stack.Data.Icon, Color.white);

            Text name = CreateText(stack.Data.DisplayName, view, new Vector2(5f, -3f), new Vector2(size.x - 10f, size.y - CountHeight), _itemFontSize,
                TextAnchor.UpperLeft);
            name.color = Color.black;

            // 탄약은 발 수, 무기는 탄창에 남은 탄을 보여 준다.
            string count = null;
            if (stack.IsStackable)
            {
                count = stack.Count.ToString();
            }
            else if (stack.Data.Weapon != null && stack.Data.Weapon.MagazineSize > 0)
            {
                count = $"{stack.AmmoInMagazine}/{stack.Data.Weapon.MagazineSize}";
            }

            if (count != null)
            {
                Text countText = CreateText(count, view, new Vector2(5f, -(size.y - CountHeight)), new Vector2(size.x - 10f, CountHeight - 2f), _countFontSize,
                    TextAnchor.LowerRight);
                countText.color = Color.black;
                countText.fontStyle = FontStyle.Bold;
            }

            return view;
        }

        private static RectTransform CreateRect(
            string objectName, RectTransform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private Text CreateText(string content, RectTransform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            RectTransform rect = CreateRect("Text", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.text = content;
            text.color = UiTheme.Text;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
    }
}
