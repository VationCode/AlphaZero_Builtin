using Alpha.Item;
using Alpha.Player.Inventory;
using System;
using UnityEngine;

namespace Alpha.Player
{
    public class ItemPickupFlow : MonoBehaviour
    {
        private PlayerInventoryModule _inventoryModule;
        private ItemDatabaseManager _itemDatabase;

        public void Bind(PlayerInventoryModule p_inventoryModule, ItemDatabaseManager p_itemDatabase)
        {
            _inventoryModule = p_inventoryModule;
            _itemDatabase = p_itemDatabase;
        }

        public bool Pickup(PickupItemInfo p_pickup)
        {
            if (p_pickup == null || _inventoryModule == null || _itemDatabase == null)
                return false;

            if (!_itemDatabase.TryGetItem(p_pickup.ItemType, p_pickup.ItemId, out ItemDTO item))
                return false;

            //int addedCount = _inventoryModule.TryAdd(item, p_pickup.Count);

            //if (addedCount <= 0)
            //    return false;

           // p_pickup.Consume(addedCount);
            return true;
        }

        private void OnTriggerEnter(Collider p_other)
        {
            // Collider가 자식에 있어도 부모의 아이템 정보를 탐색
            PickupItemInfo pickup = p_other.GetComponentInParent<PickupItemInfo>();

            if (pickup == null) return;

            if (!Pickup(pickup)) 
            {
                Debug.LogWarning($"{pickup}에 대한 픽업 실패");
            }
        }
    }
}

