using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Timeline.TimelinePlaybackControls;

namespace Alpha.Slot
{
    public class SlotGroupView : MonoBehaviour
    {
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private SlotViewBase _slotPrefab;
        [SerializeField] private ScrollRect _scrollRect;

        public List<SlotViewBase> SlotViewList => _slotViewList;
        private readonly List<SlotViewBase> _slotViewList = new();

        public event Action OnRequestAddSlot;

        private void OnEnable()
        {
            RefreshScroll();
        }

        // SlotView 단일 생성
        // 로직 생성 성공 후 Presenter가 호출
        public SlotViewBase AddSlot()
        {
            if (_contentRoot == null || _slotPrefab == null)
                return null;

            SlotViewBase slotView = Instantiate(_slotPrefab, _contentRoot);

            slotView.Clear();
            _slotViewList.Add(slotView);

            return slotView;
        }

        // AddSlotBtn에서 호출 (AddSlot)
        public void RequestAddSlot()
        {
            OnRequestAddSlot?.Invoke();
        }

        private void RefreshScroll()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);

            _scrollRect.StopMovement();
            _scrollRect.velocity = Vector2.zero;

            // 가로 스크롤의 맨 왼쪽
            _scrollRect.horizontalNormalizedPosition= 0f;
        }
    }
}