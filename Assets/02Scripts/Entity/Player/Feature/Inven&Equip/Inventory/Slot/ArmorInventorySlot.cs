using UnityEngine;
namespace Alpha.Player.Inventory
{
    // ArmorInventorySlot 상태와 아이템 수용 규칙을 관리한다.
    public class ArmorInventorySlot : InventorySlot
    {
        public EArmorType ArmorType { get; }

        // 전달받은 값으로 초기 상태를 구성한다.
        public ArmorInventorySlot(int p_index, EArmorType p_armorType) : base(p_index)
        {
            ArmorType = p_armorType;
        }

        // CanStore 실행 가능 조건을 검사한다.
        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is ArmorDTO armor &&
                   armor.ArmorType == ArmorType;
        }
    }
}
