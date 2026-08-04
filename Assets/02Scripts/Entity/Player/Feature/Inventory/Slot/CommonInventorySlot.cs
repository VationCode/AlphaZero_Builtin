using UnityEngine;
namespace Alpha.Player.Inventory
{
    public class CommonInventorySlot : InventorySlot
    {
        public EItemType ItemType { get; }

        public CommonInventorySlot(int p_index, EItemType p_itemType) : base(p_index)
        {
            ItemType = p_itemType;
        }

        public override bool CanStore(ItemDTO p_item)
        {
            return p_item != null &&
                   p_item.ItemType == ItemType;
        }
    }
}
