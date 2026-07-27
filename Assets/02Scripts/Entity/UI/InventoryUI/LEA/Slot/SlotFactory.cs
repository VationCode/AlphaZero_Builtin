namespace Alpha.Slot
{
    public static class SlotFactory
    {
        public static WeaponSlot CreateWeaponSlot(EWeaponType p_type)
        {
            return new WeaponSlot(p_type);
        }

        public static ArmorSlot CreateArmorSlot(EArmorType p_type)
        {
            return new ArmorSlot(p_type);
        }

        public static CommonSlot CreateCommonSlot(EItemType p_type)
        {
            return new CommonSlot(p_type);
        }
    }
}