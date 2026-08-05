namespace Alpha.Player.Inventory
{
    // View에 전달할 읽기 전용 화면 데이터
    public readonly struct InventorySlotViewData
    {
        public int SlotIndex { get; }
        public bool IsEmpty { get; }

        public EItemType ItemType { get; }
        public string ItemName { get; }
        public string IconKey { get; }
        public int Count { get; }

        public InventorySlotViewData(int p_slotIndex, bool p_isEmpty,
                                     EItemType p_itemType, string p_itemName,
                                     string p_iconKey, int p_count)
        {
            SlotIndex = p_slotIndex;
            IsEmpty = p_isEmpty;

            ItemType = p_itemType;
            ItemName = p_itemName;
            IconKey = p_iconKey;
            Count = p_count;
        }
    }
}