using UnityEngine;
using UnityEngine.UI;

public class SnapToItemScroll : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentPanel;
    [SerializeField] private RectTransform _sampleListItem;

    [SerializeField] private VerticalLayoutGroup _verticalLG;
    [SerializeField] string[] _itemNames;

    [SerializeField] private float _snapStartCurrentPower = 200;  // 클수록 조금만 느려져도 자석처럼 끌려감
    [SerializeField] private float snapTime = 0.015f;    // 높을수록 부드럽게 낮을수록 강하게 강하게 붙음
    int currentItem;
    private float velocity;
    private void Start()
    {
        
    }

    private void Update()
    {
        Debug.Log(currentItem);

        if (_scrollRect.velocity.magnitude < _snapStartCurrentPower)
        {
            float itemHeight = _sampleListItem.rect.height + _verticalLG.spacing;

            currentItem = Mathf.RoundToInt(
                _contentPanel.anchoredPosition.y / itemHeight);

            currentItem = Mathf.Clamp(currentItem, 0, _contentPanel.childCount - 1);

            float targetY = currentItem * itemHeight;

            Vector2 pos = _contentPanel.anchoredPosition;

            pos.y = Mathf.SmoothDamp(pos.y, targetY, ref velocity, snapTime);

            _contentPanel.anchoredPosition = pos;
        }
    }
}
