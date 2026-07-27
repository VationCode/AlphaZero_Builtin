using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.Slot
{
    // 아이콘·이름·수량 표현
    public class SlotViewBase : MonoBehaviour
    {
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;

        private ResourceLoadSystem _resourceLoader;

        public bool HasItem { get; private set; }
        public Sprite Icon => _itemIcon.sprite;

        // 리소스 로더 연결
        public void Bind(ResourceLoadSystem p_resourceLoader)
        {
            _resourceLoader = p_resourceLoader;
        }

        public void SetSlot(ItemDTO p_item, int p_count)
        {
            if (p_item == null)
            {
                Clear();
                return;
            }

            HasItem = true;

            Sprite icon = _resourceLoader?.GetIcon(p_item.ItemType, p_item.IconKey);

            _itemIcon.sprite = icon;
            SetIconVisible(true);

            _nameText.text = p_item.Name;
            _countText.text = p_count > 1 ? p_count.ToString() : string.Empty;
        }

        public void Clear()
        {
            HasItem = false;

            _itemIcon.sprite = null;
            SetIconVisible(false);

            _nameText.text = string.Empty;
            _countText.text = string.Empty;
        }

        // Drag 중 원본 아이콘 표시 상태 변경
        public void SetIconVisible(bool p_visible)
        {
            _itemIcon.enabled = p_visible && _itemIcon.sprite != null;
        }
    }
}