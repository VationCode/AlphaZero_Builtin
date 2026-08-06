using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alpha.Player.Inventory
{
    // 드래그 중인 아이템 아이콘을 최상위 Canvas 좌표에 표시한다.
    public class InventoryDragIconView : MonoBehaviour
    {
        [Header("Drag Preview")]
        [SerializeField] private RectTransform _dragIconRect;
        [SerializeField] private Image _dragIconImage;

        [SerializeField, Range(0f, 1f)]
        private float _alpha = 0.75f;

        private Canvas _rootCanvas;
        private RectTransform _canvasRect;

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
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

        // Show 화면 요소를 표시한다.
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

        // 포인터 화면 좌표를 루트 Canvas의 로컬 좌표로 변환해 이동한다.
        public void Move(PointerEventData p_eventData)
        {
            if (_dragIconRect == null || _canvasRect == null || _rootCanvas == null || !_dragIconRect.gameObject.activeSelf)
            {
                return;
            }

            // Overlay Canvas는 Camera 없이, 나머지는 포인터 이벤트 Camera로 변환한다.
            Camera eventCamera =_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay? null : p_eventData.pressEventCamera;

            bool converted =
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, p_eventData.position, eventCamera, out Vector2 localPosition);

            if (converted)
            {
                _dragIconRect.anchoredPosition = localPosition;
            }
        }

        // Hide 화면 요소를 숨긴다.
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
