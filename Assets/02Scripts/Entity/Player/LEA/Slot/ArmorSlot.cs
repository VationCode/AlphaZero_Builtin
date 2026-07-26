namespace Alpha.Player.Slot
{
    public class ArmorSlot : SlotBase
    {
        public EArmorType ArmorType { get; }

        public ArmorSlot(EArmorType p_armorType)
        {
            ArmorType = p_armorType;
        }

        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is ArmorDTO armor &&
                   armor.ArmorType == ArmorType;
        }
    }
}
