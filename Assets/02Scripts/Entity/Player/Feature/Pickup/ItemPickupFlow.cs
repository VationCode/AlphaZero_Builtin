using Alpha.Item;
using System;
using UnityEngine;

namespace Alpha.Player
{
    public class ItemPickupFlow : MonoBehaviour
    {
        private ItemDatabaseManager _itemDatabase;

        public void Bind(ItemDatabaseManager p_itemDatabase)
        {
            _itemDatabase = p_itemDatabase;
        }

        public bool Pickup(PickupItemInfo p_pickup)
        {
            if (p_pickup == null || p_pickup.Count <= 0 ||  _itemDatabase == null)
            {
                return false;
            }

            if (!_itemDatabase.TryGetItem(p_pickup.ItemType, p_pickup.ItemId, out ItemDTO item))
                return false;

            // 인벤토리에 추가된 수량만 월드 아이템에서 차감
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

