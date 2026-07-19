using System.Collections.Generic;

namespace Alpha.Inventory
{
    public class ItemInventoryPresenter
    {
        private readonly ItemInventoryModule _inventory;
        private readonly ItemInventoryView _view;
        private readonly ResourceLoadSystem _resourceLoader;

        private readonly List<SlotPresenter> _slotPresenters = new();
        private readonly SlotTransferSystem _transferSystem;
        public ItemInventoryPresenter
            (ItemInventoryModule p_inventory,
            ItemInventoryView p_view,
            ResourceLoadSystem p_resourceLoader,
            SlotTransferSystem p_transferSystem)
        {
            _inventory = p_inventory;
            _view = p_view;
            _resourceLoader = p_resourceLoader;
            _transferSystem = p_transferSystem;
        }

        public void Initialize()
        {
            foreach (SlotBase slot in _inventory.SlotList)
            {
                if (slot is not ItemSlot itemSlot)
                    continue;

                SlotBaseUI slotView = _view.CreateSlotView();

                if (slotView == null)
                    continue;

                SlotPresenter presenter =
                    new SlotPresenter(itemSlot, slotView, _resourceLoader, _transferSystem);

                _slotPresenters.Add(presenter);
                presenter.Refresh();
            }
        }
    }
}
