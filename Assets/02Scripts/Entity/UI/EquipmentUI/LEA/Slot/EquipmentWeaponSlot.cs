using Alpha.Slot;

namespace Alpha.Equipment
{
    // 무기 종류가 일치하는 아이템 하나만 보관하는 장비 슬롯
    public class EquipmentWeaponSlot : WeaponSlot
    {
        public EquipmentWeaponSlot(EWeaponType p_weaponType): base(p_weaponType){}

        protected override int GetMaxCount(ItemDTO p_item)
        {
            return 1;
        }
    }
}
