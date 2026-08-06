using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // ItemType에 해당하는 InventorySlotScrollView 그룹을 관리한다.
    public class InventoryItemPageView : MonoBehaviour
    {
        [Header("Window Type")]
        [SerializeField] private EItemType _itemType;

        [Header("Slot Group Views")]
        [SerializeField]
        private InventorySlotScrollView[] _slotScrollViews;

        public EItemType ItemType => _itemType;

        public event Action<EItemType, int> OnAddSlotRequested;

        // 하위 슬롯 ScrollView 참조를 확보한다.
        private void Awake()
        {
            EnsureSlotScrollViews();
        }

        // 그룹별 ScrollView 이벤트를 연결하고 기존 슬롯 View를 생성한다.
        public void Bind(
            IReadOnlyList<InventorySlot> p_slotList,
            ResourceLoadSystem p_resourceLoader)
        {
            if (!EnsureSlotScrollViews())
            {
                Debug.LogError($"{name}에서 " + $"{nameof(InventorySlotScrollView)}를 찾지 못했습니다.", this);
                return;
            }

            // View 요청 이벤트를 먼저 연결한 뒤 현재 Domain 슬롯을 화면에 반영한다.
            BindScrollViewEvents();

            if (p_slotList == null)
                return;

            foreach (InventorySlot slot in p_slotList)
            {
                AddSlot(slot, p_resourceLoader);
            }
        }

        // AddSlot 대상을 가능한 범위만큼 추가한다.
        public void AddSlot(InventorySlot p_slot, ResourceLoadSystem p_resourceLoader)
        {
            int groupIndex = GetGroupIndex(p_slot);

            if (groupIndex < 0 ||
                groupIndex >= _slotScrollViews.Length)
            {
                return;
            }

            _slotScrollViews[groupIndex]?.AddSlotView(p_slot, p_resourceLoader);
        }

        // Inspector 참조가 없으면 하위 계층에서 ScrollView를 탐색한다.
        private bool EnsureSlotScrollViews()
        {
            if (_slotScrollViews == null || _slotScrollViews.Length == 0)
            {
                _slotScrollViews =GetComponentsInChildren<InventorySlotScrollView>(true);
            }

            return _slotScrollViews != null && _slotScrollViews.Length > 0;
        }

        // 중복 구독을 제거한 뒤 슬롯 추가 요청 이벤트를 연결한다.
        private void BindScrollViewEvents()
        {
            foreach (InventorySlotScrollView scrollView in _slotScrollViews)
            {
                if (scrollView == null)
                    continue;

                scrollView.OnAddSlotRequested -= HandleAddSlotRequested;
                scrollView.OnAddSlotRequested += HandleAddSlotRequested;
            }
        }

        // 요청한 ScrollView의 그룹 번호를 계산해 상위 View에 전달한다.
        private void HandleAddSlotRequested(InventorySlotScrollView p_scrollView)
        {
            int groupIndex = Array.IndexOf(_slotScrollViews, p_scrollView);

            if (groupIndex < 0)
                return;

            OnAddSlotRequested?.Invoke(_itemType, groupIndex);
        }

        // 슬롯 세부 형식에 따라 표시할 ScrollView 그룹 번호를 반환한다.
        private static int GetGroupIndex(InventorySlot p_slot)
        {
            return p_slot switch
            {
                WeaponInventorySlot weaponSlot => (int)weaponSlot.WeaponType,
                ArmorInventorySlot armorSlot => (int)armorSlot.ArmorType,
                CommonInventorySlot => 0,
                _ => -1
            };
        }

        // 객체 해제 시 등록한 이벤트와 참조를 정리한다.
        private void OnDestroy()
        {
            if (_slotScrollViews == null)
                return;

            foreach (InventorySlotScrollView scrollView in _slotScrollViews)
            {
                if (scrollView != null)
                {
                    scrollView.OnAddSlotRequested -= HandleAddSlotRequested;
                }
            }
        }
    }
}
