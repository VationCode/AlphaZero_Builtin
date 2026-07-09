using UnityEngine;

public class ItemPickupController : MonoBehaviour
{
    private PlayerCore core;
    public void Bind(PlayerCore p_core)
    {
        core = p_core;
    }

    public void Pickup(PickupItem p_pickup)
    {
        if (ItemDatabaseManger.Instance.TryGetItem(p_pickup.ItemType, p_pickup.ItemId, out ItemDataDTO p_data))
        {
            GameObject prefab =
                ResourceLoadManager.Instance.GetItemPrefab(p_data.ItemType, p_data.PrefabKey);

            Instantiate(prefab, transform.position, Quaternion.identity);
        }
        //core.InventorySystem.AddItem(itemId);

        Destroy(p_pickup.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PickupItem p_pickupItem))
        {
            Pickup(p_pickupItem);
        }
    }
}

