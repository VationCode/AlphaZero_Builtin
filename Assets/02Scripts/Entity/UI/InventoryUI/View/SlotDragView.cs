using Alpha.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Drag/Drop 이벤트, ScrollRect, 드래그 아이콘
public class SlotDragView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField]
    private Vector2 _dragIconSize = new(80f, 80f);

    private SlotViewBase _slotView;
    private SlotDragHandler _dragHandler;

    private ScrollRect _scrollRect;
    private Canvas _rootCanvas;
    private Image _dragIcon;

    private bool _isScrollDrag;

    private void Awake()
    {
        _slotView = GetComponent<SlotViewBase>();
        _scrollRect = GetComponentInParent<ScrollRect>();

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            _rootCanvas = canvas.rootCanvas;
    }

    public void BindDragHandler(SlotDragHandler p_handler)
    {
        _dragHandler = p_handler;
    }

    public void OnBeginDrag(PointerEventData p_eventData)
    {
        if (p_eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        // 빈 슬롯은 ScrollRect로 전달
        if (!_slotView.HasItem)
        {
            if (_scrollRect == null)
                return;

            _isScrollDrag = true;
            _scrollRect.OnBeginDrag(p_eventData);
            return;
        }

        _isScrollDrag = false;

        if (!CreateDragIcon())
            return;

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

        MoveDragIcon(p_eventData.position);
    }

    public void OnDrop(PointerEventData p_eventData)
    {
        if (p_eventData.pointerDrag == null || _dragHandler == null)
        {
            return;
        }

        SlotDragView sourceView = p_eventData.pointerDrag.GetComponent<SlotDragView>();

        if (sourceView == null || sourceView == this || sourceView._dragIcon == null)
        {
            return;
        }

        sourceView._dragHandler?.TryMoveTo(_dragHandler);
    }

    public void OnEndDrag(PointerEventData p_eventData)
    {
        if (_isScrollDrag)
        {
            _scrollRect?.OnEndDrag(p_eventData);
            _isScrollDrag = false;
            return;
        }

        ClearDrag();
    }

    private bool CreateDragIcon()
    {
        if (_rootCanvas == null ||
            _slotView.Icon == null)
        {
            return false;
        }

        GameObject dragObject = new("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        dragObject.transform.SetParent(_rootCanvas.transform, false);

        _dragIcon = dragObject.GetComponent<Image>();
        _dragIcon.sprite = _slotView.Icon;
        _dragIcon.raycastTarget = false;
        _dragIcon.preserveAspect = true;

        RectTransform dragRect = _dragIcon.rectTransform;

        dragRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragRect.pivot = new Vector2(0.5f, 0.5f);
        dragRect.sizeDelta = _dragIconSize;
        dragRect.SetAsLastSibling();

        return true;
    }

    private void MoveDragIcon(Vector2 p_screenPosition)
    {
        if (_dragIcon == null || _rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = _rootCanvas.transform as RectTransform;

        Camera eventCamera =
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay? null : _rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, p_screenPosition, eventCamera, out Vector2 localPosition))
        {
            _dragIcon.rectTransform.anchoredPosition =
                localPosition;
        }
    }

    private void ClearDrag()
    {
        if (_dragIcon != null)
            Destroy(_dragIcon.gameObject);

        _dragIcon = null;
        _slotView.SetIconVisible(true);
    }

    private void OnDisable()
    {
        _isScrollDrag = false;
        ClearDrag();
    }
}
