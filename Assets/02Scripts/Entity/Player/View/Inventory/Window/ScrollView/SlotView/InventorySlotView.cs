using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.Player.Inventory
{
    // 전달받은 화면 데이터만 표현한다.
    public class InventorySlotView : MonoBehaviour
    {
        [Header("Slot UI")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backGroundImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;

        [SerializeField]
        private Color _normalIconColor = Color.white;

        [SerializeField, Range(0f, 1f)]
        private float _draggingIconAlpha = 0.25f;

        // Domain 객체 대신 슬롯 식별자만 보관한다.
        public int SlotIndex { get; private set; } = -1;
        public bool HasItem { get; private set; }
        public Sprite Icon => _iconImage != null ? _iconImage.sprite : null;

        private void Awake()
        {
            ConfigureIconImage();
        }

        private void ConfigureIconImage()
        {
            if (_iconImage == null)
            {
                return;
            }

            _iconImage.enabled = true;
            _iconImage.raycastTarget = false;
        }

        public void Bind(int p_slotIndex)
        {
            SlotIndex = p_slotIndex;
            ClearItem();
        }

        #region ============================== Slot 관리
        // 가공이 끝난 화면 정보만 전달받는다.
        public void Apply(InventorySlotViewData p_viewData, ResourceLoadSystem p_resourceLoader)
        {
            ConfigureIconImage();

            SlotIndex = p_viewData.SlotIndex;
            HasItem = !p_viewData.IsEmpty;

            if (!HasItem)
            {
                ClearItem();
                return;
            }

            Sprite icon = p_resourceLoader?.GetIcon(p_viewData.ItemType, p_viewData.IconKey);

            ApplyIcon(icon);

            if (_nameText != null)
            {
                _nameText.text = p_viewData.ItemName ?? string.Empty;
            }

            if (_countText != null)
            {
                _countText.text = p_viewData.Count > 1? p_viewData.Count.ToString() : string.Empty;
            }
        }

        private void ApplyIcon(Sprite p_icon)
        {
            if (_iconImage == null)
            {
                return;
            }

            _iconImage.sprite = p_icon;

            Color color = _normalIconColor;
            color.a = p_icon != null? _normalIconColor.a : 0f;

            _iconImage.color = color;
        }

        public void ResetView()
        {
            SlotIndex = -1;
            HasItem = false;

            ClearItem();
        }

        // 해당 슬롯이 비었을 때 화면만 초기화한다.
        private void ClearItem()
        {
            HasItem = false;

            ApplyIcon(null);

            if (_nameText != null)
            {
                _nameText.text = string.Empty;
            }

            if (_countText != null)
            {
                _countText.text = string.Empty;
            }
        }


        #endregion ============================== /Slot 관리

        // 드래그 시 반투명하게
        public void SetDragging(bool p_isDragging)
        {
            if (_iconImage == null || !HasItem || _iconImage.sprite == null)
            {
                return;
            }

            Color color = _normalIconColor;
            color.a = p_isDragging? _draggingIconAlpha : _normalIconColor.a;

            _iconImage.color = color;
        }

    }
}