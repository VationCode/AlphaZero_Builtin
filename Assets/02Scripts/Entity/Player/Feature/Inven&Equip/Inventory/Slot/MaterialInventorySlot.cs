namespace Alpha.Player.Inventory
{
    // 재료 아이템 종류가 일치하는 아이템만 보관한다.
    public sealed class MaterialInventorySlot : InventorySlot
    {
        public EMaterialType MaterialType { get; }

        public MaterialInventorySlot(
            int p_index,
            EMaterialType p_materialType) : base(p_index)
        {
            MaterialType = p_materialType;
        }

        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is MaterialDTO material &&
                   material.MaterialType == MaterialType;
        }
    }
}
