using System.Collections.Generic;

namespace Alpha.Inventory
{
    public class ArmorInventoryPresenter
    {
        private readonly ArmorInventoryModule _inventory;
        private readonly ArmorInventoryView _view;
        private readonly ResourceLoadSystem _resourceLoader;

        private readonly List<SlotPresenter> _slotPresenters = new();
        private readonly SlotTransferSystem _transferSystem;
        public ArmorInventoryPresenter(
            ArmorInventoryModule p_inventory,
            ArmorInventoryView p_view,
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
                if (slot is not ArmorSlot armorSlot)
                    continue;

                SlotBaseUI slotView =
                    _view.CreateSlotView(armorSlot.ArmorType);

                if (slotView == null)
                    continue;

                SlotPresenter presenter =
                    new SlotPresenter(armorSlot, slotView, _resourceLoader, _transferSystem);

                _slotPresenters.Add(presenter);
                presenter.Refresh();
            }
        }
    }
}