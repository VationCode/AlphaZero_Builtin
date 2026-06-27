using UnityEngine;

public class ItemPickupController : MonoBehaviour
{
    private PlayerCore core;
    public void Bind(PlayerCore p_core)
    {
        core = p_core;
    }

    public void Pickup(PickupItem pickup)
    {
        int itemId = pickup.ItemId;

        core.InventoryModule.AddItem(itemId);

        Destroy(pickup.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PickupItem p_pickupItem))
        {
            Pickup(p_pickupItem);
        }
    }
}

