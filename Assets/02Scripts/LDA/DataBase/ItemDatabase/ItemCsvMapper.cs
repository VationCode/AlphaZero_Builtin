// 모든 Item CSV가 공유하는 열을 ItemDTO에 적용하고 기본 규칙을 검증한다.
internal static class ItemCsvMapper
{
    public static void ApplyCommon(
        CsvRow p_row,
        ItemDTO p_item,
        EItemType p_itemType)
    {
        p_item.Id = p_row.GetInt(nameof(ItemDTO.Id));
        p_item.Name = p_row.GetRequiredString(nameof(ItemDTO.Name));
        p_item.ItemType = p_itemType;
        p_item.IconKey = p_row.GetRequiredString(nameof(ItemDTO.IconKey));
        p_item.PrefabKey = p_row.GetRequiredString(nameof(ItemDTO.PrefabKey));
        p_item.Price = p_row.GetInt(nameof(ItemDTO.Price));
        p_item.IsStackable = p_row.GetBool(nameof(ItemDTO.IsStackable));
        p_item.MaxStackCount = p_row.GetInt(nameof(ItemDTO.MaxStackCount));
        p_item.Description = p_row.GetString(nameof(ItemDTO.Description)).Trim();

        if (p_item.Id < 0)
        {
            throw p_row.CreateFormatException(
                nameof(ItemDTO.Id),
                "ID는 0 이상이어야 합니다.");
        }

        if (p_item.MaxStackCount <= 0)
        {
            throw p_row.CreateFormatException(
                nameof(ItemDTO.MaxStackCount),
                "최대 중첩 개수는 1 이상이어야 합니다.");
        }

        if (!p_item.IsStackable && p_item.MaxStackCount != 1)
        {
            throw p_row.CreateFormatException(
                nameof(ItemDTO.MaxStackCount),
                "중첩 불가능한 아이템의 최대 중첩 개수는 1이어야 합니다.");
        }
    }
}
