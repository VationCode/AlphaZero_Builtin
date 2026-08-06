namespace Alpha.Player.Slot
{
    // ItemSlotView에 전달하는 공통 화면 데이터.
    public readonly struct ItemSlotViewData
    {
        public bool IsEmpty { get; }
        public EItemType ItemType { get; }
        public string ItemName { get; }
        public string IconKey { get; }
        public int Count { get; }

        // 전달받은 값으로 초기 상태를 구성한다.
        public ItemSlotViewData(bool p_isEmpty, EItemType p_itemType, string p_itemName, string p_iconKey, int p_count)
        {
            IsEmpty = p_isEmpty;
            ItemType = p_itemType;
            ItemName = p_itemName;
            IconKey = p_iconKey;
            Count = p_count;
        }
    }
}
