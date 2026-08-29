using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // ItemType 페이지 안의 카테고리별 SlotScrollView를 관리한다.
    public class InventoryItemPageView : MonoBehaviour
    {
        [Header("Window Type")]
        [SerializeField] private EItemType _itemType;

        [Header("Category Slot Views")]
        [SerializeField]
        private InventorySlotScrollView[] _slotScrollViews;

        public EItemType ItemType => _itemType;
        public int SelectedCategoryIndex { get; private set; }

        public event Action<EItemType, int> OnAddSlotRequested;

        // 하위 슬롯 ScrollView 참조를 확보한다.
        private void Awake()
        {
            EnsureSlotScrollViews();
        }

        // 페이지를 열면 선택한 상단 카테고리의 슬롯만 표시한다.
        private void OnEnable()
        {
            ApplySelectedCategory();
        }

        // 카테고리별 ScrollView 이벤트를 연결하고 기존 SlotView를 생성한다.
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

            ApplySelectedCategory();
        }

        // 상단 탭이 전달한 번호를 현재 페이지의 선택 Category로 적용한다.
        public void RequestSelectCategory(int p_categoryIndex)
        {
            if (!EnsureSlotScrollViews() ||
                p_categoryIndex < 0 ||
                p_categoryIndex >= _slotScrollViews.Length)
            {
                return;
            }

            SelectedCategoryIndex = p_categoryIndex;
            ApplySelectedCategory();
        }

        // AddSlot 대상을 가능한 범위만큼 추가한다.
        public void AddSlot(InventorySlot p_slot, ResourceLoadSystem p_resourceLoader)
        {
            int categoryIndex = GetCategoryIndex(p_slot);

            if (categoryIndex < 0 ||
                categoryIndex >= _slotScrollViews.Length)
            {
                return;
            }

            _slotScrollViews[categoryIndex]?.AddSlotView(p_slot, p_resourceLoader);
        }

        // Inspector 참조가 없으면 하위 계층에서 ScrollView를 탐색한다.
        private bool EnsureSlotScrollViews()
        {
            if (_slotScrollViews == null || _slotScrollViews.Length == 0)
            {
                _slotScrollViews = GetComponentsInChildren<InventorySlotScrollView>(true);
            }

            return _slotScrollViews != null && _slotScrollViews.Length > 0;
        }

        // 선택한 Category의 ScrollView 하나만 활성화한다.
        private void ApplySelectedCategory()
        {
            if (!EnsureSlotScrollViews())
            {
                return;
            }

            SelectedCategoryIndex = Mathf.Clamp(
                SelectedCategoryIndex,
                0,
                _slotScrollViews.Length - 1);

            for (int index = 0;
                 index < _slotScrollViews.Length;
                 index++)
            {
                InventorySlotScrollView scrollView =
                    _slotScrollViews[index];

                if (scrollView != null)
                    scrollView.gameObject.SetActive(
                        index == SelectedCategoryIndex);
            }
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

        // 요청한 ScrollView의 카테고리 번호를 계산해 상위 View에 전달한다.
        private void HandleAddSlotRequested(InventorySlotScrollView p_scrollView)
        {
            int categoryIndex = Array.IndexOf(_slotScrollViews, p_scrollView);

            if (categoryIndex < 0)
                return;

            OnAddSlotRequested?.Invoke(_itemType, categoryIndex);
        }

        // 슬롯 세부 형식에 따라 표시할 카테고리 ScrollView 번호를 반환한다.
        private static int GetCategoryIndex(InventorySlot p_slot)
        {
            return p_slot switch
            {
                WeaponInventorySlot weaponSlot =>
                    (int)weaponSlot.WeaponCategory,
                ArmorInventorySlot armorSlot => (int)armorSlot.ArmorType,
                ConsumableInventorySlot consumableSlot =>
                    (int)consumableSlot.ConsumableType,
                MaterialInventorySlot materialSlot =>
                    (int)materialSlot.MaterialType,
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
