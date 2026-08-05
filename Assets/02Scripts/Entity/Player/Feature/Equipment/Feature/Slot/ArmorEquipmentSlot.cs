namespace Alpha.Player.Equipment
{
    public class ArmorEquipmentSlot : EquipmentSlot
    {
        public ArmorEquipmentSlot(EArmorType p_armorType)
        {
            ArmorType = p_armorType;
        }

        public EArmorType ArmorType { get; }
        public ArmorDTO Armor => Item as ArmorDTO;

        public override bool CanEquip(ItemDTO p_item)
        {
            return p_item is ArmorDTO armor &&
                   armor.ArmorType == ArmorType;
        }
    }
}
