using Alpha.UI;
using System;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    [Serializable]
    public sealed class ItemWindowActiveEntry
    {
        [SerializeField] private EItemType _itemType;
        [SerializeField] private InventoryActiveView _activeView;

        public EItemType ItemType => _itemType;
        public InventoryActiveView ActiveView => _activeView;
    }

    public class InventoryView : MonoBehaviour
    {
        private InventoryContext _context;
        private ResourceLoadSystem _resourceLoader;

        [Header("Active Views")]
        [SerializeField] private InventoryActiveView _inventoryRoot;
        [SerializeField] private InventoryActiveView _category;
        [SerializeField] private ItemWindowActiveEntry[] _itemWindowActives;

        [Header("Item Group Views")]
        [SerializeField] private InventoryItemGroupView[] _itemGroupViews;

        public event Action<EItemType, int> OnAddSlotRequested;

        public event Action OnCloseInventoryRequested;
        public event Action<EItemType> OnPageRequested;

        public void Bind(InventoryContext p_context, ResourceLoadSystem p_resourceLoader)
        {
            if (p_context == null)
                return;

            _context = p_context;
            _resourceLoader = p_resourceLoader;

            _context.OnSlotAdded += HandleSlotAdded;

            foreach (InventoryItemGroupView itemGroupView in _itemGroupViews)
            {
                if (itemGroupView == null)
                    continue;

                // 슬롯 추가 버튼 요청 연결
                itemGroupView.OnAddSlotRequested -= HandleAddSlotRequested;
                itemGroupView.OnAddSlotRequested += HandleAddSlotRequested;

                if (_context.TryGetSlotList(itemGroupView.ItemType, out var slotList))
                {
                    itemGroupView.Bind(slotList, _resourceLoader);
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
            foreach (InventoryItemGroupView itemGroupView in _itemGroupViews)
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
            foreach (ItemWindowActiveEntry entry in _itemWindowActives)
            {
                if (entry.ActiveView == null) continue;

                bool showItemWindow = p_isInventoryOpen && entry.ItemType == p_pageType;

                entry.ActiveView.ApplyActive(showItemWindow);
            }

            // Root는 마지막에 처리한다.
            _inventoryRoot.ApplyActive(p_isInventoryOpen);
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

        private void OnDestroy()
        {
            if (_context != null)
            {
                _context.OnSlotAdded -= HandleSlotAdded;
            }

            foreach (InventoryItemGroupView itemGroupView in _itemGroupViews)
            {
                if (itemGroupView != null)
                {
                    itemGroupView.OnAddSlotRequested -= HandleAddSlotRequested;
                }
            }
        }
    }
}