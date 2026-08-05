namespace Alpha.Player.Inventory
{
    public enum EInventoryTransferResult
    {
        Rejected,   // 거부
        Moved,
        Merged,
        Swapped
    }

    // 두 슬롯 사이의 아이템 이전 규칙 담당
    public class InventoryTransferModule
    {
        public EInventoryTransferResult Transfer(InventorySlot p_source, InventorySlot p_target)
        {
            bool canSourceReceiveTarget = p_source.CanReplace(p_target.Item, p_target.Count);


            bool canTargetReceiveSource = p_target.CanReplace(p_source.Item, p_source.Count);

            // 이동, 병합, 교환 규칙 처리
            return EInventoryTransferResult.Rejected;
        }

    }
}
