namespace Alpha.Player.Inventory
{
    public class CommonSlot : SlotBase
    {
        public EItemType ItemType { get; }

        public CommonSlot(EItemType p_itemType)
        {
            ItemType = p_itemType;
        }

        public override bool CanStore(ItemDTO p_item)
        {
            return p_item != null;
                //&& p_item.ItemType == ItemType;
        }
    }
}
