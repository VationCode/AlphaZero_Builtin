using Alpha.UI.Inventory;
using System.Collections.Generic;

// 1. 슬롯 종류에 맞는 InventoryPageView 선택

namespace Alpha.Player.Inventory
{
    public class PlayerInventoryPresenter
    {
        private readonly PlayerInventoryModule _inventoryModule;
        private readonly PlayerInventoryView _inventoryView;

        private readonly Dictionary<InventoryPage, InventoryPageView> _pageViewDict = new();
        private readonly Dictionary<SlotGroup, SlotGroupView> _slotGroupViewDict = new();
        private readonly Dictionary<SlotBase, SlotViewBase> _slotViewDict = new();

        private bool _isInitialized;


        public PlayerInventoryPresenter(PlayerInventoryModule p_inventoryModule, PlayerInventoryView p_inventoryView)
        {
            _inventoryModule = p_inventoryModule;
            _inventoryView = p_inventoryView;
        }


        #region ======================================== Initialize

        public void Initialize()
        {
            if (_isInitialized)
                return;

            _inventoryModule.Initialize();

            SetupPage(EItemType.Weapon, EInventoryPage.Weapon);

            SetupPage(EItemType.Armor, EInventoryPage.Armor);

            SetupPage(EItemType.Consumable, EInventoryPage.Consumable);

            SetupPage(EItemType.Material, EInventoryPage.Material);

            SetupPage(EItemType.QuestItem, EInventoryPage.QuestItem);

            _isInitialized = true;
        }

        #endregion ======================================== /Initialize

        // Page 조회 및 전체 연결 흐름
        #region ======================================== Setup Flow
        private void SetupPage(EItemType p_itemType, EInventoryPage p_pageType)
        {
            if (!_inventoryModule.TryGetPage(p_itemType, out InventoryPage page))
                return;

            InventoryPageView pageView = _inventoryView.GetPage(p_pageType);

            if (pageView == null)
                return;

            BindPage(page, pageView);

            SetupSlotGroupList(p_itemType, page, pageView);
        }

        // Page 내부 SlotGroup 연결 흐름
        private void SetupSlotGroupList(EItemType p_itemType, InventoryPage p_page, InventoryPageView p_pageView)
        {
            foreach (KeyValuePair<int, SlotGroup> pair in p_page.SlotGroupDict)
            {
                int groupIndex = pair.Key;
                SlotGroup slotGroup = pair.Value;

                if (!p_pageView.TryGetViewGroup(groupIndex, out SlotGroupView slotGroupView))
                    continue;

                BindSlotGroup(slotGroup, slotGroupView);

                // AddSlotBtn 요청 연결
                slotGroupView.OnRequestAddSlot += () => HandleAddSlotRequest(p_itemType,groupIndex, slotGroupView);

                SetupSlotList(slotGroup, slotGroupView);
            }
        }

        // SlotGroup 내부 Slot 연결 흐름
        private void SetupSlotList(SlotGroup p_slotGroup, SlotGroupView p_slotGroupView)
        {
            foreach (SlotBase slot in p_slotGroup.SlotList)
            {
                SlotViewBase slotView = p_slotGroupView.AddSlot();

                if (slotView == null)
                    continue;

                BindSlot(slot, slotView);
            }
        }
        #endregion ======================================== /Setup Flow

        // Page와 PageView 연결
        #region ======================================== Bind
        private void BindPage(InventoryPage p_page, InventoryPageView p_pageView)
        {
            if (p_page == null || p_pageView == null)
                return;

            _pageViewDict[p_page] = p_pageView;
        }

        // SlotGroup과 SlotGroupView 연결
        private void BindSlotGroup(SlotGroup p_slotGroup, SlotGroupView p_slotGroupView)
        {
            if (p_slotGroup == null || p_slotGroupView == null)
                return;

            _slotGroupViewDict[p_slotGroup] = p_slotGroupView;
        }

        // Slot과 SlotView 연결
        private void BindSlot(SlotBase p_slot, SlotViewBase p_slotView)
        {
            if (p_slot == null || p_slotView == null)
                return;

            _slotViewDict[p_slot] = p_slotView;
        }
        #endregion ======================================== /Bind

        // AddSlotBtn 요청 처리
        #region ======================================== Request Add Slot
        private void HandleAddSlotRequest(EItemType p_itemType, int p_groupIndex, SlotGroupView p_slotGroupView)
        {
            // 논리 Slot 생성 및 SlotGroup 추가
            SlotBase slot = _inventoryModule.AddSlot(p_itemType, p_groupIndex);

            if (slot == null)
                return;

            // SlotView 생성
            SlotViewBase slotView = p_slotGroupView.AddSlot();

            if (slotView == null)
                return;

            BindSlot(slot, slotView);
        }

        #endregion ======================================== /Request Add Slot

        // View 조회
        #region Lookup
        public bool TryGetPageView(InventoryPage p_page, out InventoryPageView p_pageView)
        {
            return _pageViewDict.TryGetValue(
                p_page,
                out p_pageView);
        }

        public bool TryGetSlotGroupView(SlotGroup p_slotGroup, out SlotGroupView p_slotGroupView)
        {
            return _slotGroupViewDict.TryGetValue(p_slotGroup, out p_slotGroupView);
        }

        public bool TryGetSlotView(SlotBase p_slot, out SlotViewBase p_slotView)
        {
            return _slotViewDict.TryGetValue(p_slot, out p_slotView);
        }

        #endregion
    }
}