namespace Alpha.Player.Inventory
{
    // 소비 아이템 종류가 일치하는 아이템만 보관한다.
    public sealed class ConsumableInventorySlot : InventorySlot
    {
        public EConsumableType ConsumableType { get; }

        public ConsumableInventorySlot(
            int p_index,
            EConsumableType p_consumableType) : base(p_index)
        {
            ConsumableType = p_consumableType;
        }

        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is ConsumableDTO consumable &&
                   consumable.ConsumableType == ConsumableType;
        }
    }
}
