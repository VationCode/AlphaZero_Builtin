using System;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    public class InventoryView : MonoBehaviour
    {
        private InventoryContext _context;
        private ResourceLoadSystem _resourceLoader;

        [SerializeField] private GameObject _inventoryRoot;

        [Header("Active Views")]
        [SerializeField] private InventoryActiveView[] _inventoryActiveViews;
        private InventoryActiveView _category;

        [Header("Item Group Views")]
        [SerializeField] private InventoryItemPageView[] _itemPageViews;

        public event Action<EItemType, int> OnAddSlotRequested;

        // Source SlotIndex, Target SlotIndex
        public event Action<int, int> OnTransferRequested;

        // 창 관리
        public event Action OnCloseInventoryRequested;
        public event Action<EItemType> OnPageRequested;

        private void Awake()
        {
            InventoryActiveView[] inventoryViews = GetComponentsInChildren<InventoryActiveView>(true);
            _category = inventoryViews[0];

            _inventoryActiveViews = new InventoryActiveView[inventoryViews.Length-1];

            for (int i = 1; i < inventoryViews.Length; i++)
            {
                _inventoryActiveViews[i-1] = inventoryViews[i];
            }

            _itemPageViews = GetComponentsInChildren<InventoryItemPageView>(true);
        }

        public void Bind(InventoryContext p_context, ResourceLoadSystem p_resourceLoader)
        {
            if (p_context == null)
                return;

            _context = p_context;
            _resourceLoader = p_resourceLoader;

            _context.OnSlotAdded += HandleSlotAdded;

            foreach (InventoryItemPageView itemPageView in _itemPageViews)
            {
                if (itemPageView == null)
                    continue;

                // 슬롯 추가 버튼 요청 연결
                itemPageView.OnAddSlotRequested -= HandleAddSlotRequested;
                itemPageView.OnAddSlotRequested += HandleAddSlotRequested;

                // 아이템 이전 요청 연결
                itemPageView.OnTransferRequested -= HandleTransferRequested;
                itemPageView.OnTransferRequested += HandleTransferRequested;

                if (_context.TryGetSlotList(itemPageView.ItemType, out var slotList))
                {
                    itemPageView.Bind(slotList, _resourceLoader);
                }
            }
        }

        #region ============================== 슬롯 관련 
        private void HandleAddSlotRequested(EItemType p_itemType, int p_groupIndex)
        {
            OnAddSlotRequested?.Invoke(p_itemType, p_groupIndex);
        }

        // Context에 실제 슬롯이 추가되면 해당 View를 생성
        private void HandleSlotAdded(EItemType p_itemType, InventorySlot p_slot)
        {
            foreach (InventoryItemPageView itemGroupView in _itemPageViews)
            {
                if (itemGroupView == null || itemGroupView.ItemType != p_itemType)
                {
                    continue;
                }

                itemGroupView.AddSlot(p_slot, _resourceLoader);

                return;
            }
        }
        #endregion ============================== /슬롯 관련 

        #region ============================== 인벤토리 창 관련
        // Flow와 View 상태 연결
        public void ApplyViewState(bool p_isInventoryOpen, EItemType p_pageType)
        {
            // None일 때만 Category를 표시한다.
            bool showCategory = p_isInventoryOpen && p_pageType == EItemType.None;

            _category.ApplyActive(showCategory);

            // 선택된 ItemType인벤토리만 열고 나머지는 전부 닫는다.
            foreach (InventoryActiveView activeView in _inventoryActiveViews)
            {
                if (activeView == null) continue;

                bool showItemWindow = p_isInventoryOpen && activeView.ItemType == p_pageType;

                activeView.ApplyActive(showItemWindow);
            }

            // Root는 마지막에 처리한다.
            _inventoryRoot.SetActive(p_isInventoryOpen);
        }


        // 버튼을 통한 On/Off
        public void RequestPage(int p_itemTypeValue)
        {
            if (!Enum.IsDefined(typeof(EItemType), p_itemTypeValue))
                return;

            EItemType itemType = (EItemType)p_itemTypeValue;

            OnPageRequested?.Invoke(itemType);
        }

        public void RequestCloseInventory()
        {
            OnCloseInventoryRequested?.Invoke();
        }
        #endregion ============================== /인벤토리 창 관련

        #region ============================== Drag & Drop 관리

        private void HandleTransferRequested(int p_sourceSlotIndex, int p_targetSlotIndex)
        {
            // 판단 없이 외부 Flow 방향으로 전달
            OnTransferRequested?.Invoke(p_sourceSlotIndex, p_targetSlotIndex);
        }

        #endregion ============================== /Drag & Drop 관리

        private void OnDestroy()
        {
            if (_context != null)
            {
                _context.OnSlotAdded -= HandleSlotAdded;
            }

            foreach (InventoryItemPageView itemGroupView in _itemPageViews)
            {
                if (itemGroupView != null)
                {
                    itemGroupView.OnAddSlotRequested -= HandleAddSlotRequested;
                    itemGroupView.OnTransferRequested -= HandleTransferRequested;
                }
            }
        }
    }
}