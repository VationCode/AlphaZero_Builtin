using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alpha.Player.Inventory
{
    public class InventoryDragIconView : MonoBehaviour
    {
        [Header("Drag Preview")]
        [SerializeField] private RectTransform _dragIconRect;
        [SerializeField] private Image _dragIconImage;

        [SerializeField, Range(0f, 1f)]
        private float _alpha = 0.75f;

        private Canvas _rootCanvas;
        private RectTransform _canvasRect;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            _canvasRect = _rootCanvas != null? _rootCanvas.transform as RectTransform : null;

            if (_dragIconImage != null)
            {
                // Drop 슬롯을 감지할 수 있도록 Raycast를 막지 않는다.
                _dragIconImage.raycastTarget = false;

                Color color = _dragIconImage.color;
                color.a = _alpha;
                _dragIconImage.color = color;
            }

            Hide();
        }

        public void Show(Sprite p_icon, PointerEventData p_eventData)
        {
            if (p_icon == null || _dragIconRect == null || _dragIconImage == null)
            {
                return;
            }

            _dragIconImage.sprite = p_icon;
            _dragIconRect.gameObject.SetActive(true);

            // 다른 UI보다 위에 표현한다.
            _dragIconRect.SetAsLastSibling();

            Move(p_eventData);
        }

        public void Move(PointerEventData p_eventData)
        {
            if (_dragIconRect == null || _canvasRect == null || _rootCanvas == null || !_dragIconRect.gameObject.activeSelf)
            {
                return;
            }

            Camera eventCamera =_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay? null : p_eventData.pressEventCamera;

            bool converted =
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, p_eventData.position, eventCamera, out Vector2 localPosition);

            if (converted)
            {
                _dragIconRect.anchoredPosition = localPosition;
            }
        }

        public void Hide()
        {
            if (_dragIconImage != null)
            {
                _dragIconImage.sprite = null;
            }

            if (_dragIconRect != null)
            {
                _dragIconRect.gameObject.SetActive(false);
            }
        }
    }
}
