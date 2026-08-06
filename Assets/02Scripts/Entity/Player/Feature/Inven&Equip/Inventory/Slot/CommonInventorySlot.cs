using UnityEngine;
namespace Alpha.Player.Inventory
{
    // CommonInventorySlot 상태와 아이템 수용 규칙을 관리한다.
    public class CommonInventorySlot : InventorySlot
    {
        public EItemType ItemType { get; }

        // 전달받은 값으로 초기 상태를 구성한다.
        public CommonInventorySlot(int p_index, EItemType p_itemType) : base(p_index)
        {
            ItemType = p_itemType;
        }

        // CanStore 실행 가능 조건을 검사한다.
        public override bool CanStore(ItemDTO p_item)
        {
            return p_item != null &&
                   p_item.ItemType == ItemType;
        }
    }
}
