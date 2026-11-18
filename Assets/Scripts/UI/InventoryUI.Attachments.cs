using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 총기 부착물 창 (기획서 17장). 가방에서 무기를 우클릭하면 열린다.
    // 부착물을 부위 칸으로 끌어다 놓으면 달리고, 달린 부착물을 누르면 빠져서 가방으로 돌아간다.
    public partial class InventoryUI
    {
        private static readonly string[] SlotNames = { "개머리판", "손잡이", "조준경", "총구", "레이저" };

        private RectTransform _attachPanel;
        private Text _attachTitle;
        private Text _attachStats;
        private readonly RectTransform[] _attachCells = new RectTransform[5];
        private readonly Image[] _attachIcons = new Image[5];
        private RectTransform _attachClose;
        private ItemStack _attachWeapon;

        private bool IsAttachOpen => _attachWeapon != null;

        // 우클릭한 칸의 무기가 부착물을 달 수 있으면 창을 연다. 열었으면 true.
        private bool TryOpenAttachments(DropTarget target, Vector2 mouse)
        {
            ItemStack stack = null;
            if (target.Kind == TargetKind.Grid)
            {
                stack = target.View.Grid.GetAt(target.X, target.Y);
            }
            else if (target.Kind == TargetKind.Equip)
            {
                stack = _inventory.GetEquipped(target.Slot);
            }

            if (stack == null || stack.Attachments == null)
            {
                return false;
            }

            _attachWeapon = stack;
            _attachPanel.gameObject.SetActive(true);
            _attachPanel.SetAsLastSibling();

            MoveToScreenPoint(_attachPanel, mouse);
            Rect screen = ((RectTransform)transform).rect;
            Vector2 size = _attachPanel.sizeDelta;
            Vector3 position = _attachPanel.localPosition;
            position.x = Mathf.Clamp(position.x + 20f, screen.xMin + 10f, screen.xMax - size.x - 10f);
            position.y = Mathf.Clamp(position.y, screen.yMin + size.y * 0.5f + 10f, screen.yMax - size.y * 0.5f - 10f);
            _attachPanel.localPosition = position;

            RefreshAttachments();
            Sfx.Play(SfxId.UiPrompt);
            return true;
        }

        private void CloseAttachments()
        {
            _attachWeapon = null;
            if (_attachPanel != null)
            {
                _attachPanel.gameObject.SetActive(false);
            }
        }

        // 아무것도 들지 않았을 때 창 안의 클릭. 창 안을 눌렀으면 true (다른 칸의 아이템을 집지 않게).
        private bool HandleAttachClick(Vector2 mouse)
        {
            if (!IsAttachOpen || !RectTransformUtility.RectangleContainsScreenPoint(_attachPanel, mouse, UiCamera))
            {
                return false;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(_attachClose, mouse, UiCamera))
            {
                Sfx.Play(SfxId.UiCancel);
                CloseAttachments();
                return true;
            }

            for (int i = 0; i < _attachCells.Length; i++)
            {
                ItemData attachment = _attachWeapon.Attachments[i];
                if (attachment == null || !RectTransformUtility.RectangleContainsScreenPoint(_attachCells[i], mouse, UiCamera))
                {
                    continue;
                }

                // 빼면 가방으로 돌아간다. 가방에 자리가 없으면 그대로 둔다.
                if (_inventory.Bag.TryAdd(new ItemStack(attachment)))
                {
                    _attachWeapon.Attachments[i] = null;
                    Sfx.Play(SfxId.ItemPick);
                    _inventory.NotifyChanged();
                    _dirty = true;
                    RefreshAttachments();
                }
                else
                {
                    Sfx.Play(SfxId.ItemRefuse);
                }
            }

            return true;
        }

        private bool TryFindAttachSlot(Vector2 mouse, ref DropTarget target)
        {
            if (!IsAttachOpen)
            {
                return false;
            }

            for (int i = 0; i < _attachCells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_attachCells[i], mouse, UiCamera))
                {
                    target.Kind = TargetKind.Attachment;
                    target.X = i;
                    return true;
                }
            }

            // 창 위에 놓으면 아무 일도 없이 제자리로 돌아간다.
            if (RectTransformUtility.RectangleContainsScreenPoint(_attachPanel, mouse, UiCamera))
            {
                target.Kind = TargetKind.None;
                return true;
            }

            return false;
        }

        // 그 부위의 부착물만 달 수 있다. 이미 달려 있으면 맞바꾸고, 원래 것은 끌어온 자리로 간다.
        private bool CanAttach(int slot)
        {
            if (_held.Data.Category != ItemCategory.Attachment || (int)_held.Data.AttachmentSlot != slot || _held == _attachWeapon)
            {
                return false;
            }

            ItemData old = _attachWeapon.Attachments[slot];
            return old == null || (!_originIsEquip && _originView.Grid.CanPlace(new ItemStack(old), _originX, _originY));
        }

        private void Attach(int slot)
        {
            ItemData old = _attachWeapon.Attachments[slot];
            _attachWeapon.Attachments[slot] = _held.Data;
            if (old != null)
            {
                _originView.Grid.Place(new ItemStack(old), _originX, _originY);
            }

            RefreshAttachments();
        }

        private void RefreshAttachments()
        {
            if (!IsAttachOpen)
            {
                return;
            }

            _attachTitle.text = $"{_attachWeapon.Data.DisplayName} 부착물";
            for (int i = 0; i < _attachCells.Length; i++)
            {
                ItemData attachment = _attachWeapon.Attachments[i];
                UiBuilder.SetIcon(_attachIcons[i], attachment != null ? attachment.Icon : null, Color.white);
            }

            float recoil = (1f - _attachWeapon.RecoilMultiplier) * 100f;
            float spread = (1f - _attachWeapon.SpreadMultiplier) * 100f;
            string move = _attachWeapon.MoveSpreadScale < 2f ? $"   이동 중 퍼짐 ×{_attachWeapon.MoveSpreadScale:0.0#}" : string.Empty;
            string laser = _attachWeapon.HasLaserSight ? "   조준 레이저" : string.Empty;
            _attachStats.text = $"반동 −{recoil:0}%   퍼짐 −{spread:0}%{move}{laser}";
        }

        private void BuildAttachPanel(RectTransform root)
        {
            float cell = _cellSize;
            float pitch = cell + 14f;
            var size = new Vector2(pitch * 5f + 30f, cell + 170f);
            _attachPanel = CreateRect("AttachPanel", root, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, size);
            _attachPanel.anchorMax = Vector2.zero;
            UiBuilder.Window(_attachPanel);

            _attachTitle = CreateText(string.Empty, _attachPanel, new Vector2(20f, -12f), new Vector2(size.x - 140f, 36f), 24, TextAnchor.MiddleLeft);
            _attachTitle.fontStyle = FontStyle.Bold;
            _attachTitle.color = UiTheme.Accent;

            _attachClose = CreateRect("Close", _attachPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -10f), new Vector2(96f, 40f));
            UiBuilder.Panel(_attachClose, UiTheme.ButtonQuietFill, UiTheme.SlotBorder);
            UiBuilder.Text("닫기", _attachClose, 20, TextAnchor.MiddleCenter, UiTheme.Text);

            for (int i = 0; i < _attachCells.Length; i++)
            {
                float x = 15f + i * pitch + 7f;
                _attachCells[i] = CreateRect(SlotNames[i], _attachPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -60f), new Vector2(cell, cell));
                UiBuilder.Cell(_attachCells[i], UiTheme.Cell);
                _attachIcons[i] = UiBuilder.Icon(_attachCells[i], cell * 0.78f);
                Text label = CreateText(SlotNames[i], _attachPanel, new Vector2(x - 7f, -64f - cell), new Vector2(cell + 14f, 30f), 18, TextAnchor.MiddleCenter);
                label.color = UiTheme.TextDim;
            }

            _attachStats = CreateText(string.Empty, _attachPanel, new Vector2(20f, -cell - 102f), new Vector2(size.x - 40f, 56f), 19, TextAnchor.UpperLeft);
            _attachPanel.gameObject.SetActive(false);
        }
    }
}
