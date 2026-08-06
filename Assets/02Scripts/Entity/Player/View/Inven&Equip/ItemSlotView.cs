using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.Player.Slot
{
    // Inventory와 Equipment가 공유하는 슬롯 화면 표현.
    public class ItemSlotView : MonoBehaviour
    {
        [Header("Slot UI")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backGroundImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;

        [SerializeField] private Color _normalIconColor = Color.white;

        [SerializeField, Range(0f, 1f)]
        private float _draggingIconAlpha = 0.25f;

        public bool HasItem { get; private set; }
        public Sprite Icon => _iconImage != null ? _iconImage.sprite : null;

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        private void Awake()
        {
            ConfigureIconImage();
        }

        // 전달받은 화면 데이터로 아이콘·이름·수량을 갱신한다.
        public void Apply(ItemSlotViewData p_viewData, ResourceLoadSystem p_resourceLoader)
        {
            ConfigureIconImage();
            HasItem = !p_viewData.IsEmpty;

            // 빈 데이터는 이전 아이템 표시를 모두 제거한다.
            if (!HasItem)
            {
                ClearItem();
                return;
            }

            // 아이콘은 키를 사용해 캐시에서 조회하고 나머지는 전달 값을 그대로 표시한다.
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

        // 재사용 또는 해제 전에 슬롯 표시를 빈 상태로 되돌린다.
        public void ResetView()
        {
            ClearItem();
        }

        // 공통 드래그 피드백.
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

        // 아이콘이 드롭 대상의 UI Raycast를 가로막지 않도록 설정한다.
        private void ConfigureIconImage()
        {
            if (_iconImage == null)
                return;

            _iconImage.enabled = true;
            _iconImage.raycastTarget = false;
        }

        // 아이콘 Sprite와 표시 알파를 함께 적용한다.
        private void ApplyIcon(Sprite p_icon)
        {
            if (_iconImage == null)
                return;

            _iconImage.sprite = p_icon;

            Color color = _normalIconColor;
            color.a = p_icon != null? _normalIconColor.a : 0f;

            _iconImage.color = color;
        }

        // 아이콘·이름·수량을 모두 지워 빈 슬롯으로 표시한다.
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
    }
}
