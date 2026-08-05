using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.Player.Inventory
{
    public class InventorySlotView : MonoBehaviour
    {
        [Header("Slot UI")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backGroundImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;

        private InventorySlot _slot;
        private ResourceLoadSystem _resourceLoader;

        public InventorySlot Slot => _slot;

        public void Bind(InventorySlot p_slot, ResourceLoadSystem p_resourceLoader)
        {
            Unbind();

            _slot = p_slot;
            _resourceLoader = p_resourceLoader;

            if (_slot == null)
            {
                Clear();
                return;
            }

            _slot.OnChanged += HandleSlotChanged;

            // 현재 슬롯 상태를 즉시 표시한다.
            Refresh();
        }

        #region ============================== Slot 관리
        private void HandleSlotChanged(InventorySlot p_slot)
        {
            Refresh();
        }

        // 로직 슬롯 상태를 기반으로 UI를 갱신한다.
        private void Refresh()
        {
            if (_slot == null || _slot.IsEmpty)
            {
                Clear();
                return;
            }

            ItemDTO item = _slot.Item;

            Sprite icon = _resourceLoader?.GetIcon(item.ItemType, item.IconKey);

            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;

            _nameText.text = item.Name;

            // 수량이 1인 아이템은 숫자를 표시하지 않는다.
            _countText.text = _slot.Count > 1? _slot.Count.ToString() : string.Empty;
        }

        private void Clear()
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = null;
                _iconImage.enabled = false;
            }

            if (_nameText != null)
                _nameText.text = string.Empty;

            if (_countText != null)
                _countText.text = string.Empty;
        }
        #endregion ============================== /Slot 관리

        #region ============================== Drag & Drop 관리

        #endregion ============================== /Drag & Drop 관리
        public void Unbind()
        {
            if (_slot != null)
            {
                _slot.OnChanged -= HandleSlotChanged;
            }

            _slot = null;
            _resourceLoader = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}