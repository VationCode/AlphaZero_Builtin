using Alpha.Slot;
using System.Collections.Generic;
using System;
// 1. 슬롯 종류에 맞는 InventoryPageView 선택

namespace Alpha.Inventory
{
    // SlotViewBase → SlotBase로의 전달 역할
    public class InventoryPresenter
    {
        private readonly InventoryModule _inventoryModule;
        private readonly InventoryView _inventoryView;
        private readonly ResourceLoadSystem _resourceLoader;


        // Drag & Drop에 대한SlotView의 요청을 실제 Inventory Slot으로 변환할 때 사용한다.
        private readonly Dictionary<SlotViewBase, SlotBase> _slotDict = new();

        // Slot 추가 버튼에 연결한 람다를 해제하기 위해 보관한다.
        private readonly Dictionary<SlotGroupView, Action> _addSlotHandlerDict = new();

        // Inventory 외부 슬롯이 포함된 Drop 요청을 전달한다.(Equipment -> Inventory로 Drop요청)
        public event Action<SlotViewBase, SlotViewBase> OnExternalDropRequested;

        private bool _isInitialized;

        public InventoryPresenter(InventoryModule p_inventoryModule, InventoryView p_inventoryView, ResourceLoadSystem p_resourceLoader)
        {
            _inventoryModule = p_inventoryModule;
            _inventoryView = p_inventoryView;
            _resourceLoader = p_resourceLoader;
        }


        #region ============================== Initialize

        public void Initialize()
        {
            if (_isInitialized)
                return;

            SetupPage(EItemType.Weapon, EInventoryPage.Weapon);

            SetupPage(EItemType.Armor, EInventoryPage.Armor);

            SetupPage(EItemType.Consumable, EInventoryPage.Consumable);

            SetupPage(EItemType.Material, EInventoryPage.Material);

            SetupPage(EItemType.QuestItem, EInventoryPage.QuestItem);

            _isInitialized = true;
        }

        #endregion ============================== /Initialize

        // Page 조회 및 전체 연결 흐름
        #region ============================== Setup Flow
        private void SetupPage(EItemType p_itemType, EInventoryPage p_pageType)
        {
            if (!_inventoryModule.TryGetPage(p_itemType, out InventoryPage page))
                return;

            InventoryPageView pageView = _inventoryView.GetPage(p_pageType);

            if (pageView == null)
                return;

            SetupSlotGroupList(p_itemType, page, pageView);
        }

        /// <summary>
        /// InventoryPage 내부 SlotGroup을 대응하는 SlotGroupView에 연결한다.
        /// Slot 추가 요청과 기존 Slot View 생성을 함께 처리한다.
        /// </summary>
        private void SetupSlotGroupList(EItemType p_itemType, InventoryPage p_page, InventoryPageView p_pageView)
        {
            if (p_page == null || p_pageView == null)
                return;

            foreach (KeyValuePair<int, SlotGroup> pair in p_page.SlotGroupDict)
            {
                int groupIndex = pair.Key;
                SlotGroup slotGroup = pair.Value;

                if (slotGroup == null)
                    continue;

                if (!p_pageView.TryGetViewGroup(groupIndex, out SlotGroupView slotGroupView))
                    continue;

                // 재초기화 시 기존 Slot 추가 이벤트를 먼저 해제한다.
                if (_addSlotHandlerDict.TryGetValue(slotGroupView, out Action previousHandler))
                {
                    slotGroupView.OnRequestAddSlot -= previousHandler;
                }

                // 람다를 보관해야 Unbind에서 같은 Handler를 해제할 수 있다.
                Action addSlotHandler = () => HandleAddSlotRequest(p_itemType, groupIndex, slotGroupView);

                _addSlotHandlerDict[slotGroupView] = addSlotHandler;

                // 추가 Slot 요청(버튼 이벤트)을 InventoryModule에 전달한다.
                slotGroupView.OnRequestAddSlot += addSlotHandler;

                // 현재 SlotGroup에 포함된 Slot을 View로 생성하고 연결한다.
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
        #endregion ============================== /Setup Flow

        #region ============================== Bind
        // Slot과 SlotView 연결
        private void BindSlot(SlotBase p_slot, SlotViewBase p_slotView)
        {
            if (p_slot == null || p_slotView == null)
                return;

            // UI 요청을 실제 Inventory Slot으로 변환하기 위한 매핑
            _slotDict[p_slotView] = p_slot;

            // 아이콘 로딩 연결
            p_slotView.Bind(_resourceLoader);

            // 슬롯 변경 시 View 갱신을 위한 연결
            p_slot.OnSlotChanged -= p_slotView.SetSlot;
            p_slot.OnSlotChanged += p_slotView.SetSlot;

            // 현재 슬롯 상태 반영
            p_slotView.SetSlot(p_slot.Item, p_slot.Count);

            // Drag & Drop에 대한 이벤트 연결
            if (p_slotView.TryGetComponent(out SlotDragView dragView))
            {
                dragView.OnDropRequested -= HandleDropRequested;
                dragView.OnDropRequested += HandleDropRequested;
            }
        }

        /// <summary>
        /// Slot, SlotGroup, Drag View에 연결한 이벤트를 모두 해제한다.
        /// </summary>
        public void Unbind()
        {
            foreach (KeyValuePair<SlotViewBase, SlotBase> pair in _slotDict)
            {
                SlotViewBase slotView = pair.Key;
                SlotBase slot = pair.Value;

                if (slot != null && slotView != null)
                {
                    slot.OnSlotChanged -= slotView.SetSlot;
                }

                if (slotView != null && slotView.TryGetComponent(out SlotDragView dragView))
                {
                    dragView.OnDropRequested -= HandleDropRequested;
                }
            }

            foreach (KeyValuePair<SlotGroupView, Action> pair in _addSlotHandlerDict)
            {
                if (pair.Key != null)
                {
                    pair.Key.OnRequestAddSlot -= pair.Value;
                }
            }

            _slotDict.Clear();
            _addSlotHandlerDict.Clear();

            // EquipmentPresenter 등의 외부 구독도 제거한다.
            OnExternalDropRequested = null;

            _isInitialized = false;
        }
        #endregion ============================== /Bind

        #region ============================== Request
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

        // Drop 요청
        private void HandleDropRequested(SlotViewBase p_sourceView, SlotViewBase p_targetView)
        {
            bool hasSource = _slotDict.TryGetValue(p_sourceView, out SlotBase source);

            bool hasTarget = _slotDict.TryGetValue(p_targetView, out SlotBase target);

            // Inventory 외부 슬롯이 포함되면 장비 연결 측으로 전달한다.
            if (!hasSource || !hasTarget)
            {
                OnExternalDropRequested?.Invoke(p_sourceView, p_targetView);

                return;
            }

            if (_inventoryModule.TryMergeSlotItem(source, target, out _))
                return;

            _inventoryModule.TrySwapSlotItem(source, target);
        }
        #endregion ============================== /Request

        // View 조회
        #region ============================== Lookup
        /// <summary>
        /// Inventory SlotView에 대응하는 실제 Slot을 조회한다.
        /// Equipment와의 Drag & Drop 연결에서 사용한다.
        /// </summary>
        public bool TryGetSlot(SlotViewBase p_slotView, out SlotBase p_slot)
        {
            if (p_slotView == null)
            {
                p_slot = null;
                return false;
            }

            return _slotDict.TryGetValue(p_slotView, out p_slot);
        }
        #endregion ============================== /Lookup
    }
}