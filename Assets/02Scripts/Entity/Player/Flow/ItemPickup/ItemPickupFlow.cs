using UnityEngine;

public class ItemPickupFlow : MonoBehaviour
{
    private PlayerInventoryModule _inventoryModule;

    public void Bind(PlayerInventoryModule p_inventoryModule)
    {
        _inventoryModule = p_inventoryModule;
    }

    public bool Pickup(PickupItemInfo p_pickup)
    {
        if (p_pickup == null || _inventoryModule == null)
            return false;

        
        if (!ItemDatabaseSystem.Instance.TryGetItem(p_pickup.ItemType, p_pickup.ItemId, out ItemDTO item))
        {
            return false;
        }

        if (!_inventoryModule.TryAdd(item, p_pickup.Count))
            return false;

        Destroy(p_pickup.gameObject);
        return true;
    }

    private void OnTriggerEnter(Collider p_other)
    {
        if (p_other.TryGetComponent(out PickupItemInfo pickupItem))
        {
            Pickup(pickupItem);
        }
    }
}

