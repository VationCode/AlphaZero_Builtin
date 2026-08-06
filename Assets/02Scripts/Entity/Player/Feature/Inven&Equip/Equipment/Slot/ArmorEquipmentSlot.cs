namespace Alpha.Player.Equipment
{
    // ArmorEquipmentSlot 상태와 아이템 수용 규칙을 관리한다.
    public class ArmorEquipmentSlot : EquipmentSlot
    {
        // 전달받은 값으로 초기 상태를 구성한다.
        public ArmorEquipmentSlot(EArmorType p_armorType)
        {
            ArmorType = p_armorType;
        }

        public EArmorType ArmorType { get; }
        public ArmorDTO Armor => Item as ArmorDTO;

        // CanEquip 실행 가능 조건을 검사한다.
        public override bool CanEquip(ItemDTO p_item)
        {
            return p_item is ArmorDTO armor &&
                   armor.ArmorType == ArmorType;
        }
    }
}
