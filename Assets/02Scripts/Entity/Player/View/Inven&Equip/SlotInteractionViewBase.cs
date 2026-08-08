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

        // HandleDoubleClick 이벤트를 받아 필요한 후속 처리를 수행한다.
        protected abstract void HandleDoubleClick();

        // HandleDrop 이벤트를 받아 필요한 후속 처리를 수행한다.
        protected abstract void HandleDrop(SlotInteractionViewBase p_source);

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

        #region ============================== 슬롯 클릭 시 판단
        // 아이템이 있는 유효한 슬롯의 좌클릭 더블 클릭만 처리한다.
        public void OnPointerClick(PointerEventData p_eventData)
        {
            if (!HasValidSlot() || IsEmptySlot())
                return;

            // 더블클릭 시 Inventory와 Eqipment에서 정의된 동작 실행
            if (IsDoubleClick(p_eventData))
                HandleDoubleClick();    
        }
        // 판별
        private bool HasValidSlot()
        {
            return SlotView != null && HasValidSource;
        }
        private bool IsEmptySlot()
        {
            return !SlotView.HasItem;
        }

        // 두 번째 좌클릭 이벤트인지 확인한다.
        private static bool IsDoubleClick(PointerEventData p_eventData)
        {
            return p_eventData.button == PointerEventData.InputButton.Left &&
                   p_eventData.clickCount == 2;
        }
        #endregion ============================== /슬롯 클릭시 판단

        #region ============================== 드래그 시작 시 판단
        // 이동량이 Drag Threshold를 넘었을 때 호출된다.
        public void OnBeginDrag(PointerEventData p_eventData)
        {
            // 드래그 종류 판별
            _dragMode = ResolveDragMode(p_eventData);

            // 아이템 드래그인지 스크롤뷰 드래그인지 판별
            switch (_dragMode)
            {
                case ESlotDragMode.Item:
                    BeginItemDrag(p_eventData);
                    break;

                case ESlotDragMode.Scroll:
                    BeginScrollDrag(p_eventData);
                    break;
            }
        }

        // 슬롯 상태와 시작 위치를 기준으로 드래그 종류를 결정한다.
        private ESlotDragMode ResolveDragMode(PointerEventData p_eventData)
        {
            // 아무것도 없는(스크롤뷰, 슬롯)곳 클릭한 상태
            if (p_eventData.button != PointerEventData.InputButton.Left ||
                SlotView == null || !HasValidSource)
            {
                return ESlotDragMode.None;
            }

            if (CanBeginItemDrag(p_eventData))
            {
                return ESlotDragMode.Item;
            }

            return _scrollRect != null? ESlotDragMode.Scroll : ESlotDragMode.None;
        }

        // 아이템 있는 슬롯 선택시 드래그 가능한지 판별
        private bool CanBeginItemDrag(PointerEventData p_eventData)
        {
            if (IsEmptySlot() || SlotView.Icon == null)
                return false;

            return _itemDragArea == null ||
                   RectTransformUtility.RectangleContainsScreenPoint(_itemDragArea, p_eventData.pressPosition, p_eventData.pressEventCamera);
        }

        // 드래그 아이콘 셋팅 및 아이템 드래그 시작.
        private void BeginItemDrag(PointerEventData p_eventData)
        {
            SlotView.SetDragging(true);
            GetDragIconView()?.Show(SlotView.Icon, p_eventData);
        }

        // 상위 ScrollRect에 Scroll 드래그 시작을 전달한다.
        private void BeginScrollDrag(PointerEventData p_eventData)
        {
            if (_scrollRect == null)
                return;

            _scrollRect.OnBeginDrag(p_eventData);
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
        #endregion ============================== 드래그 시작 시 판단


        // 결정된 드래그 모드에 맞는 대상을 이동시킨다.
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

        #region ============================== Drop 발생 전달
        // 드래그 원본과 이동 방향만 판별.
        // 빈 슬롯인지, 교환 가능한지는 판단 X -> 각 Flow에서 판단
        public void OnDrop(PointerEventData p_eventData)
        {
            if (!HasValidSource)
                return;

            if (!TryGetDragSource(p_eventData, out SlotInteractionViewBase source))
            {
                return;
            }

            HandleDrop(source);
        }

        // 실제 아이템 드래그 중인 다른 슬롯을 원본으로 반환한다.
        private bool TryGetDragSource(PointerEventData p_eventData, out SlotInteractionViewBase p_source)
        {
            p_source = p_eventData.pointerDrag?.GetComponentInParent<SlotInteractionViewBase>();

            return p_source != null && p_source != this && p_source.IsItemDragging;
        }
        #endregion ============================== /Drop 발생 전달

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
