using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 대기실 상인의 이름표, E 안내, 대화 창 (기획서 14.2, 16장). 창은 목록 + 버튼 방식이다 (8장).
    // 다른 화면처럼 마우스 판정은 직접 계산한다.
    public class MerchantUI : MonoBehaviour
    {
        public enum Tab
        {
            Shop,
            Craft,
            Sell,
            Quest,
            Repair,
        }

        private const int VisibleRows = 7;
        private const string OkColor = "#7CF08F";
        private const string LackColor = "#FF7A6B";

        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private LobbyManager _lobby;
        [SerializeField, Tooltip("칸. 상인에게서 이 거리 안에 있어야 말을 걸 수 있다")] private float _range = 1.6f;
        [SerializeField, Tooltip("칸. 이름표를 상인보다 이만큼 위에 띄운다")] private float _labelOffset = 0.75f;
        [SerializeField] private float _rowHeight = 78f;

        // 퀘스트 탭과 해금 이름은 퀘스트 화면이 채운다 (기획서 14장). 없으면 퀘스트 이름 대신 번호를 쓴다.
        public static Func<string, string> QuestTitle = id => id;

        public class Row
        {
            public Sprite Icon;
            public Color IconColor = Color.white;
            public string Title;
            public string Detail;
            public string Price;
            public string Button;
            public bool Enabled;
            public Action OnClick;
        }

        private class RowView
        {
            public RectTransform Rect;
            public Image Icon;
            public Image IconBack;
            public Text Title;
            public Text Detail;
            public Text Price;
            public RectTransform Button;
            public Image ButtonFill;
            public Text ButtonText;
        }

        private readonly List<Npc> _npcs = new List<Npc>();
        private readonly List<RectTransform> _labels = new List<RectTransform>();
        private readonly List<Row> _rows = new List<Row>();
        private readonly List<RowView> _rowViews = new List<RowView>();
        private readonly List<(Tab tab, RectTransform rect, Image fill)> _tabs = new List<(Tab, RectTransform, Image)>();
        private Canvas _canvas;
        private Camera _camera;
        private LobbyStock _stock;
        private RectTransform _root;
        private RectTransform _hint;
        private Text _name;
        private Text _greeting;
        private Text _credits;
        private Text _message;
        private Text _empty;
        private RectTransform _list;
        private Npc _open;
        private Npc _near;
        private Tab _tab;
        private int _scroll;
        private bool _dirty;

        public bool IsOpen => _open != null;
        public Npc OpenNpc => _open;
        public LobbyStock Stock => _stock;
        public PlayerInventory Inventory => _inventory;

        // 퀘스트 탭의 줄을 채우는 쪽 (QuestUI). 없으면 퀘스트 탭을 보이지 않는다.
        public Action<Npc, List<Row>> QuestRows;
        public Func<Npc, bool> HasQuests;

        // 퀘스트 탭에서 왼쪽 인사말 자리에 보여 줄 이야기. null을 돌려주면 상인의 인사말을 쓴다.
        public Func<Npc, string> QuestGreeting;

        // 장거리 탈출 포드의 수리 탭 (기획서 14.5)
        public Action<Npc, List<Row>> RepairRows;

        private static SaveData Data => GameSession.Data;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _camera = Camera.main;
        }

        private void Start()
        {
            _npcs.AddRange(FindObjectsByType<Npc>(FindObjectsSortMode.None));
            _npcs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            _stock = new LobbyStock(_lobby.Storage.Grid, _inventory);
            Build();
            _inventory.Changed += MarkDirty;
        }

        private void OnDestroy()
        {
            if (_open != null)
            {
                _controller.IsMovementLocked = false;
            }

            _inventory.Changed -= MarkDirty;
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        private void Update()
        {
            if (StoryScreenUI.IsShowing)
            {
                _hint.gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < _npcs.Count; i++)
            {
                UiBuilder.MoveToScreenPoint(_canvas, _labels[i], _camera.WorldToScreenPoint(_npcs[i].transform.position + Vector3.up * _labelOffset));
            }

            Keyboard keyboard = Keyboard.current;
            bool free = !_controller.IsDead && !_controller.IsBagOpen && !_lobby.IsConfirming && !SettingsUI.IsOpen;

            if (_open == null)
            {
                _near = free ? FindNear() : null;
                _hint.gameObject.SetActive(_near != null);
                if (_near != null)
                {
                    UiBuilder.MoveToScreenPoint(_canvas, _hint,
                        _camera.WorldToScreenPoint(_near.transform.position + Vector3.up * (_labelOffset + 0.55f)));
                    if (_input.InteractPressedThisFrame)
                    {
                        Open(_near);
                    }
                }

                return;
            }

            bool near = Vector2.Distance(_controller.transform.position, _open.transform.position) <= _range + 0.3f;
            if (!near || _controller.IsBagOpen || _input.InteractPressedThisFrame
                || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                Close();
                return;
            }

            HandleInput();
            if (_dirty)
            {
                Refresh();
            }
        }

        private Npc FindNear()
        {
            Npc best = null;
            float bestDistance = _range;
            foreach (Npc npc in _npcs)
            {
                float distance = Vector2.Distance(_controller.transform.position, npc.transform.position);
                if (distance <= bestDistance)
                {
                    best = npc;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void Open(Npc npc)
        {
            _open = npc;
            _controller.IsMovementLocked = true;
            _hint.gameObject.SetActive(false);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _message.text = string.Empty;

            MerchantData merchant = npc.Merchant;
            _name.text = $"{merchant.DisplayName}  <size=26><color=#B9AFC9>{merchant.Title}</color></size>";
            _greeting.text = merchant.Greeting;
            _tab = FirstTab();
            _scroll = 0;
            Refresh();
            Sfx.Play(SfxId.UiPrompt);
        }

        public void CloseWindow()
        {
            if (_open != null)
            {
                Close();
            }
        }

        private void Close()
        {
            _open = null;
            _controller.IsMovementLocked = false;
            _root.gameObject.SetActive(false);
            Sfx.Play(SfxId.UiCancel);
        }

        private bool HasTab(Tab tab)
        {
            MerchantData merchant = _open.Merchant;
            switch (tab)
            {
                case Tab.Shop:
                    return merchant.Offers.Count > 0;
                case Tab.Craft:
                    return merchant.Recipes.Count > 0;
                case Tab.Sell:
                    return merchant.BuysItems;
                case Tab.Quest:
                    return QuestRows != null && (HasQuests == null || HasQuests(_open));
                case Tab.Repair:
                    return RepairRows != null && merchant.Id == "LongPod";
                default:
                    return false;
            }
        }

        private Tab FirstTab()
        {
            foreach (Tab tab in new[] { Tab.Repair, Tab.Quest, Tab.Shop, Tab.Craft, Tab.Sell })
            {
                if (HasTab(tab))
                {
                    return tab;
                }
            }

            return Tab.Shop;
        }

        public void ShowMessage(string text)
        {
            _message.text = text;
        }

        private void HandleInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 point = mouse.position.ReadValue();
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f && Contains(_list, point))
            {
                int max = Mathf.Max(0, _rows.Count - VisibleRows);
                int next = Mathf.Clamp(_scroll - Math.Sign(wheel), 0, max);
                if (next != _scroll)
                {
                    _scroll = next;
                    DrawRows();
                }
            }

            if (!mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            foreach ((Tab tab, RectTransform rect, Image _) in _tabs)
            {
                if (rect.gameObject.activeSelf && Contains(rect, point) && tab != _tab)
                {
                    _tab = tab;
                    _scroll = 0;
                    _message.text = string.Empty;
                    Sfx.Play(SfxId.ItemPick);
                    Refresh();
                    return;
                }
            }

            for (int i = 0; i < _rowViews.Count; i++)
            {
                int index = _scroll + i;
                if (index >= _rows.Count || !Contains(_rowViews[i].Button, point))
                {
                    continue;
                }

                Row row = _rows[index];
                if (row.Enabled && row.OnClick != null)
                {
                    row.OnClick();
                    _inventory.NotifyChanged();
                    Refresh();
                }
                else
                {
                    Sfx.Play(SfxId.ItemRefuse);
                }

                return;
            }
        }

        private bool Contains(RectTransform rect, Vector2 point)
        {
            return rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, point, UiBuilder.CameraOf(_canvas));
        }

        // ---------- 줄 만들기 ----------

        private void Refresh()
        {
            _dirty = false;
            if (_open == null)
            {
                return;
            }

            _credits.text = $"크레딧  {Data.Credits:N0}";
            // 이 상인에게 있는 탭만 왼쪽부터 붙여 놓는다.
            int shown = 0;
            foreach ((Tab tab, RectTransform rect, Image fill) in _tabs)
            {
                bool has = HasTab(tab);
                rect.gameObject.SetActive(has);
                fill.color = tab == _tab ? UiTheme.ButtonFill : UiTheme.ButtonQuietFill;
                if (has)
                {
                    rect.anchoredPosition = new Vector2(440f + shown * 140f, -30f);
                    shown++;
                }
            }

            _rows.Clear();
            switch (_tab)
            {
                case Tab.Shop:
                    BuildShopRows();
                    break;
                case Tab.Craft:
                    BuildCraftRows();
                    break;
                case Tab.Sell:
                    BuildSellRows();
                    break;
                case Tab.Quest:
                    QuestRows?.Invoke(_open, _rows);
                    break;
                case Tab.Repair:
                    RepairRows?.Invoke(_open, _rows);
                    break;
            }

            string story = _tab == Tab.Quest && QuestGreeting != null ? QuestGreeting(_open) : null;
            _greeting.text = story ?? _open.Merchant.Greeting;

            _scroll = Mathf.Clamp(_scroll, 0, Mathf.Max(0, _rows.Count - VisibleRows));
            DrawRows();
        }

        public static bool IsQuestDone(string questId)
        {
            return string.IsNullOrEmpty(questId) || (Data != null && Data.CompletedQuests.Contains(questId));
        }

        private void BuildShopRows()
        {
            foreach (MerchantData.Offer offer in _open.Merchant.Offers)
            {
                if (offer.Item == null)
                {
                    continue;
                }

                MerchantData.Offer captured = offer;
                bool unlocked = IsQuestDone(offer.UnlockQuest);
                bool afford = Data.Credits >= offer.Price;
                string count = offer.Count > 1 ? $" ×{offer.Count}" : string.Empty;
                _rows.Add(new Row
                {
                    Icon = offer.Item.Icon,
                    IconColor = offer.Item.Icon != null ? Color.white : offer.Item.Color,
                    Title = offer.Item.DisplayName + count,
                    Detail = unlocked ? Describe(offer.Item) : $"<color={LackColor}>잠김</color> · 퀘스트 \"{QuestTitle(offer.UnlockQuest)}\" 완료 필요",
                    Price = $"<color={(afford ? OkColor : LackColor)}>{offer.Price:N0}</color>",
                    Button = unlocked ? "구매" : "잠김",
                    Enabled = unlocked && afford,
                    OnClick = () => Buy(captured),
                });
            }
        }

        private void Buy(MerchantData.Offer offer)
        {
            if (!_stock.AddToStorage(offer.Item, offer.Count))
            {
                Sfx.Play(SfxId.ItemRefuse);
                ShowMessage($"<color={LackColor}>창고에 자리가 없습니다.</color>");
                return;
            }

            Data.Credits -= offer.Price;
            Sfx.Play(SfxId.UiConfirm);
            ShowMessage($"{offer.Item.DisplayName}을(를) 샀습니다. 창고에 넣었습니다.");
        }

        private void BuildCraftRows()
        {
            foreach (MerchantData.Recipe recipe in _open.Merchant.Recipes)
            {
                if (recipe.Output == null)
                {
                    continue;
                }

                MerchantData.Recipe captured = recipe;
                bool unlocked = IsQuestDone(recipe.UnlockQuest);
                bool has = _stock.Has(recipe.Inputs);
                var needs = new System.Text.StringBuilder();
                foreach (TechTree.Cost cost in recipe.Inputs)
                {
                    if (cost.Item == null)
                    {
                        continue;
                    }

                    int owned = _stock.Count(cost.Item);
                    if (needs.Length > 0)
                    {
                        needs.Append("  ");
                    }

                    needs.Append($"{cost.Item.DisplayName} <color={(owned >= cost.Count ? OkColor : LackColor)}>{owned}/{cost.Count}</color>");
                }

                string count = recipe.Count > 1 ? $" ×{recipe.Count}" : string.Empty;
                _rows.Add(new Row
                {
                    Icon = recipe.Output.Icon,
                    IconColor = recipe.Output.Icon != null ? Color.white : recipe.Output.Color,
                    Title = recipe.Output.DisplayName + count,
                    Detail = unlocked ? needs.ToString() : $"<color={LackColor}>잠김</color> · 퀘스트 \"{QuestTitle(recipe.UnlockQuest)}\" 완료 필요",
                    Price = string.Empty,
                    Button = unlocked ? "제작" : "잠김",
                    Enabled = unlocked && has,
                    OnClick = () => Craft(captured),
                });
            }
        }

        private void Craft(MerchantData.Recipe recipe)
        {
            // 재료를 먼저 빼면 그 자리에 결과물이 들어갈 수 있다. 그래도 자리가 없으면 재료를 돌려준다.
            _stock.Consume(recipe.Inputs);
            if (!_stock.AddToStorage(recipe.Output, recipe.Count))
            {
                foreach (TechTree.Cost cost in recipe.Inputs)
                {
                    if (cost.Item != null)
                    {
                        _stock.AddToStorage(cost.Item, cost.Count);
                    }
                }

                Sfx.Play(SfxId.ItemRefuse);
                ShowMessage($"<color={LackColor}>창고에 자리가 없습니다.</color>");
                return;
            }

            Sfx.Play(SfxId.LevelUp);
            ShowMessage($"{recipe.Output.DisplayName}을(를) 만들었습니다. 창고에 넣었습니다.");
        }

        private void BuildSellRows()
        {
            AddSellRows(_lobby.Storage.Grid, "창고");
            AddSellRows(_inventory.Bag, "가방");
        }

        private void AddSellRows(InventoryGrid grid, string where)
        {
            foreach (ItemStack stack in grid.Items)
            {
                ItemStack captured = stack;
                int price = _open.Merchant.SellPrice(stack);
                string count = stack.Count > 1 ? $" ×{stack.Count}" : string.Empty;
                _rows.Add(new Row
                {
                    Icon = stack.Data.Icon,
                    IconColor = stack.Data.Icon != null ? Color.white : stack.Data.Color,
                    Title = stack.Data.DisplayName + count,
                    Detail = price > 0 ? where : $"{where} · <color={LackColor}>팔 수 없음</color>",
                    Price = price > 0 ? $"<color={OkColor}>+{price:N0}</color>" : "-",
                    Button = "판매",
                    Enabled = price > 0,
                    OnClick = () => Sell(grid, captured, price),
                });
            }
        }

        private void Sell(InventoryGrid grid, ItemStack stack, int price)
        {
            grid.Remove(stack);
            Data.Credits += price;
            Sfx.Play(SfxId.UiConfirm);
            ShowMessage($"{stack.Data.DisplayName}을(를) {price:N0} 크레딧에 팔았습니다.");
        }

        private static string Describe(ItemData item)
        {
            switch (item.Category)
            {
                case ItemCategory.Weapon:
                    return item.Weapon != null ? $"무기 · 피해 {item.Weapon.Damage}" : "무기";
                case ItemCategory.Vest:
                    return $"방탄복 · 방어력 +{item.Armor} · 가방 +{item.ExtraBagRows * 3}칸";
                case ItemCategory.Helmet:
                    return $"헬멧 · 방어력 +{item.Armor} · 산소 +{item.ExtraOxygenSeconds / 60f:0.#}분";
                case ItemCategory.Ammo:
                    return "탄약";
                case ItemCategory.Medical:
                    return $"의약품 · HP +{item.HealAmount}";
                case ItemCategory.Consumable:
                    return item.OxygenSeconds > 0f ? $"산소 +{item.OxygenSeconds}초" : "소모품";
                case ItemCategory.Attachment:
                    return "총기 부착물";
                default:
                    return $"무게 {item.Weight}kg";
            }
        }

        // ---------- 그리기 ----------

        private void DrawRows()
        {
            _empty.gameObject.SetActive(_rows.Count == 0);
            _empty.text = _tab == Tab.Sell ? "팔 물건이 없습니다." : "아직 없습니다.";
            for (int i = 0; i < _rowViews.Count; i++)
            {
                RowView view = _rowViews[i];
                int index = _scroll + i;
                bool show = index < _rows.Count;
                view.Rect.gameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                Row row = _rows[index];
                UiBuilder.SetIcon(view.Icon, row.Icon, row.IconColor);
                view.Title.text = row.Title;
                view.Detail.text = row.Detail;
                view.Price.text = row.Price;
                view.Button.gameObject.SetActive(!string.IsNullOrEmpty(row.Button));
                view.ButtonText.text = row.Button;
                view.ButtonFill.color = row.Enabled ? UiTheme.ButtonFill : UiTheme.ButtonQuietFill;
                view.ButtonText.color = row.Enabled ? UiTheme.Text : UiTheme.TextDim;
            }
        }

        private void Build()
        {
            foreach (Npc npc in _npcs)
            {
                RectTransform label = UiBuilder.Rect("NpcLabel", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(220f, 40f));
                UiBuilder.Panel(label, UiTheme.PanelFill, UiTheme.SlotBorder);
                string title = npc.Merchant != null ? $"{npc.Merchant.DisplayName} · {npc.Merchant.Title}" : npc.name;
                UiBuilder.Text(title, label, 20, TextAnchor.MiddleCenter, UiTheme.Text);
                _labels.Add(label);
            }

            _hint = UiBuilder.Rect("NpcHint", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(60f, 60f));
            UiBuilder.Panel(_hint, UiTheme.ButtonFill, UiTheme.Accent);
            Text e = UiBuilder.Text("E", _hint, 32, TextAnchor.MiddleCenter, UiTheme.Text);
            e.fontStyle = FontStyle.Bold;
            _hint.gameObject.SetActive(false);

            _root = UiBuilder.Dim("Merchant", transform);
            RectTransform box = UiBuilder.Rect("Box", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 820f));
            UiBuilder.Window(box);

            // 왼쪽: 이름, 인사, 크레딧
            RectTransform name = UiBuilder.Rect("Name", box, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(380f, 60f));
            _name = UiBuilder.Text(string.Empty, name, 40, TextAnchor.MiddleLeft, UiTheme.Accent);
            _name.fontStyle = FontStyle.Bold;
            _name.supportRichText = true;

            RectTransform greeting = UiBuilder.Rect("Greeting", box, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -110f), new Vector2(360f, 520f));
            _greeting = UiBuilder.Text(string.Empty, greeting, 23, TextAnchor.UpperLeft, UiTheme.Text);
            _greeting.lineSpacing = 1.25f;
            _greeting.supportRichText = true;

            RectTransform credits = UiBuilder.Rect("Credits", box, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 36f), new Vector2(360f, 50f));
            UiBuilder.Panel(credits, UiTheme.SlotFill, UiTheme.SlotBorder);
            _credits = UiBuilder.Text(string.Empty, credits, 26, TextAnchor.MiddleCenter, UiTheme.Accent);
            _credits.fontStyle = FontStyle.Bold;

            // 오른쪽: 탭과 목록
            string[] tabNames = { "상점", "제작", "판매", "퀘스트", "수리" };
            Tab[] order = { Tab.Repair, Tab.Quest, Tab.Shop, Tab.Craft, Tab.Sell };
            for (int i = 0; i < order.Length; i++)
            {
                RectTransform tab = UiBuilder.Rect("Tab", box, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(440f + i * 140f, -30f), new Vector2(130f, 56f));
                Image fill = UiBuilder.Panel(tab, UiTheme.ButtonQuietFill, UiTheme.SlotBorder);
                Text text = UiBuilder.Text(tabNames[(int)order[i]], tab, 26, TextAnchor.MiddleCenter, UiTheme.Text);
                text.fontStyle = FontStyle.Bold;
                _tabs.Add((order[i], tab, fill));
            }

            _list = UiBuilder.Rect("List", box, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(440f, -100f),
                new Vector2(844f, _rowHeight * VisibleRows));
            for (int i = 0; i < VisibleRows; i++)
            {
                _rowViews.Add(BuildRow(i));
            }

            RectTransform empty = UiBuilder.Rect("Empty", _list, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 60f));
            _empty = UiBuilder.Text(string.Empty, empty, 26, TextAnchor.MiddleCenter, UiTheme.TextDim);

            RectTransform message = UiBuilder.Rect("Message", box, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(440f, 30f), new Vector2(844f, 60f));
            _message = UiBuilder.Text(string.Empty, message, 23, TextAnchor.MiddleLeft, UiTheme.Text);
            _message.supportRichText = true;

            RectTransform close = UiBuilder.Rect("CloseHint", box, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -36f), new Vector2(160f, 44f));
            UiBuilder.Text("E / Esc 닫기", close, 20, TextAnchor.MiddleRight, UiTheme.TextDim);

            _root.gameObject.SetActive(false);
        }

        private RowView BuildRow(int index)
        {
            var view = new RowView();
            view.Rect = UiBuilder.Rect("Row", _list, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -index * _rowHeight),
                new Vector2(844f, _rowHeight - 8f));
            UiBuilder.Cell(view.Rect, UiTheme.SlotFill);

            RectTransform iconBack = UiBuilder.Rect("IconBack", view.Rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(60f, 60f));
            view.IconBack = UiBuilder.Cell(iconBack, UiTheme.Cell);
            view.Icon = UiBuilder.Icon(iconBack, 52f);

            RectTransform title = UiBuilder.Rect("Title", view.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -6f), new Vector2(480f, 34f));
            view.Title = UiBuilder.Text(string.Empty, title, 25, TextAnchor.MiddleLeft, UiTheme.Text);
            view.Title.fontStyle = FontStyle.Bold;

            RectTransform detail = UiBuilder.Rect("Detail", view.Rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(84f, 6f), new Vector2(560f, 30f));
            view.Detail = UiBuilder.Text(string.Empty, detail, 19, TextAnchor.MiddleLeft, UiTheme.TextDim);
            view.Detail.supportRichText = true;

            RectTransform price = UiBuilder.Rect("Price", view.Rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(130f, 40f));
            view.Price = UiBuilder.Text(string.Empty, price, 24, TextAnchor.MiddleRight, UiTheme.Text);
            view.Price.supportRichText = true;

            view.Button = UiBuilder.Rect("Button", view.Rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(124f, 54f));
            view.ButtonFill = UiBuilder.Panel(view.Button, UiTheme.ButtonFill, UiTheme.Accent);
            view.ButtonText = UiBuilder.Text(string.Empty, view.Button, 24, TextAnchor.MiddleCenter, UiTheme.Text);
            view.ButtonText.fontStyle = FontStyle.Bold;
            return view;
        }
    }
}
