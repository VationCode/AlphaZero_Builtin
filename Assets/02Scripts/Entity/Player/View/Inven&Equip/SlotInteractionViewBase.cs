using Alpha.Player.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alpha.Player.Slot
{
    // ESlotDragMode 관련 선택 값을 정의한다.
    public enum ESlotDragMode
    {
        None,
        Item,
        Scroll
    }

    // 슬롯 종류와 무관한 클릭·드래그 생명주기를 담당한다.
    // Inventory와 Equipment 슬롯이 공통 입력 처리를 재사용하는 부모 클래스다.
    public abstract class SlotInteractionViewBase : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, 
                                                    IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [Header("Item Drag Area")]
        [SerializeField] private RectTransform _itemDragArea;

        private InventoryDragIconView _dragIconView;
        private ScrollRect _scrollRect;
        private ESlotDragMode _dragMode;

        protected ItemSlotView SlotView { get; private set; }
        protected abstract bool HasValidSource { get; }

        public bool IsItemDragging => _dragMode == ESlotDragMode.Item;

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        protected virtual void Awake()
        {
            SlotView = GetComponent<ItemSlotView>();
            _scrollRect = GetComponentInParent<ScrollRect>();
            _dragIconView = GetComponentInParent<InventoryDragIconView>(true);
        }

        // ScrollRect가 드래그 후보 상태를 준비하도록 전달한다.
        public void OnInitializePotentialDrag(PointerEventData p_eventData)
        {
            _scrollRect?.OnInitializePotentialDrag(p_eventData);
        }

        // 포인터 위치와 슬롯 상태에 따라 아이템 드래그와 스크롤을 구분한다.
        public void OnBeginDrag(PointerEventData p_eventData)
        {
            _dragMode = ESlotDragMode.None;

            // 좌클릭만 아이템 또는 스크롤 드래그로 처리한다.
            if (p_eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            // 아이템 영역에서 시작했다면 드래그 아이콘과 원본 피드백을 표시한다.
            if (CanBeginItemDrag(p_eventData))
            {
                _dragMode = ESlotDragMode.Item;
                SlotView.SetDragging(true);
                GetDragIconView()?.Show(SlotView.Icon, p_eventData);
                return;
            }

            if (_scrollRect == null)
                return;

            // 아이템 드래그가 아니면 상위 ScrollRect에 동작을 위임한다.
            _dragMode = ESlotDragMode.Scroll;
            _scrollRect.OnBeginDrag(p_eventData);
        }

        // 시작 시 결정된 드래그 모드에 맞는 대상을 이동시킨다.
        public void OnDrag(PointerEventData p_eventData)
        {
            switch (_dragMode)
            {
                case ESlotDragMode.Item:
                    GetDragIconView()?.Move(p_eventData);
                    break;

                case ESlotDragMode.Scroll:
                    _scrollRect?.OnDrag(p_eventData);
                    break;
            }
        }

        // 아이템 피드백 또는 ScrollRect 드래그를 종료하고 모드를 초기화한다.
        public void OnEndDrag(PointerEventData p_eventData)
        {
            // 시작한 Drag Mode에 대응하는 시각 상태 또는 ScrollRect 입력을 종료한다.
            switch (_dragMode)
            {
                case ESlotDragMode.Item:
                    SlotView?.SetDragging(false);
                    _dragIconView?.Hide();
                    break;

                case ESlotDragMode.Scroll:
                    _scrollRect?.OnEndDrag(p_eventData);
                    break;
            }

            _dragMode = ESlotDragMode.None;
        }

        // 아이템이 있는 유효한 슬롯의 좌클릭 더블 클릭만 처리한다.
        public void OnPointerClick(PointerEventData p_eventData)
        {
            if (p_eventData.button != PointerEventData.InputButton.Left || p_eventData.clickCount != 2 ||
                SlotView == null || !SlotView.HasItem || !HasValidSource)
            {
                return;
            }

            HandleDoubleClick();
        }

        // 실제 아이템 드래그 원본을 찾아 슬롯별 드롭 처리로 전달한다.
        public void OnDrop(PointerEventData p_eventData)
        {
            SlotInteractionViewBase source =
                p_eventData.pointerDrag?.GetComponentInParent<SlotInteractionViewBase>();

            if (source == null || !source.IsItemDragging || !HasValidSource)
            {
                return;
            }

            HandleDrop(source);
        }

        // HandleDoubleClick 이벤트를 받아 필요한 후속 처리를 수행한다.
        protected abstract void HandleDoubleClick();

        // HandleDrop 이벤트를 받아 필요한 후속 처리를 수행한다.
        protected abstract void HandleDrop(SlotInteractionViewBase p_source);

        // 아이템과 아이콘이 있고 지정된 드래그 영역에서 시작했는지 검사한다.
        private bool CanBeginItemDrag(PointerEventData p_eventData)
        {
            if (SlotView == null || !SlotView.HasItem || SlotView.Icon == null || !HasValidSource)
            {
                return false;
            }

            return _itemDragArea == null ||
                   RectTransformUtility.RectangleContainsScreenPoint(_itemDragArea, p_eventData.pressPosition, p_eventData.pressEventCamera);
        }

        // 비활성 자식까지 포함해 공용 드래그 아이콘 View를 지연 조회한다.
        private InventoryDragIconView GetDragIconView()
        {
            if (_dragIconView == null)
            {
                _dragIconView = GetComponentInParent<InventoryDragIconView>(true);
            }

            return _dragIconView;
        }

        // 비활성화 도중 남을 수 있는 아이템 드래그 피드백을 정리한다.
        private void OnDisable()
        {
            if (_dragMode == ESlotDragMode.Item)
            {
                SlotView?.SetDragging(false);
                _dragIconView?.Hide();
            }

            _dragMode = ESlotDragMode.None;
        }
    }
}
