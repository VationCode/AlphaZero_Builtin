using UnityEngine;
using UnityEngine.UI;

// Scroll 속도가 낮아지면 가장 가까운 세로 목록 항목으로 Content를 정렬한다.
public class SnapToItemScroll : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentPanel;
    [SerializeField] private RectTransform _sampleListItem;

    [SerializeField] private VerticalLayoutGroup _verticalLG;
    [SerializeField] string[] _itemNames;

    [SerializeField] private float _offset;
    [SerializeField] private float _snapStartCurrentPower = 200;  // 클수록 조금만 느려져도 자석처럼 끌려감
    [SerializeField] private float snapTime = 0.015f;    // 높을수록 부드럽게 낮을수록 강하게 강하게 붙음
    int currentItem;
    private float velocity;
    // 현재 별도의 시작 초기화는 없다.
    private void Start()
    {
        
    }

    // Scroll 감속 시 가장 가까운 항목의 위치로 부드럽게 보정한다.
    private void Update()
    {
        if (_scrollRect.velocity.magnitude < _snapStartCurrentPower)
        {
            // 항목 높이와 간격을 합쳐 현재 가장 가까운 Index를 계산한다.
            float itemHeight = _sampleListItem.rect.height + _verticalLG.spacing;

            currentItem = Mathf.RoundToInt((_contentPanel.anchoredPosition.y - _offset) / itemHeight);

            currentItem = Mathf.Clamp(currentItem, 0, _contentPanel.childCount - 1);

            // 계산한 항목의 기준 위치까지 Content를 SmoothDamp로 이동한다.
            float targetY = _offset + (currentItem * itemHeight);

            Vector2 pos = _contentPanel.anchoredPosition;

            pos.y = Mathf.SmoothDamp(pos.y, targetY, ref velocity, snapTime);

            _contentPanel.anchoredPosition = pos;
        }
    }
}
