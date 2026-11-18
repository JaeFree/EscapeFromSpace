using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 레이드와 대기실에서 항상 보이는 화면 (기획서 9.4): 왼쪽 아래 원 게이지, 가운데 아래 무기칸과 도구칸, 도구칸 위 진행 바, 포드 위 탈출 안내.
    public class HudUI : MonoBehaviour
    {
        private static readonly Color BackColor = UiTheme.SlotFill;
        private static readonly Color HurtColor = new Color(0.95f, 0.08f, 0.1f);
        private static readonly Color ChokeColor = new Color(0.2f, 0.45f, 1f);
        private static readonly Color HpColor = new Color(0.9f, 0.25f, 0.25f);
        private static readonly Color OxygenColor = new Color(0.3f, 0.7f, 1f);
        private static readonly Color WeightColor = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color WarningColor = new Color(1f, 0.15f, 0.15f);
        private static readonly Color ReloadBarColor = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color RollBarColor = new Color(0.45f, 0.75f, 1f);
        private static readonly Color ItemBarColor = new Color(0.4f, 0.9f, 0.5f);
        private static readonly Color EscapeBarColor = new Color(0.3f, 0.95f, 0.5f);

        [SerializeField] private PlayerData _playerData;
        [SerializeField] private Health _health;
        [SerializeField] private PlayerOxygen _oxygen;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerShooter _shooter;
        [SerializeField] private PlayerItemUser _itemUser;
        [SerializeField] private PlayerRoll _roll;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private RaidManager _raid;
        [SerializeField] private Sprite _circleSprite;

        // 크기는 1920×1080 화면 기준의 픽셀이다. Play를 다시 시작하면 반영된다.
        [Header("크기")]
        [SerializeField, Tooltip("원 게이지의 지름")] private float _gaugeSize = 130f;
        [SerializeField, Tooltip("무기칸과 도구칸 한 변")] private float _slotSize = 100f;
        [SerializeField] private int _fontSize = 20;

        // 화면 가장자리가 물드는 효과. 맞으면 붉게 번쩍이고, HP가 낮으면 붉게, 산소가 모자라면 푸르게 남는다.
        [Header("화면 가장자리 효과")]
        [SerializeField, Range(0f, 1f), Tooltip("맞은 순간의 진하기")] private float _hitFlashAlpha = 0.3f;
        [SerializeField, Tooltip("초. 맞은 뒤 사라지는 시간")] private float _hitFlashDuration = 0.45f;
        [SerializeField, Range(0f, 1f), Tooltip("HP가 이 비율 아래면 가장자리가 붉게 남는다")] private float _lowHpRatio = 0.3f;
        [SerializeField, Range(0f, 1f), Tooltip("HP나 산소가 모자랄 때의 진하기")] private float _warningAlpha = 0.18f;

        private class Gauge
        {
            public Image Fill;
            public Text Value;
        }

        private Canvas _canvas;
        private Camera _camera;
        private RectTransform _gaugeRoot;
        private Gauge _hpGauge;
        private Gauge _oxygenGauge;
        private Gauge _weightGauge;
        private Image _weaponImage;
        private Image _weaponIcon;
        private readonly Image[] _toolIcons = new Image[PlayerItemUser.SlotCount];
        private Text _weaponName;
        private Text _ammoText;
        private readonly Image[] _toolImages = new Image[PlayerItemUser.SlotCount];
        private readonly Text[] _toolNames = new Text[PlayerItemUser.SlotCount];
        private readonly Text[] _toolCounts = new Text[PlayerItemUser.SlotCount];

        // 재장전과 구르기 쿨타임은 같은 바를 쓰고, 아이템 사용은 그 위의 바를 따로 쓴다 (2026-11-16 사용자 결정).
        private Image _actionBar;
        private Text _actionBarLabel;
        private Image _itemBar;
        private Text _itemBarLabel;
        private RectTransform _podHint;
        private Image _escapeBar;
        private RectTransform _actionRoot;
        private InventoryUI _inventoryUi;
        private Image _vignette;
        private float _hitFlash;

        // 게이지 위의 레벨과 경험치 바 (기획서 12장)
        private Text _levelText;
        private Image _xpBar;
        private Text _pointText;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _camera = Camera.main;
            _inventoryUi = GetComponent<InventoryUI>();
            Build();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
        }

        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            _hitFlash = 1f;
        }

        // 가장 급한 것 하나만 보여 준다: 방금 맞음 > HP 부족 > 산소 부족.
        private void UpdateVignette()
        {
            if (_hitFlashDuration > 0f)
            {
                _hitFlash = Mathf.Max(0f, _hitFlash - Time.unscaledDeltaTime / _hitFlashDuration);
            }

            float throb = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
            bool lowHp = !_health.IsDead && _health.MaxHp > 0f && _health.CurrentHp / _health.MaxHp < _lowHpRatio;
            float hurt = Mathf.Max(_hitFlash * _hitFlashAlpha, lowHp ? _warningAlpha * throb : 0f);
            float choke = _oxygen.IsDraining && _oxygen.IsLow ? _warningAlpha * throb : 0f;

            Color color = hurt >= choke ? HurtColor : ChokeColor;
            color.a = Mathf.Max(hurt, choke);
            _vignette.color = color;
            _vignette.enabled = color.a > 0.005f;
        }

        private void Update()
        {
            UpdateVignette();

            // 가방 화면이 왼쪽을 덮으므로 열려 있는 동안에는 게이지를 그 오른쪽으로 옮긴다.
            float panelWidth = _inventoryUi != null ? _inventoryUi.LeftPanelWidth : 0f;

            // 대기실 창고 칸은 화면 아래 가운데까지 덮는다. 열려 있는 동안에는 무기칸과 도구칸을 숨긴다.
            bool showActions = _inventoryUi == null || !_inventoryUi.IsLootDocked;
            _actionRoot.gameObject.SetActive(showActions);

            // 옮겨 간 게이지가 가운데의 무기칸·도구칸과 겹치지 않게, 그 칸들이 보이는 동안에는 그 위로 올린다.
            float bottom = panelWidth > 0f && showActions ? 30f + _slotSize + 70f : 30f;
            _gaugeRoot.anchoredPosition = new Vector2(panelWidth > 0f ? panelWidth + 24f : 30f, bottom);

            UpdateGauges();
            UpdateLevel();
            UpdateWeapon();
            UpdateTools();
            UpdateBars();
            UpdatePodHint();
        }

        private void UpdateGauges()
        {
            _hpGauge.Fill.fillAmount = _health.MaxHp > 0f ? _health.CurrentHp / _health.MaxHp : 0f;
            _hpGauge.Value.text = Mathf.CeilToInt(_health.CurrentHp).ToString();

            _oxygenGauge.Fill.fillAmount = _oxygen.Max > 0f ? _oxygen.Current / _oxygen.Max : 0f;
            int seconds = Mathf.CeilToInt(_oxygen.Current);
            _oxygenGauge.Value.text = $"{seconds / 60}:{seconds % 60:00}";

            // 남은 1분부터 빨갛게 깜빡인다 (기획서 7장).
            bool blink = _oxygen.IsLow && Mathf.Repeat(Time.unscaledTime, 0.6f) < 0.3f;
            _oxygenGauge.Fill.color = blink ? WarningColor : OxygenColor;

            float weight = _inventory.TotalWeight;
            float ratio = weight / _controller.MaxWeight;
            _weightGauge.Fill.fillAmount = ratio;
            _weightGauge.Value.text = $"{weight:0.0}\n/ {_controller.MaxWeight:0}kg";
            _weightGauge.Fill.color = ratio > _playerData.OverloadRatio ? WarningColor : WeightColor;
        }

        private void UpdateLevel()
        {
            PlayerProgress progress = PlayerProgress.Current;
            if (progress == null)
            {
                return;
            }

            _levelText.text = $"Lv. {progress.Level}";
            _xpBar.fillAmount = progress.IsMaxLevel ? 1f : progress.Xp / (float)progress.XpToNext;

            // 쓰지 않은 강화 포인트가 있으면 알려 준다.
            _pointText.text = progress.Points > 0 ? $"강화 포인트 {progress.Points}" : string.Empty;
        }

        private void UpdateWeapon()
        {
            ItemStack weapon = _inventory.GetEquipped(EquipSlot.Weapon);
            if (weapon == null)
            {
                _weaponImage.color = BackColor;
                UiBuilder.SetIcon(_weaponIcon, null, Color.white);
                _weaponName.text = "맨손";
                _weaponName.color = UiTheme.TextDim;
                _ammoText.text = string.Empty;
                return;
            }

            _weaponImage.color = weapon.Data.Color;
            UiBuilder.SetIcon(_weaponIcon, weapon.Data.Icon, Color.white);
            _weaponName.text = weapon.Data.DisplayName;
            _weaponName.color = Color.black;

            if (weapon.Data.Weapon.MagazineSize <= 0)
            {
                _ammoText.text = string.Empty;
            }
            else
            {
                // 고출력 탄약이 든 탄창은 탄 수를 주황으로 보여 준다 (기획서 16장).
                string magazine = _shooter.LoadedAmmo != null ? $"<color=#FFA028>{_shooter.AmmoInMagazine}</color>" : _shooter.AmmoInMagazine.ToString();
                _ammoText.text = $"{magazine} / {_shooter.ReserveAmmo}";
            }
        }

        private void UpdateTools()
        {
            for (int i = 0; i < PlayerItemUser.SlotCount; i++)
            {
                ItemData item = _itemUser.GetSlot(i);
                int count = item != null ? _itemUser.CountInSlot(i) : 0;
                bool has = count > 0;

                // 가진 것이 없으면 어둡게 해서 비어 있음을 보여 준다.
                _toolImages[i].color = item == null ? BackColor
                    : has ? item.Color : Color.Lerp(item.Color, Color.black, 0.75f);
                // 가진 것이 없으면 그림도 흐리게 한다.
                UiBuilder.SetIcon(_toolIcons[i], item != null ? item.Icon : null, has ? Color.white : new Color(1f, 1f, 1f, 0.3f));
                _toolNames[i].text = item != null ? item.DisplayName : string.Empty;
                _toolNames[i].color = has ? Color.black : UiTheme.TextDim;
                _toolCounts[i].text = item != null ? count.ToString() : string.Empty;
                _toolCounts[i].color = has ? Color.black : UiTheme.TextDim;
            }
        }

        // 진행 중인 동작을 도구칸 위에 바로 보여 준다. 하는 동안에만 보인다.
        private void UpdateBars()
        {
            // 재장전과 구르기 쿨타임이 겹치면 지금 하고 있는 동작인 재장전을 먼저 보여 준다.
            float cooldown = _roll.CooldownRemaining;
            if (_shooter.IsReloading)
            {
                ShowBar(_actionBar, _actionBarLabel, ReloadBarColor, _shooter.ReloadProgress, $"재장전 {_shooter.ReloadTimeLeft:0.0}");
            }
            else if (cooldown > 0f && _playerData.RollCooldown > 0f)
            {
                ShowBar(_actionBar, _actionBarLabel, RollBarColor, 1f - cooldown / _playerData.RollCooldown, $"구르기 {cooldown:0.0}");
            }
            else
            {
                HideBar(_actionBar);
            }

            ItemData item = _itemUser.Using;
            if (item != null)
            {
                ShowBar(_itemBar, _itemBarLabel, ItemBarColor, _itemUser.UseProgress, $"{item.DisplayName} {_itemUser.UseTimeLeft:0.0}");
            }
            else
            {
                HideBar(_itemBar);
            }
        }

        private static void ShowBar(Image fill, Text label, Color color, float progress, string text)
        {
            fill.transform.parent.gameObject.SetActive(true);
            fill.color = color;
            fill.fillAmount = progress;
            label.text = text;
        }

        private static void HideBar(Image fill)
        {
            fill.transform.parent.gameObject.SetActive(false);
        }

        // 열린 포드 안에 서 있으면 포드 위에 탈출 키와 누르고 있는 진행도를 보여 준다.
        private void UpdatePodHint()
        {
            EscapePod pod = _raid != null ? _raid.ReachablePod : null;
            _podHint.gameObject.SetActive(pod != null);
            if (pod == null)
            {
                return;
            }

            UiBuilder.MoveToScreenPoint(_canvas, _podHint, _camera.WorldToScreenPoint(pod.transform.position + Vector3.up * 1.6f));
            _escapeBar.fillAmount = _raid.EscapeProgress;
        }

        // ---------- 그리기 ----------

        private void Build()
        {
            // 화면 가장자리 효과는 다른 화면 요소를 가리지 않게 맨 뒤에 둔다.
            var vignetteRect = new GameObject("Vignette", typeof(RectTransform)).GetComponent<RectTransform>();
            vignetteRect.SetParent(transform, false);
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.offsetMin = Vector2.zero;
            vignetteRect.offsetMax = Vector2.zero;
            vignetteRect.SetAsFirstSibling();
            _vignette = UiBuilder.Image(vignetteRect, Color.clear, UiBuilder.VignetteSprite);
            _vignette.enabled = false;

            // 왼쪽 아래: HP, 산소, 무게 원 게이지
            _gaugeRoot = UiBuilder.Rect("Gauges", transform, Vector2.zero, Vector2.zero, new Vector2(30f, 30f), Vector2.zero);
            _hpGauge = CreateGauge("HP", 0, HpColor);
            _oxygenGauge = CreateGauge("산소", 1, OxygenColor);
            _weightGauge = CreateGauge("무게", 2, WeightColor);

            // 게이지 이름 위: 레벨, 경험치 바, 남은 강화 포인트
            float levelY = _gaugeSize + _fontSize + 26f;
            RectTransform level = UiBuilder.Rect("Level", _gaugeRoot, Vector2.zero, Vector2.zero, new Vector2(0f, levelY), new Vector2(86f, 30f));
            _levelText = UiBuilder.Text(string.Empty, level, _fontSize + 2, TextAnchor.MiddleLeft, UiTheme.Accent);
            _levelText.fontStyle = FontStyle.Bold;
            UiBuilder.Outline(_levelText);

            _xpBar = UiBuilder.Bar("Xp", _gaugeRoot, Vector2.zero, Vector2.zero, new Vector2(90f, levelY + 6f), new Vector2(170f, 18f),
                new Color(0.55f, 0.85f, 1f));

            RectTransform points = UiBuilder.Rect("Points", _gaugeRoot, Vector2.zero, Vector2.zero, new Vector2(270f, levelY),
                new Vector2(180f, 30f));
            _pointText = UiBuilder.Text(string.Empty, points, _fontSize, TextAnchor.MiddleLeft, UiTheme.Accent);
            UiBuilder.Outline(_pointText);

            // 가운데 아래: 무기칸 1개와 도구칸 3개
            float gap = 10f;
            float width = _slotSize * 4f + gap * 3f + 24f;
            RectTransform bar = UiBuilder.Rect("ActionBar", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 30f), new Vector2(width, _slotSize));
            _actionRoot = bar;

            RectTransform weapon = UiBuilder.Rect("WeaponSlot", bar, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(_slotSize, _slotSize));
            _weaponImage = UiBuilder.Panel(weapon, BackColor, UiTheme.PanelBorder);
            _weaponIcon = UiBuilder.Icon(weapon, _slotSize * 0.62f);
            _weaponName = UiBuilder.Text(string.Empty, weapon, _fontSize, TextAnchor.UpperLeft, Color.black);

            // 탄창 / 예비탄은 무기칸 위에 작게 (기획서 9.4)
            RectTransform ammo = UiBuilder.Rect("Ammo", weapon, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 6f),
                new Vector2(_slotSize + 60f, _fontSize + 12f));
            _ammoText = UiBuilder.Text(string.Empty, ammo, _fontSize + 2, TextAnchor.MiddleCenter, UiTheme.Text);
            _ammoText.fontStyle = FontStyle.Bold;
            UiBuilder.Outline(_ammoText);

            for (int i = 0; i < PlayerItemUser.SlotCount; i++)
            {
                float x = _slotSize + 24f + gap + i * (_slotSize + gap);
                RectTransform slot = UiBuilder.Rect($"ToolSlot {i + 1}", bar, Vector2.zero, Vector2.zero, new Vector2(x, 0f),
                    new Vector2(_slotSize, _slotSize));
                _toolImages[i] = UiBuilder.Panel(slot, BackColor, UiTheme.SlotBorder);
                _toolIcons[i] = UiBuilder.Icon(slot, _slotSize * 0.55f);

                _toolNames[i] = UiBuilder.Text(string.Empty, slot, _fontSize - 4, TextAnchor.LowerLeft, Color.black);
                _toolCounts[i] = UiBuilder.Text(string.Empty, slot, _fontSize + 2, TextAnchor.LowerRight, Color.black);
                _toolCounts[i].fontStyle = FontStyle.Bold;

                // 누르는 키
                Text slotKey = UiBuilder.Text((i + 1).ToString(), slot, _fontSize, TextAnchor.UpperLeft, UiTheme.Accent);
                slotKey.fontStyle = FontStyle.Bold;
                UiBuilder.Outline(slotKey);
            }

            // 도구칸 위의 진행 바 두 줄: 아래는 재장전·구르기 쿨타임, 위는 아이템 사용
            var barSize = new Vector2(_slotSize * 3f + gap * 2f, _fontSize + 10f);
            _actionBar = UiBuilder.Bar("ActionProgress", bar, new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 8f), barSize,
                ReloadBarColor);
            _actionBarLabel = CreateBarLabel(_actionBar);
            _itemBar = UiBuilder.Bar("ItemProgress", bar, new Vector2(1f, 1f), new Vector2(1f, 0f),
                new Vector2(0f, 8f + barSize.y + 6f), barSize, ItemBarColor);
            _itemBarLabel = CreateBarLabel(_itemBar);
            HideBar(_actionBar);
            HideBar(_itemBar);

            // 탈출 안내: 포드 위에 키와 그 아래 진행 바
            _podHint = UiBuilder.Rect("PodHint", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(64f, 64f));
            UiBuilder.Panel(_podHint, UiTheme.PanelFill, UiTheme.PanelBorder);
            Text key = UiBuilder.Text("X", _podHint, 36, TextAnchor.MiddleCenter, UiTheme.Text);
            key.fontStyle = FontStyle.Bold;
            _escapeBar = UiBuilder.Bar("EscapeProgress", _podHint, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -6f),
                new Vector2(160f, 20f), EscapeBarColor);
            _podHint.gameObject.SetActive(false);
        }

        // 바 위에 겹쳐 쓰는 글자. 채워진 색 위에서도 읽히게 테두리를 준다.
        private Text CreateBarLabel(Image fill)
        {
            Text label = UiBuilder.Text(string.Empty, (RectTransform)fill.transform.parent, _fontSize - 2, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Outline(label);
            return label;
        }

        // 아래에서부터 차오르는 원. 값은 원 안에, 이름은 원 위에 쓴다.
        private Gauge CreateGauge(string label, int index, Color color)
        {
            float x = index * (_gaugeSize + 18f);
            var size = new Vector2(_gaugeSize, _gaugeSize);

            RectTransform back = UiBuilder.Rect(label, _gaugeRoot, Vector2.zero, Vector2.zero, new Vector2(x, 0f), size);
            UiBuilder.Image(back, BackColor, _circleSprite);

            RectTransform fillRect = UiBuilder.Rect("Fill", back, Vector2.zero, Vector2.zero, Vector2.zero, size);
            Image fill = UiBuilder.Image(fillRect, color, _circleSprite);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;

            // 게이지가 맵 위에서도 또렷하게 틀의 색으로 고리를 두른다.
            RectTransform ring = UiBuilder.Rect("Ring", back, Vector2.zero, Vector2.zero, Vector2.zero, size);
            UiBuilder.Image(ring, UiTheme.PanelBorder, UiBuilder.RingSprite);

            Text value = UiBuilder.Text(string.Empty, back, _fontSize + 4, TextAnchor.MiddleCenter, Color.white);
            value.fontStyle = FontStyle.Bold;
            UiBuilder.Outline(value);

            RectTransform labelRect = UiBuilder.Rect("Label", back, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 4f),
                new Vector2(_gaugeSize, _fontSize + 10f));
            Text labelText = UiBuilder.Text(label, labelRect, _fontSize, TextAnchor.MiddleCenter, UiTheme.Text);
            labelText.fontStyle = FontStyle.Bold;
            UiBuilder.Outline(labelText);

            return new Gauge { Fill = fill, Value = value };
        }
    }
}
