using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 대기실 강화 단말기의 화면 (기획서 12장). 단말기 가까이에서 E로 열고, 노드를 골라 포인트와 재료로 강화한다.
    // 마우스 판정은 다른 화면처럼 직접 계산한다.
    public class TechTreeUI : MonoBehaviour
    {
        private static readonly Color LineColor = new Color(0.36f, 0.32f, 0.46f, 1f);
        private static readonly Color LockedFill = new Color(0.1f, 0.085f, 0.15f, 1f);
        private static readonly Color LockedText = new Color(0.5f, 0.47f, 0.56f, 1f);
        private static readonly Color DoneFill = new Color(0.62f, 0.36f, 0.08f, 1f);
        private const string OkColor = "#7CF08F";
        private const string LackColor = "#FF7A6B";

        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private LobbyManager _lobby;
        [SerializeField, Tooltip("강화 단말기 오브젝트")] private Transform _terminal;
        [SerializeField, Tooltip("칸. 단말기에서 이 거리 안에 있어야 열 수 있다")] private float _range = 1.8f;
        [SerializeField, Tooltip("칸. 이름표와 E 안내를 단말기보다 이만큼 위에 띄운다")] private float _labelOffset = 0.8f;

        // 크기는 1920×1080 화면 기준의 픽셀이다. Play를 다시 시작하면 반영된다.
        [Header("크기")]
        [SerializeField] private Vector2 _nodeSize = new Vector2(214f, 68f);
        [SerializeField, Tooltip("열 사이, 줄 사이의 간격")] private Vector2 _nodePitch = new Vector2(250f, 82f);

        private class NodeView
        {
            public TechTree.Node Node;
            public RectTransform Rect;
            public Image Fill;
            public Image Border;
            public Text Name;
            public Text Effect;
        }

        private class LineView
        {
            public Image Image;
            public TechTree.Node From;
            public TechTree.Node To;
        }

        private readonly List<NodeView> _nodes = new List<NodeView>();
        private readonly List<LineView> _lines = new List<LineView>();
        private Canvas _canvas;
        private Camera _camera;
        private RectTransform _root;
        private RectTransform _window;
        private RectTransform _hint;
        private RectTransform _label;
        private RectTransform _selection;
        private RectTransform _button;
        private Image _buttonFill;
        private Text _buttonText;
        private Text _levelText;
        private Text _xpText;
        private Text _pointText;
        private Image _xpBar;
        private Text _detailName;
        private Text _detailEffect;
        private Text _detailBody;
        private Text _materialText;
        private NodeView _selected;
        private bool _isOpen;
        private bool _dirty;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _camera = Camera.main;
        }

        // 트리는 PlayerProgress의 Awake 뒤에 읽어야 해서 Start에서 만든다.
        private void Start()
        {
            Build();
            PlayerProgress.Current.Changed += MarkDirty;
            _inventory.Changed += MarkDirty;
        }

        private void OnDestroy()
        {
            if (PlayerProgress.Current != null)
            {
                PlayerProgress.Current.Changed -= MarkDirty;
            }

            _inventory.Changed -= MarkDirty;
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void Update()
        {
            Vector3 above = _terminal.position + Vector3.up * _labelOffset;
            UiBuilder.MoveToScreenPoint(_canvas, _label, _camera.WorldToScreenPoint(above));

            bool near = !_controller.IsDead && Vector2.Distance(_controller.transform.position, _terminal.position) <= _range;
            bool free = !_controller.IsBagOpen && !_lobby.IsConfirming;
            Keyboard keyboard = Keyboard.current;

            if (!_isOpen)
            {
                _hint.gameObject.SetActive(near && free);
                if (near && free)
                {
                    UiBuilder.MoveToScreenPoint(_canvas, _hint, _camera.WorldToScreenPoint(above + Vector3.up * 0.55f));
                    if (_input.InteractPressedThisFrame)
                    {
                        Open();
                    }
                }

                return;
            }

            // E나 Esc로 닫는다. 가방을 열어도 닫힌다.
            if (!near || _controller.IsBagOpen || _input.InteractPressedThisFrame
                || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                Close();
                return;
            }

            HandleClick(keyboard);

            if (_dirty)
            {
                Refresh();
            }
        }

        private void Open()
        {
            _isOpen = true;
            _controller.IsMovementLocked = true;
            _hint.gameObject.SetActive(false);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Refresh();
            Sfx.Play(SfxId.UiPrompt);
        }

        private void Close()
        {
            _isOpen = false;
            _controller.IsMovementLocked = false;
            _root.gameObject.SetActive(false);
            Sfx.Play(SfxId.UiCancel);
        }

        private void HandleClick(Keyboard keyboard)
        {
            Mouse mouse = Mouse.current;
            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool entered = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
            Vector2 point = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            Camera uiCamera = UiBuilder.CameraOf(_canvas);

            if (entered || (clicked && RectTransformUtility.RectangleContainsScreenPoint(_button, point, uiCamera)))
            {
                TryUnlock();
                return;
            }

            if (!clicked)
            {
                return;
            }

            foreach (NodeView view in _nodes)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(view.Rect, point, uiCamera))
                {
                    _selected = view;
                    _dirty = true;
                    Sfx.Play(SfxId.ItemPick);
                    return;
                }
            }
        }

        private void TryUnlock()
        {
            PlayerProgress progress = PlayerProgress.Current;
            if (_selected == null || progress.CheckUnlock(_selected.Node, CountItem) != TechBlock.None)
            {
                Sfx.Play(SfxId.ItemRefuse);
                return;
            }

            TechTree.Node node = _selected.Node;
            if (node.Costs != null)
            {
                foreach (TechTree.Cost cost in node.Costs)
                {
                    if (cost.Item != null)
                    {
                        Consume(cost.Item, cost.Count);
                    }
                }
            }

            progress.Unlock(node);
            Sfx.Play(SfxId.LevelUp);

            // 재료가 빠진 가방과 창고, 강화한 내용을 저장하게 한다 (대기실은 가진 것이 바뀔 때마다 저장한다).
            _inventory.NotifyChanged();
            _dirty = true;
        }

        // ---------- 재료 ----------

        // 재료는 창고, 가방, 보관함에 있는 것을 모두 센다.
        private IEnumerable<InventoryGrid> MaterialGrids()
        {
            yield return _lobby.Storage.Grid;
            yield return _inventory.Bag;
            yield return _inventory.SecureBox;
        }

        private int CountItem(ItemData item)
        {
            int total = 0;
            foreach (InventoryGrid grid in MaterialGrids())
            {
                foreach (ItemStack stack in grid.Items)
                {
                    if (stack.Data == item)
                    {
                        total += stack.Count;
                    }
                }
            }

            return total;
        }

        // 창고의 것부터 쓴다. 다 쓴 묶음은 칸에서 사라진다.
        private void Consume(ItemData item, int amount)
        {
            foreach (InventoryGrid grid in MaterialGrids())
            {
                var stacks = new List<ItemStack>(grid.Items);
                foreach (ItemStack stack in stacks)
                {
                    if (amount <= 0)
                    {
                        return;
                    }

                    if (stack.Data != item)
                    {
                        continue;
                    }

                    int used = Mathf.Min(amount, stack.Count);
                    stack.Count -= used;
                    amount -= used;
                    if (stack.Count <= 0)
                    {
                        grid.Remove(stack);
                    }
                }
            }
        }

        // ---------- 그리기 ----------

        private void Refresh()
        {
            _dirty = false;
            PlayerProgress progress = PlayerProgress.Current;

            _levelText.text = $"레벨 {progress.Level}";
            _xpBar.fillAmount = progress.IsMaxLevel ? 1f : progress.Xp / (float)progress.XpToNext;
            _xpText.text = progress.IsMaxLevel ? "최고 레벨" : $"{progress.Xp} / {progress.XpToNext}";
            _pointText.text = $"강화 포인트  {progress.Points}";

            foreach (NodeView view in _nodes)
            {
                bool unlocked = progress.IsUnlocked(view.Node.Id);
                bool reachable = progress.HasPrerequisites(view.Node);

                // 강화한 것은 호박색, 지금 찍을 수 있는 줄은 밝게, 아직 먼 것은 어둡게.
                view.Fill.color = unlocked ? DoneFill : reachable ? UiTheme.ButtonQuietFill : LockedFill;
                view.Border.color = unlocked ? UiTheme.Accent : reachable ? UiTheme.Text : LineColor;
                view.Name.color = unlocked || reachable ? UiTheme.Text : LockedText;
                view.Effect.color = unlocked ? UiTheme.Text : reachable ? UiTheme.Accent : LockedText;
            }

            foreach (LineView line in _lines)
            {
                line.Image.color = progress.IsUnlocked(line.From.Id)
                    ? (progress.IsUnlocked(line.To.Id) ? UiTheme.Accent : UiTheme.Text)
                    : LineColor;
            }

            _selection.gameObject.SetActive(_selected != null);
            if (_selected != null)
            {
                _selection.SetParent(_selected.Rect, false);
                _selection.anchoredPosition = Vector2.zero;
            }

            RefreshDetail(progress);
            RefreshMaterials(progress);
        }

        private void RefreshDetail(PlayerProgress progress)
        {
            if (_selected == null)
            {
                _detailName.text = "노드를 고르세요";
                _detailEffect.text = string.Empty;
                _detailBody.text = "왼쪽 트리에서 노드를 누르면\n효과와 필요한 것이 보입니다.";
                _button.gameObject.SetActive(false);
                return;
            }

            TechTree.Node node = _selected.Node;
            _detailName.text = node.DisplayName;
            _detailEffect.text = TechTree.Describe(node.Stat, node.Amount);
            _button.gameObject.SetActive(true);

            if (progress.IsUnlocked(node.Id))
            {
                _detailBody.text = "강화를 마쳤습니다.";
                _buttonText.text = "강화 완료";
                _buttonFill.color = UiTheme.ButtonQuietFill;
                return;
            }

            var body = new StringBuilder();
            if (node.Requires != null && node.Requires.Length > 0)
            {
                body.AppendLine("선행 강화");
                foreach (string id in node.Requires)
                {
                    TechTree.Node required = progress.Tree.Find(id);
                    string requiredName = required != null ? required.DisplayName : id;
                    body.AppendLine(Colored($"  {requiredName}", progress.IsUnlocked(id)));
                }
            }

            body.AppendLine(Colored($"레벨 {node.RequiredLevel} 이상", progress.Level >= node.RequiredLevel));
            body.AppendLine(Colored($"강화 포인트 {node.PointCost}", progress.Points >= node.PointCost));

            if (node.Costs != null)
            {
                foreach (TechTree.Cost cost in node.Costs)
                {
                    if (cost.Item != null)
                    {
                        int have = CountItem(cost.Item);
                        body.AppendLine(Colored($"{cost.Item.DisplayName} {cost.Count}개  (보유 {have})", have >= cost.Count));
                    }
                }
            }

            _detailBody.text = body.ToString();

            bool can = progress.CheckUnlock(node, CountItem) == TechBlock.None;
            _buttonText.text = can ? "강화 (Enter)" : "조건 부족";
            _buttonFill.color = can ? UiTheme.ButtonFill : UiTheme.ButtonQuietFill;
        }

        // 트리에 쓰이는 재료를 모두 모아 가진 수를 보여 준다.
        private void RefreshMaterials(PlayerProgress progress)
        {
            var seen = new List<ItemData>();
            var text = new StringBuilder("보유 재료 (창고 + 가방)\n");
            foreach (TechTree.Node node in progress.Tree.Nodes)
            {
                if (node.Costs == null)
                {
                    continue;
                }

                foreach (TechTree.Cost cost in node.Costs)
                {
                    if (cost.Item != null && !seen.Contains(cost.Item))
                    {
                        seen.Add(cost.Item);
                        text.AppendLine($"{cost.Item.DisplayName}  {CountItem(cost.Item)}");
                    }
                }
            }

            _materialText.text = text.ToString();
        }

        private static string Colored(string content, bool ok)
        {
            return $"<color={(ok ? OkColor : LackColor)}>{(ok ? "○" : "×")} {content}</color>";
        }

        private void Build()
        {
            TechTree tree = PlayerProgress.Current.Tree;

            // 단말기 위의 이름표와 E 안내
            _label = UiBuilder.Rect("TechLabel", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(190f, 44f));
            UiBuilder.Panel(_label, UiTheme.SlotFill, UiTheme.SlotBorder);
            UiBuilder.Text("강화 단말기", _label, 22, TextAnchor.MiddleCenter, UiTheme.Text);
            _label.SetAsFirstSibling();

            _hint = UiBuilder.Rect("TechHint", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(60f, 60f));
            UiBuilder.Panel(_hint, UiTheme.PanelFill, UiTheme.PanelBorder);
            UiBuilder.Text("E", _hint, 32, TextAnchor.MiddleCenter, UiTheme.Text).fontStyle = FontStyle.Bold;
            _hint.gameObject.SetActive(false);

            _root = UiBuilder.Dim("TechTree", transform);

            int columns = 0;
            int rows = 0;
            foreach (TechTree.Node node in tree.Nodes)
            {
                columns = Mathf.Max(columns, node.Column + 1);
                rows = Mathf.Max(rows, node.Row + 1);
            }

            const float Padding = 36f;
            const float HeaderHeight = 84f;
            const float DetailWidth = 430f;
            var treeSize = new Vector2((columns - 1) * _nodePitch.x + _nodeSize.x, (rows - 1) * _nodePitch.y + _nodeSize.y);
            var windowSize = new Vector2(Padding * 3f + treeSize.x + DetailWidth, Padding * 2f + HeaderHeight + treeSize.y);

            _window = UiBuilder.Rect("Window", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, windowSize);
            UiBuilder.Window(_window);

            // 은신 갈래(기획서 19.3)가 붙어 줄이 늘면 높이 1080 화면을 넘는다. 넘으면 창을 통째로 줄인다.
            float fit = Mathf.Min(1f, 1030f / windowSize.y, 1880f / windowSize.x);
            _window.localScale = new Vector3(fit, fit, 1f);

            BuildHeader(windowSize.x, Padding);

            // 트리: 왼쪽 위가 원점이고 아래로 갈수록 줄 번호가 커진다. 선을 먼저 그려 노드 아래에 깔리게 한다.
            RectTransform area = UiBuilder.Rect("Nodes", _window, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(Padding, -(Padding + HeaderHeight)), treeSize);

            foreach (TechTree.Node node in tree.Nodes)
            {
                if (node.Requires == null)
                {
                    continue;
                }

                foreach (string id in node.Requires)
                {
                    TechTree.Node from = tree.Find(id);
                    if (from != null)
                    {
                        _lines.Add(BuildLine(area, from, node));
                    }
                }
            }

            foreach (TechTree.Node node in tree.Nodes)
            {
                _nodes.Add(BuildNode(area, node));
            }

            _selection = UiBuilder.Rect("Selection", area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                _nodeSize + new Vector2(14f, 14f));
            Image selectionImage = UiBuilder.Image(_selection, Color.white, UiBuilder.BorderSprite);
            selectionImage.type = Image.Type.Sliced;
            _selection.gameObject.SetActive(false);

            BuildDetail(Padding, HeaderHeight, DetailWidth, treeSize.y);
            _root.gameObject.SetActive(false);
        }

        private void BuildHeader(float width, float padding)
        {
            RectTransform title = UiBuilder.Rect("Title", _window, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding, -22f),
                new Vector2(300f, 50f));
            Text titleText = UiBuilder.Text("강화 단말기", title, 34, TextAnchor.MiddleLeft, UiTheme.Accent);
            titleText.fontStyle = FontStyle.Bold;

            RectTransform level = UiBuilder.Rect("Level", _window, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding + 320f, -22f),
                new Vector2(150f, 50f));
            _levelText = UiBuilder.Text(string.Empty, level, 30, TextAnchor.MiddleLeft, UiTheme.Text);
            _levelText.fontStyle = FontStyle.Bold;

            _xpBar = UiBuilder.Bar("Xp", _window, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding + 480f, -32f),
                new Vector2(320f, 30f), new Color(0.55f, 0.85f, 1f));
            _xpText = UiBuilder.Text(string.Empty, (RectTransform)_xpBar.transform.parent, 20, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Outline(_xpText);

            RectTransform points = UiBuilder.Rect("Points", _window, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-padding, -22f),
                new Vector2(300f, 50f));
            _pointText = UiBuilder.Text(string.Empty, points, 30, TextAnchor.MiddleRight, UiTheme.Accent);
            _pointText.fontStyle = FontStyle.Bold;

            RectTransform help = UiBuilder.Rect("Help", _window, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-padding - 310f, -22f),
                new Vector2(260f, 50f));
            UiBuilder.Text("E / Esc: 닫기", help, 22, TextAnchor.MiddleRight, UiTheme.TextDim);
        }

        private NodeView BuildNode(RectTransform area, TechTree.Node node)
        {
            RectTransform rect = UiBuilder.Rect(node.Id, area, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(node.Column * _nodePitch.x, -node.Row * _nodePitch.y), _nodeSize);
            Image fill = UiBuilder.Panel(rect, LockedFill, LineColor);

            RectTransform nameRect = UiBuilder.Rect("Name", rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f),
                new Vector2(_nodeSize.x - 8f, _nodeSize.y * 0.5f));
            Text nodeName = UiBuilder.Text(node.DisplayName, nameRect, 22, TextAnchor.MiddleCenter, UiTheme.Text);
            nodeName.fontStyle = FontStyle.Bold;

            RectTransform effectRect = UiBuilder.Rect("Effect", rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f),
                new Vector2(_nodeSize.x - 8f, _nodeSize.y * 0.45f));
            Text effect = UiBuilder.Text(TechTree.Describe(node.Stat, node.Amount), effectRect, 18, TextAnchor.MiddleCenter, UiTheme.Accent);

            return new NodeView
            {
                Node = node,
                Rect = rect,
                Fill = fill,
                Border = rect.Find("Border").GetComponent<Image>(),
                Name = nodeName,
                Effect = effect,
            };
        }

        // 앞 노드의 오른쪽 가운데에서 뒤 노드의 왼쪽 가운데로 긋는 선
        private LineView BuildLine(RectTransform area, TechTree.Node from, TechTree.Node to)
        {
            var start = new Vector2(from.Column * _nodePitch.x + _nodeSize.x, -from.Row * _nodePitch.y - _nodeSize.y * 0.5f);
            var end = new Vector2(to.Column * _nodePitch.x, -to.Row * _nodePitch.y - _nodeSize.y * 0.5f);
            Vector2 offset = end - start;

            RectTransform rect = UiBuilder.Rect("Line", area, new Vector2(0f, 1f), new Vector2(0f, 0.5f), start,
                new Vector2(offset.magnitude, 5f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
            Image image = UiBuilder.Image(rect, LineColor, UiBuilder.WhiteSprite);
            return new LineView { Image = image, From = from, To = to };
        }

        // 오른쪽: 고른 노드의 효과와 필요한 것, 강화 버튼, 가진 재료
        private void BuildDetail(float padding, float headerHeight, float width, float height)
        {
            RectTransform panel = UiBuilder.Rect("Detail", _window, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-padding, -(padding + headerHeight)), new Vector2(width, height));
            UiBuilder.Panel(panel, UiTheme.SlotFill, UiTheme.SlotBorder);

            RectTransform nameRect = UiBuilder.Rect("Name", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f),
                new Vector2(width - 40f, 48f));
            _detailName = UiBuilder.Text(string.Empty, nameRect, 32, TextAnchor.MiddleLeft, UiTheme.Accent);
            _detailName.fontStyle = FontStyle.Bold;

            RectTransform effectRect = UiBuilder.Rect("Effect", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f),
                new Vector2(width - 40f, 40f));
            _detailEffect = UiBuilder.Text(string.Empty, effectRect, 26, TextAnchor.MiddleLeft, UiTheme.Text);

            RectTransform bodyRect = UiBuilder.Rect("Body", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f),
                new Vector2(width - 40f, 260f));
            _detailBody = UiBuilder.Text(string.Empty, bodyRect, 22, TextAnchor.UpperLeft, UiTheme.Text);
            _detailBody.lineSpacing = 1.25f;

            _button = UiBuilder.Button("강화", panel, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(width - 80f, 70f), true);
            _buttonFill = _button.GetComponent<Image>();
            _buttonText = _button.GetComponentInChildren<Text>();

            RectTransform materials = UiBuilder.Rect("Materials", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f),
                new Vector2(width - 40f, 150f));
            _materialText = UiBuilder.Text(string.Empty, materials, 21, TextAnchor.UpperLeft, UiTheme.TextDim);
            _materialText.lineSpacing = 1.2f;
        }
    }
}
