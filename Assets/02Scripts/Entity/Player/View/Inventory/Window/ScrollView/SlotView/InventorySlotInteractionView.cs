using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alpha.Player.Inventory
{
    public enum EDragMode
    {
        None,
        Item,
        Scroll
    }
    // 슬롯의 Drag, Drop, Scroll 입력 조정 담당
    public class InventorySlotInteractionView : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler,
                                                IDragHandler, IEndDragHandler, IDropHandler
    {

        [Header("Item Drag Area")]
        [SerializeField]
        private RectTransform _itemDragArea;

        private InventorySlotView _slotView;
        private InventoryDragIconView _dragIconView;
        private ScrollRect _scrollRect;

        private EDragMode _dragMode;

        public int SlotIndex => _slotView != null? _slotView.SlotIndex : -1;

        public bool IsItemDragging => _dragMode == EDragMode.Item;

        public event Action<int, int> OnTransferRequested;

        private void Awake()
        {
            _slotView = GetComponent<InventorySlotView>();

            _scrollRect = GetComponentInParent<ScrollRect>();

            // InventoryView 계층에 있는 공용 프리뷰를 조회한다.
            _dragIconView = GetComponentInParent<InventoryDragIconView>(true);
        }

        public void OnInitializePotentialDrag(PointerEventData p_eventData)
        {
            // ScrollRect의 기존 속도를 초기화한다.
            _scrollRect?.OnInitializePotentialDrag(p_eventData);
        }

        public void OnBeginDrag(PointerEventData p_eventData)
        {
            _dragMode = EDragMode.None;

            if (p_eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (CanBeginItemDrag(p_eventData))
            {
                _dragMode = EDragMode.Item;

                // 슬롯 아이콘 투명하게
                _slotView.SetDragging(true);

                GetDragIconView()?.Show(_slotView.Icon, p_eventData);

                return;
            }

            if (_scrollRect == null)
                return;

            _dragMode = EDragMode.Scroll;
            _scrollRect.OnBeginDrag(p_eventData);
        }

        public void OnDrag(PointerEventData p_eventData)
        {
            switch (_dragMode)
            {
                case EDragMode.Item:
                    GetDragIconView()?.Move(p_eventData);
                    break;

                case EDragMode.Scroll:
                    _scrollRect?.OnDrag(p_eventData);
                    break;
            }
        }

        public void OnEndDrag(PointerEventData p_eventData)
        {
            switch (_dragMode)
            {
                case EDragMode.Item:
                    _slotView?.SetDragging(false);
                    _dragIconView?.Hide();
                    break;

                case EDragMode.Scroll:
                    _scrollRect?.OnEndDrag(p_eventData);
                    break;
            }

            _dragMode = EDragMode.None;
        }

        public void OnDrop(PointerEventData p_eventData)
        {
            InventorySlotInteractionView sourceView =
                p_eventData.pointerDrag?.GetComponentInParent<InventorySlotInteractionView>();

            if (sourceView == null || !sourceView.IsItemDragging || _slotView == null)
            {
                return;
            }

            int sourceSlotIndex = sourceView.SlotIndex;
            int targetSlotIndex = SlotIndex;

            if (sourceSlotIndex < 0 || targetSlotIndex < 0)
            {
                return;
            }

            OnTransferRequested?.Invoke(sourceSlotIndex, targetSlotIndex);
        }

        private bool CanBeginItemDrag(PointerEventData p_eventData)
        {
            if (_slotView == null || !_slotView.HasItem ||
                _slotView.Icon == null || _slotView.SlotIndex < 0)
            {
                return false;
            }

            // 별도 영역이 없으면 슬롯 전체를 Drag 영역으로 사용한다.
            if (_itemDragArea == null)
                return true;

            // 현재 위치가 아니라 최초 클릭 위치로 판단한다.
            return RectTransformUtility.RectangleContainsScreenPoint(_itemDragArea, p_eventData.pressPosition, p_eventData.pressEventCamera);
        }

        private InventoryDragIconView GetDragIconView()
        {
            if (_dragIconView == null)
            {
                _dragIconView = GetComponentInParent<InventoryDragIconView>(true);
            }

            return _dragIconView;
        }

        private void OnDisable()
        {
            if (_dragMode == EDragMode.Item)
            {
                _slotView?.SetDragging(false);
                _dragIconView?.Hide();
            }
            _dragMode = EDragMode.None;
        }
    }
}
