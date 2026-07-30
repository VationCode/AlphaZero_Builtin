using Alpha.Slot;

namespace Alpha.Equipment
{
    // 방어구 종류가 일치하는 아이템 하나만 보관하는 장비 슬롯
    public class EquipmentArmorSlot : ArmorSlot
    {
        public EquipmentArmorSlot(EArmorType p_armorType) : base(p_armorType){}

        protected override int GetMaxCount(ItemDTO p_item)
        {
            return 1;
        }
    }
}
