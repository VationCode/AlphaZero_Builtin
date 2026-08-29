// QuestItem CSV의 공통 Item 값을 QuestItemDTO로 변환한다.
internal static class QuestItemCsvMapper
{
    public static readonly string[] Columns =
    {
        nameof(ItemDTO.Id),
        nameof(ItemDTO.Name),
        nameof(ItemDTO.IconKey),
        nameof(ItemDTO.PrefabKey),
        nameof(ItemDTO.Price),
        nameof(ItemDTO.IsStackable),
        nameof(ItemDTO.MaxStackCount),
        nameof(ItemDTO.Description)
    };

    public static QuestItemDTO Map(CsvRow p_row)
    {
        QuestItemDTO questItem = new();
        ItemCsvMapper.ApplyCommon(p_row, questItem, EItemType.QuestItem);
        return questItem;
    }
}
