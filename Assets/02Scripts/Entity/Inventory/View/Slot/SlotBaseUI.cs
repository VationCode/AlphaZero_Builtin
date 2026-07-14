using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotBaseUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private ScrollRect _scrollRect;

    [SerializeField]
    private Image _itemIcon;

    [SerializeField]
    private TMP_Text _countText;

    [SerializeField]
    private Vector2 _dragIconSize = new Vector2(80f, 80f);

    private ISlotDragHandler _dragHandler;

    private Canvas _rootCanvas;
    private Image _dragIcon;

    private bool _isScrollDrag;
    private void Awake()
    {
        _scrollRect = GetComponentInParent<ScrollRect>();
    }
    public void BindDragHandler(ISlotDragHandler p_dragHandler)
    {
        _dragHandler = p_dragHandler;
    }

    public void Show(Sprite p_icon, int p_count)
    {
        _itemIcon.sprite = p_icon;
        _itemIcon.enabled = p_icon != null;

        if (p_count > 1)
            _countText.text = p_count.ToString();
        else
            _countText.text = string.Empty;
    }

    public void Clear()
    {
        _itemIcon.sprite = null;
        _itemIcon.enabled = false;
        _countText.text = string.Empty;
    }

    public void OnBeginDrag(PointerEventData p_eventData)
    {
        if (_itemIcon.sprite == null)
        {
            if (_scrollRect == null)
                return;
            _isScrollDrag = true;
            _scrollRect.OnBeginDrag(p_eventData);
            return;
        }

        _isScrollDrag = false;

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            return;

        _rootCanvas = canvas.rootCanvas;

        _dragIcon = Instantiate(_itemIcon, _rootCanvas.transform);

        _dragIcon.name = "DragIcon";
        _dragIcon.raycastTarget = false;
        _dragIcon.preserveAspect = false;

        RectTransform dragRect = _dragIcon.rectTransform;

        dragRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragRect.pivot = new Vector2(0.5f, 0.5f);
        _dragIcon.rectTransform.sizeDelta = _dragIconSize;
        dragRect.localScale = Vector3.one;
        dragRect.SetAsLastSibling();

        _itemIcon.enabled = false;
        MoveDragIcon(p_eventData);
    }

    public void OnDrag(PointerEventData p_eventData)
    {
        if (_isScrollDrag)
        {
            _scrollRect.OnDrag(p_eventData);
            return;
        }

        MoveDragIcon(p_eventData);
    }

    public void OnDrop(PointerEventData p_eventData)
    {
        if (p_eventData.pointerDrag == null)
            return;

        SlotBaseUI sourceView =
            p_eventData.pointerDrag.GetComponent<SlotBaseUI>();

        if (sourceView == null || sourceView == this)
            return;

        sourceView._dragHandler?.TryMoveTo(_dragHandler);
    }

    public void OnEndDrag(PointerEventData p_eventData)
    {
        if (_isScrollDrag)
        {
            _scrollRect.OnEndDrag(p_eventData);
            _isScrollDrag = false;
            return;
        }

        if (_dragIcon != null)
            Destroy(_dragIcon.gameObject);

        _dragIcon = null;
        _rootCanvas = null;

        if (_itemIcon.sprite != null)
            _itemIcon.enabled = true;
    }

    private void MoveDragIcon(PointerEventData p_eventData)
    {
        if (_dragIcon == null || _rootCanvas == null)
            return;

        RectTransform canvasRect =
            _rootCanvas.transform as RectTransform;

        Camera eventCamera = null;

        if (_rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = _rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            p_eventData.position,
            eventCamera,
            out Vector2 localPosition))
            return;

        _dragIcon.rectTransform.anchoredPosition = localPosition;
    }
}
