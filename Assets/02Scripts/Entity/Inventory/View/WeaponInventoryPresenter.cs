using System.Collections.Generic;

namespace Alpha.Inventory
{
    public class WeaponInventoryPresenter
    {
        private readonly WeaponInventoryModule _inventory;
        private readonly WeaponInventoryView _view;
        private readonly ResourceLoadSystem _resourceLoader;

        private readonly List<SlotPresenter> _slotPresenters = new();
        private readonly SlotTransferSystem _transferSystem;
        public WeaponInventoryPresenter(
                                        WeaponInventoryModule p_inventory,
                                        WeaponInventoryView p_view,
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
                if (slot is not WeaponSlot weaponSlot)
                    continue;

                SlotBaseUI slotView =
                    _view.CreateSlotView(weaponSlot.WeaponType);

                if (slotView == null)
                    continue;

                SlotPresenter presenter =
                    new SlotPresenter(weaponSlot, slotView, _resourceLoader, _transferSystem);

                _slotPresenters.Add(presenter);
                presenter.Refresh();
            }
        }
    }
}
