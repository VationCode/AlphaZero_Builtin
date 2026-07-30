using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alpha.Slot
{
    // 슬롯의 드래그 입력과 드래그 아이콘 표현을 담당한다.
    // 실제 아이템 변경은 처리하지 않고 Presenter에 요청만 전달한다.
    public class SlotDragView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField]
        private Vector2 _dragIconSize = new(80f, 80f);

        private SlotViewBase _slotView;
        private ScrollRect _scrollRect;
        private Canvas _rootCanvas;

        // 드래그 중 임시로 표시하는 아이콘
        private Image _dragIcon;

        // 빈 슬롯 드래그와 아이템 드래그 상태 구분
        private bool _isScrollDrag;
        private bool _isItemDrag;

        // Source와 Target의 SlotView를 Presenter에 전달한다.
        public event Action<SlotViewBase, SlotViewBase> OnDropRequested;

        private void Awake()
        {
            _slotView = GetComponent<SlotViewBase>();
            _scrollRect = GetComponentInParent<ScrollRect>();

            // 드래그 아이콘은 최상위 Canvas에 생성한다.
            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas != null)
                _rootCanvas = canvas.rootCanvas;
        }

        public void OnBeginDrag(PointerEventData p_eventData)
        {
            // 아이템 이동은 마우스 왼쪽 버튼만 허용한다.
            if (p_eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            // 빈 슬롯에서 드래그하면 아이템 이동이 아니라
            // 상위 ScrollRect의 스크롤 입력으로 전달한다.
            if (_slotView == null || !_slotView.HasItem)
            {
                if (_scrollRect == null)
                    return;

                _isScrollDrag = true;
                _scrollRect.OnBeginDrag(p_eventData);
                return;
            }

            // 아이콘을 생성하지 못하면 드래그를 시작하지 않는다.
            if (!CreateDragIcon())
                return;

            _isItemDrag = true;

            // 원본 아이콘은 드래그 중 중복 표시되지 않도록 숨긴다.
            _slotView.SetIconVisible(false);

            MoveDragIcon(p_eventData.position);
        }

        public void OnDrag(PointerEventData p_eventData)
        {
            if (_isScrollDrag)
            {
                _scrollRect?.OnDrag(p_eventData);
                return;
            }

            if (_isItemDrag)
                MoveDragIcon(p_eventData.position);
        }

        public void OnDrop(PointerEventData p_eventData)
        {
            if (p_eventData.pointerDrag == null || _slotView == null)
            {
                return;
            }

            // 드래그를 시작한 Source SlotDragView를 조회한다.
            SlotDragView sourceDragView = p_eventData.pointerDrag.GetComponent<SlotDragView>();

            if (sourceDragView == null || sourceDragView == this ||
                !sourceDragView._isItemDrag || sourceDragView._slotView == null)
            {
                return;
            }

            // View는 아이템 상태를 직접 변경하지 않는다.
            // Presenter가 Source와 Target에 연결된 SlotBase를 찾아
            // InventoryModule에 변경을 요청한다.
            OnDropRequested?.Invoke(sourceDragView._slotView, _slotView);
        }

        public void OnEndDrag(PointerEventData p_eventData)
        {
            if (_isScrollDrag)
            {
                _scrollRect?.OnEndDrag(p_eventData);
                _isScrollDrag = false;
                return;
            }

            ClearItemDrag();
        }


        private bool CreateDragIcon()
        {
            if (_rootCanvas == null || _slotView == null || _slotView.Icon == null)
            {
                return false;
            }

            GameObject dragObject = new("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            dragObject.transform.SetParent(_rootCanvas.transform, false);

            _dragIcon = dragObject.GetComponent<Image>();
            _dragIcon.sprite = _slotView.Icon;
            _dragIcon.preserveAspect = true;

            // 드래그 아이콘이 Raycast를 막으면
            // 아래에 있는 Target 슬롯이 Drop 이벤트를 받지 못한다. 그래서 Off
            _dragIcon.raycastTarget = false;

            RectTransform dragRect = _dragIcon.rectTransform;

            dragRect.sizeDelta = _dragIconSize;
            dragRect.SetAsLastSibling();

            return true;
        }

        private void MoveDragIcon(Vector2 p_screenPosition)
        {
            if (_dragIcon == null || _rootCanvas == null)
                return;

            RectTransform canvasRect = _rootCanvas.transform as RectTransform;

            Camera eventCamera =
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay? null : _rootCanvas.worldCamera;

            // 화면 좌표를 Canvas 내부 좌표로 변환한다.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, p_screenPosition, eventCamera, out Vector2 localPosition))
            {
                _dragIcon.rectTransform.anchoredPosition = localPosition;
            }
        }

        private void ClearItemDrag()
        {
            if (_dragIcon != null)
                Destroy(_dragIcon.gameObject);

            _dragIcon = null;
            _isItemDrag = false;

            // 이동 결과에 따라 갱신된 현재 슬롯 아이콘을 다시 표시한다.
            _slotView?.SetIconVisible(true);
        }

        private void OnDisable()
        {
            // UI가 닫힐 때 남아 있는 드래그 상태와 아이콘을 정리한다.
            _isScrollDrag = false;
            ClearItemDrag();
        }
    }
}
