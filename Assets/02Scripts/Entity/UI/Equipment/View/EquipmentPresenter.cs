using Alpha.Slot;
using Alpha.Inventory;

using System.Collections.Generic;

namespace Alpha.Equipment
{
    // Equipment 상태와 Page View를 연결한다.
    public class EquipmentPresenter
    {
        private readonly EquipmentModule _module;
        private readonly EquipmentView _view;
        private readonly ResourceLoadSystem _resourceLoader;
        private InventoryPresenter _inventoryPresenter;

        private readonly Dictionary<SlotViewBase, SlotBase> _slotDict = new();

        private bool _isInitialized;

        public EquipmentPresenter(EquipmentModule p_module, EquipmentView p_view, ResourceLoadSystem p_resourceLoader)
        {
            _module = p_module;
            _view = p_view;
            _resourceLoader = p_resourceLoader;
        }

        public void Initialize()
        {
            if (_isInitialized || !_module.IsInitialized)
                return;

            SetupWeaponPage();
            SetupArmorPage();

            _isInitialized = true;
        }

        private void SetupWeaponPage()
        {
            if (!_view.TryGetWeaponPage(out EquipmentWeaponPageView pageView))
            {
                return;
            }

            BindWeaponSlot(EWeaponType.Melee, pageView);
            BindWeaponSlot(EWeaponType.Range, pageView);
            BindWeaponSlot(EWeaponType.Special, pageView);
        }

        private void SetupArmorPage()
        {
            if (!_view.TryGetArmorPage(out EquipmentArmorPageView pageView))
            {
                return;
            }

            BindArmorSlot(EArmorType.Helmet, pageView);
            BindArmorSlot(EArmorType.Chest, pageView);
            BindArmorSlot(EArmorType.Gloves, pageView);
            BindArmorSlot(EArmorType.Boots, pageView);
        }

        #region ============================== Bind
        // Inventory Presenter 연결 함수
        public void BindInventory(InventoryPresenter p_inventoryPresenter)
        {
            if (p_inventoryPresenter == null)
                return;

            // 재연결 시 이전 이벤트 구독을 제거한다.
            if (_inventoryPresenter != null)
            {
                _inventoryPresenter.OnExternalDropRequested -= HandleDropRequested;
            }

            _inventoryPresenter = p_inventoryPresenter;

            _inventoryPresenter.OnExternalDropRequested -= HandleDropRequested;

            _inventoryPresenter.OnExternalDropRequested += HandleDropRequested;
        }

        private void BindWeaponSlot(EWeaponType p_type, EquipmentWeaponPageView p_pageView)
        {
            if (!_module.TryGetWeaponSlot(p_type, out EquipmentWeaponSlot slot))
                return;

            if (!p_pageView.TryGetSlot(p_type, out SlotViewBase slotView))
                return;

            BindSlot(slot, slotView);
        }

        private void BindArmorSlot(EArmorType p_type, EquipmentArmorPageView p_pageView)
        {
            if (!_module.TryGetArmorSlot(p_type, out EquipmentArmorSlot slot))
                return;

            if (!p_pageView.TryGetSlot(p_type, out SlotViewBase slotView))
                return;

            BindSlot(slot, slotView);
        }

        private void BindSlot(SlotBase p_slot, SlotViewBase p_slotView)
        {
            // Drop 요청을 실제 Equipment Slot으로 변환하기 위한 매핑
            _slotDict[p_slotView] = p_slot;

            // 아이콘 로딩과 슬롯 변경 상태를 View에 연결한다.
            p_slotView.Bind(_resourceLoader);

            p_slot.OnSlotChanged -= p_slotView.SetSlot;
            p_slot.OnSlotChanged += p_slotView.SetSlot;

            // 현재 장비 상태를 최초 한 번 표시한다.
            p_slotView.SetSlot(p_slot.Item, p_slot.Count);

            // 장비 슬롯이 Drop Target인 경우의 요청을 받는다.
            if (p_slotView.TryGetComponent(out SlotDragView dragView))
            {
                dragView.OnDropRequested -= HandleDropRequested;
                dragView.OnDropRequested += HandleDropRequested;
            }
        }

        /// <summary>
        /// Inventory, Slot, Drag View에 연결한 이벤트를 모두 해제한다.
        /// </summary>
        public void Unbind()
        {
            if (_inventoryPresenter != null)
            {
                _inventoryPresenter.OnExternalDropRequested -= HandleDropRequested;
                _inventoryPresenter = null;
            }

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

            _slotDict.Clear();
            _isInitialized = false;
        }

        #endregion ============================== /Bind

        private void HandleDropRequested(SlotViewBase p_sourceView, SlotViewBase p_targetView)
        {
            if (!_isInitialized || _inventoryPresenter == null)
                return;

            if (!TryResolveSlot(p_sourceView, out SlotBase source))
                return;

            if (!TryResolveSlot(p_targetView, out SlotBase target))
                return;

            _module.TrySwapSlotItem(source, target);
        }

        // Dict 전체 탐색
        private bool TryResolveSlot(SlotViewBase p_slotView, out SlotBase p_slot)
        {
            if (_slotDict.TryGetValue(p_slotView, out p_slot))
                return true;

            // 장비 슬롯이 아니면 Inventory 슬롯에서 조회한다.
            return _inventoryPresenter.TryGetSlot(p_slotView, out p_slot);
        }
    }
}
