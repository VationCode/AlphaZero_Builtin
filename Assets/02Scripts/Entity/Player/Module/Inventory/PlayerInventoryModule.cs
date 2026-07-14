using UnityEngine;
using System;

public class PlayerInventoryModule : MonoBehaviour
{
    [SerializeField] private int _weaponSlotCount = 10;
    [SerializeField] private int _armorSlotCount = 10;
    [SerializeField] private int _consumableSlotCount = 10;
    [SerializeField] private int _materialSlotCount = 10;
    [SerializeField] private int _questSlotCount = 10;

    public event Action InventoryChanged;
    public WeaponInventoryModule WeaponInventory { get; private set; }
    public ArmorInventoryModule ArmorInventory { get; private set; }
    public ItemInventoryModule ConsumableInventory { get; private set; }
    public ItemInventoryModule MaterialInventory { get; private set; }
    public ItemInventoryModule QuestInventory { get; private set; }


    private void Awake()
    {
        WeaponInventory =
            new WeaponInventoryModule(_weaponSlotCount);

        ArmorInventory =
            new ArmorInventoryModule(_armorSlotCount);

        ConsumableInventory =
            new ItemInventoryModule(EItemType.Consumable, _consumableSlotCount);

        MaterialInventory =
            new ItemInventoryModule(EItemType.Material, _materialSlotCount);

        QuestInventory =
            new ItemInventoryModule(EItemType.Quest, _questSlotCount);
    }

    public bool TryAdd(ItemDTO p_item, int p_count)
    {
        if (p_item == null || p_count <= 0)
            return false;

        bool isAdded;

        switch (p_item.ItemType)
        {
            case EItemType.Weapon:
                isAdded = WeaponInventory.TryAdd(p_item, p_count);
                break;

            case EItemType.Armor:
                isAdded = ArmorInventory.TryAdd(p_item, p_count);
                break;

            case EItemType.Consumable:
                isAdded = ConsumableInventory.TryAdd(p_item, p_count);
                break;

            case EItemType.Material:
                isAdded = MaterialInventory.TryAdd(p_item, p_count);
                break;

            case EItemType.Quest:
                isAdded = QuestInventory.TryAdd(p_item, p_count);
                break;

            default:
                return false;
        }

        if (isAdded)
            InventoryChanged?.Invoke();

        return isAdded;
    }

    public bool TryRemove(EItemType p_itemType, int p_itemId, int p_count)
    {
        if (p_count <= 0)
            return false;

        bool isRemoved;

        switch (p_itemType)
        {
            case EItemType.Weapon:
                isRemoved =
                    WeaponInventory.TryRemove(p_itemId, p_count);
                break;

            case EItemType.Armor:
                isRemoved =
                    ArmorInventory.TryRemove(p_itemId, p_count);
                break;

            case EItemType.Consumable:
                isRemoved =
                    ConsumableInventory.TryRemove(p_itemId, p_count);
                break;

            case EItemType.Material:
                isRemoved =
                    MaterialInventory.TryRemove(p_itemId, p_count);
                break;

            case EItemType.Quest:
                isRemoved =
                    QuestInventory.TryRemove(p_itemId, p_count);
                break;

            default:
                return false;
        }

        if (isRemoved)
            InventoryChanged?.Invoke();

        return isRemoved;
    }
}
