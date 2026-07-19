using Alpha.Player;
using System;
using System.Collections.Generic;

namespace Alpha.Inventory
{
    public class InventoryPresenter
    {
        private readonly PlayerInventoryModule _playerInventory;
        private readonly InventoryView _inventoryView;
        private readonly ResourceLoadSystem _resourceLoader;
        private readonly SlotTransferSystem _transferSystem;

        private readonly List<SlotPresenter> _slotPresenters = new();
        public bool IsOpen => _inventoryView.IsOpen;
        
        public event Action<bool> OpenStateChanged;

        // 생성자
        public InventoryPresenter(PlayerInventoryModule p_playerInventory, InventoryView p_inventoryView,
                                  ResourceLoadSystem p_resourceLoader, SlotTransferSystem p_transferSystem)
        {
            _inventoryView = p_inventoryView;

            _playerInventory = p_playerInventory;
            _inventoryView = p_inventoryView;
            _resourceLoader = p_resourceLoader;
            _transferSystem = p_transferSystem;

            _inventoryView.OpenStateChanged += OnOpenStateChanged;
        }

        public void Initialize()
        {
            CreateWeaponSlots();
            CreateArmorSlots();

            CreateItemSlots(_playerInventory.ConsumableInventory, _inventoryView.ConsumableView);

            CreateItemSlots(_playerInventory.MaterialInventory, _inventoryView.MaterialView);

            CreateItemSlots(_playerInventory.QuestInventory, _inventoryView.QuestView);
        }

        // Inventory 화면을 열거나 닫는다.
        public bool ToggleWindow()
        {
            if (_inventoryView.IsOpen)
            {
                _inventoryView.CloseView();
            }
            else
            {
                _inventoryView.OpenView((int)EInventoryView.Category);
            }

            return _inventoryView.IsOpen;
        }

        // Weapon Slot UI를 생성한다.
        private void CreateWeaponSlots()
        {
            foreach (SlotBase slot in _playerInventory.WeaponInventory.SlotList)
            {
                WeaponSlot weaponSlot = (WeaponSlot)slot;

                SlotBaseUI slotView = _inventoryView.WeaponView.CreateSlotView(weaponSlot.WeaponType);

                CreateSlotPresenter(weaponSlot,slotView);
            }
        }

        // Armor Slot UI를 생성한다.
        private void CreateArmorSlots()
        {
            foreach (SlotBase slot in _playerInventory.ArmorInventory.SlotList)
            {
                ArmorSlot armorSlot = (ArmorSlot)slot;

                SlotBaseUI slotView = _inventoryView.ArmorView.CreateSlotView(armorSlot.ArmorType);

                CreateSlotPresenter(armorSlot, slotView);
            }
        }

        // 일반 Item Slot UI를 생성한다.
        private void CreateItemSlots(ItemInventoryModule p_inventory, ItemInventoryView p_view)
        {
            foreach (SlotBase slot in p_inventory.SlotList)
            {
                CreateSlotPresenter(
                    slot,
                    p_view.CreateSlotView());
            }
        }

        // Slot과 Slot UI를 연결한다.
        private void CreateSlotPresenter(SlotBase p_slot, SlotBaseUI p_view)
        {
            SlotPresenter presenter =
                new SlotPresenter(p_slot, p_view, _resourceLoader, _transferSystem);

            _slotPresenters.Add(presenter);
        }

        // Inventory 열림 상태를 외부에 전달한다.
        private void OnOpenStateChanged(bool p_isOpen)
        {
            OpenStateChanged?.Invoke(p_isOpen);
        }
    }
}