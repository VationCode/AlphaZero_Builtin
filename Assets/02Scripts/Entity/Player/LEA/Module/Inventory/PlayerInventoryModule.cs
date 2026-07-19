using UnityEngine;

namespace Alpha.Player
{
    public class PlayerInventoryModule : MonoBehaviour
    {
        [SerializeField] private int _weaponSlotCount = 10;
        [SerializeField] private int _armorSlotCount = 10;
        [SerializeField] private int _consumableSlotCount = 10;
        [SerializeField] private int _materialSlotCount = 10;
        [SerializeField] private int _questSlotCount = 10;

        public WeaponInventoryModule WeaponInventory { get; private set; }
        public ArmorInventoryModule ArmorInventory { get; private set; }
        public ItemInventoryModule ConsumableInventory { get; private set; }
        public ItemInventoryModule MaterialInventory { get; private set; }
        public ItemInventoryModule QuestInventory { get; private set; }

        private void Awake()
        {
            WeaponInventory = new WeaponInventoryModule(_weaponSlotCount);
            ArmorInventory = new ArmorInventoryModule(_armorSlotCount);
            ConsumableInventory = new ItemInventoryModule(EItemType.Consumable, _consumableSlotCount);
            MaterialInventory = new ItemInventoryModule(EItemType.Material, _materialSlotCount);
            QuestInventory = new ItemInventoryModule(EItemType.QuestItem, _questSlotCount);
        }

        // Item 종류에 맞는 Inventory에 추가한다.
        public bool TryAdd(ItemDTO p_item, int p_count = 1)
        {
            if (p_item == null)
                return false;


            InventoryModuleBase inventory = GetInventory(p_item.ItemType);

            return inventory != null && inventory.TryAddItem(p_item, p_count);
        }

        public bool TryRemove(EItemType p_itemType, int p_itemId, int p_count = 1)
        {
            InventoryModuleBase inventory = GetInventory(p_itemType);

            return inventory != null && inventory.TryRemoveItem(p_itemId, p_count);
        }

        // Item 종류에 해당하는 Inventory를 반환한다.
        private InventoryModuleBase GetInventory(EItemType p_itemType)
        {
            switch (p_itemType)
            {
                case EItemType.Weapon:
                    return WeaponInventory;

                case EItemType.Armor:
                    return ArmorInventory;

                case EItemType.Consumable:
                    return ConsumableInventory;

                case EItemType.Material:
                    return MaterialInventory;

                case EItemType.QuestItem:
                    return QuestInventory;

                default:
                    return null;
            }
        }
    }
}