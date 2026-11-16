using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 레이드 중 항상 보이는 화면 (기획서 9.4): 왼쪽 아래 원 게이지, 가운데 아래 무기칸과 도구칸, 포드 위 카운트다운.
    public class HudUI : MonoBehaviour
    {
        private static readonly Color BackColor = new Color(0.08f, 0.09f, 0.12f, 0.85f);
        private static readonly Color HpColor = new Color(0.9f, 0.25f, 0.25f);
        private static readonly Color OxygenColor = new Color(0.3f, 0.7f, 1f);
        private static readonly Color WeightColor = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color WarningColor = new Color(1f, 0.15f, 0.15f);

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
        private Text _weaponName;
        private Text _ammoText;
        private readonly Image[] _toolImages = new Image[PlayerItemUser.SlotCount];
        private readonly Image[] _toolProgress = new Image[PlayerItemUser.SlotCount];
        private readonly Text[] _toolNames = new Text[PlayerItemUser.SlotCount];
        private readonly Text[] _toolCounts = new Text[PlayerItemUser.SlotCount];
        private Text _rollText;
        private RectTransform _podCountdown;
        private Text _podCountdownText;
        private RectTransform _bagPanel;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _camera = Camera.main;
            Build();
        }

        private void Update()
        {
            // 가방 화면이 왼쪽을 덮으므로 열려 있는 동안에는 게이지를 그 오른쪽으로 옮긴다.
            if (_bagPanel == null)
            {
                Transform panel = transform.Find("BagPanel");
                _bagPanel = panel != null ? (RectTransform)panel : null;
            }

            float left = _controller.IsBagOpen && _bagPanel != null
                ? _bagPanel.anchoredPosition.x + _bagPanel.sizeDelta.x + 24f
                : 30f;
            _gaugeRoot.anchoredPosition = new Vector2(left, 30f);

            UpdateGauges();
            UpdateWeapon();
            UpdateTools();
            UpdatePodCountdown();
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
            float ratio = weight / _playerData.MaxWeight;
            _weightGauge.Fill.fillAmount = ratio;
            _weightGauge.Value.text = $"{weight:0.0}\n/ {_playerData.MaxWeight:0}kg";
            _weightGauge.Fill.color = ratio > _playerData.OverloadRatio ? WarningColor : WeightColor;
        }

        private void UpdateWeapon()
        {
            ItemStack weapon = _inventory.GetEquipped(EquipSlot.Weapon);
            if (weapon == null)
            {
                _weaponImage.color = BackColor;
                _weaponName.text = "맨손";
                _weaponName.color = Color.gray;
                _ammoText.text = string.Empty;
                return;
            }

            _weaponImage.color = weapon.Data.Color;
            _weaponName.text = weapon.Data.DisplayName;
            _weaponName.color = Color.black;

            if (weapon.Data.Weapon.MagazineSize <= 0)
            {
                _ammoText.text = string.Empty;
            }
            else if (_shooter.IsReloading)
            {
                _ammoText.text = "재장전 중";
            }
            else
            {
                _ammoText.text = $"{_shooter.AmmoInMagazine} / {_shooter.ReserveAmmo}";
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
                _toolNames[i].text = item != null ? item.DisplayName : string.Empty;
                _toolNames[i].color = has ? Color.black : Color.gray;
                _toolCounts[i].text = item != null ? count.ToString() : string.Empty;
                _toolCounts[i].color = has ? Color.black : Color.gray;

                bool isUsing = item != null && _itemUser.Using == item;
                _toolProgress[i].enabled = isUsing;
                _toolProgress[i].fillAmount = isUsing ? _itemUser.UseProgress : 0f;
            }

            // 구르기를 했을 때만 도구칸 위에 쿨타임을 보여 준다.
            float cooldown = _roll.CooldownRemaining;
            _rollText.text = cooldown > 0f ? $"구르기 {cooldown:0.0}" : string.Empty;
        }

        private void UpdatePodCountdown()
        {
            EscapePod pod = _raid != null ? _raid.CountingPod : null;
            _podCountdown.gameObject.SetActive(pod != null);
            if (pod == null)
            {
                return;
            }

            UiBuilder.MoveToScreenPoint(_canvas, _podCountdown, _camera.WorldToScreenPoint(pod.transform.position + Vector3.up * 1.6f));
            _podCountdownText.text = Mathf.CeilToInt(_raid.CountdownLeft).ToString();
        }

        // ---------- 그리기 ----------

        private void Build()
        {
            // 왼쪽 아래: HP, 산소, 무게 원 게이지
            _gaugeRoot = UiBuilder.Rect("Gauges", transform, Vector2.zero, Vector2.zero, new Vector2(30f, 30f), Vector2.zero);
            _hpGauge = CreateGauge("HP", 0, HpColor);
            _oxygenGauge = CreateGauge("산소", 1, OxygenColor);
            _weightGauge = CreateGauge("무게", 2, WeightColor);

            // 가운데 아래: 무기칸 1개와 도구칸 3개
            float gap = 10f;
            float width = _slotSize * 4f + gap * 3f + 24f;
            RectTransform bar = UiBuilder.Rect("ActionBar", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 30f), new Vector2(width, _slotSize));

            RectTransform weapon = UiBuilder.Rect("WeaponSlot", bar, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(_slotSize, _slotSize));
            _weaponImage = UiBuilder.Image(weapon, BackColor);
            _weaponName = UiBuilder.Text(string.Empty, weapon, _fontSize, TextAnchor.UpperLeft, Color.black);

            // 탄창 / 예비탄은 무기칸 위에 작게 (기획서 9.4)
            RectTransform ammo = UiBuilder.Rect("Ammo", weapon, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 6f),
                new Vector2(_slotSize + 60f, _fontSize + 12f));
            _ammoText = UiBuilder.Text(string.Empty, ammo, _fontSize + 2, TextAnchor.MiddleCenter, Color.white);

            for (int i = 0; i < PlayerItemUser.SlotCount; i++)
            {
                float x = _slotSize + 24f + gap + i * (_slotSize + gap);
                RectTransform slot = UiBuilder.Rect($"ToolSlot {i + 1}", bar, Vector2.zero, Vector2.zero, new Vector2(x, 0f),
                    new Vector2(_slotSize, _slotSize));
                _toolImages[i] = UiBuilder.Image(slot, BackColor);

                RectTransform progress = UiBuilder.Rect("Progress", slot, Vector2.zero, Vector2.zero, Vector2.zero,
                    new Vector2(_slotSize, _slotSize));
                _toolProgress[i] = UiBuilder.Image(progress, new Color(1f, 1f, 1f, 0.55f), UiBuilder.WhiteSprite);
                _toolProgress[i].type = Image.Type.Filled;
                _toolProgress[i].fillMethod = Image.FillMethod.Vertical;
                _toolProgress[i].fillOrigin = (int)Image.OriginVertical.Bottom;

                _toolNames[i] = UiBuilder.Text(string.Empty, slot, _fontSize - 2, TextAnchor.MiddleCenter, Color.black);
                _toolCounts[i] = UiBuilder.Text(string.Empty, slot, _fontSize + 2, TextAnchor.LowerRight, Color.black);
                _toolCounts[i].fontStyle = FontStyle.Bold;

                // 누르는 키
                Text key = UiBuilder.Text((i + 1).ToString(), slot, _fontSize, TextAnchor.UpperLeft, Color.white);
                key.fontStyle = FontStyle.Bold;
                key.gameObject.AddComponent<Outline>().effectColor = Color.black;
            }

            RectTransform roll = UiBuilder.Rect("RollCooldown", bar, new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 6f),
                new Vector2(_slotSize * 3f + gap * 2f, _fontSize + 12f));
            _rollText = UiBuilder.Text(string.Empty, roll, _fontSize, TextAnchor.MiddleCenter, Color.white);

            // 탈출 카운트다운은 포드 위에 숫자로
            _podCountdown = UiBuilder.Rect("PodCountdown", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero,
                new Vector2(120f, 90f));
            _podCountdownText = UiBuilder.Text(string.Empty, _podCountdown, 64, TextAnchor.MiddleCenter, Color.white);
            _podCountdownText.fontStyle = FontStyle.Bold;
            _podCountdownText.gameObject.AddComponent<Outline>().effectColor = Color.black;
            _podCountdown.gameObject.SetActive(false);
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

            Text value = UiBuilder.Text(string.Empty, back, _fontSize + 4, TextAnchor.MiddleCenter, Color.white);
            value.fontStyle = FontStyle.Bold;
            value.gameObject.AddComponent<Outline>().effectColor = Color.black;

            RectTransform labelRect = UiBuilder.Rect("Label", back, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 4f),
                new Vector2(_gaugeSize, _fontSize + 10f));
            UiBuilder.Text(label, labelRect, _fontSize, TextAnchor.MiddleCenter, Color.white);

            return new Gauge { Fill = fill, Value = value };
        }
    }
}
