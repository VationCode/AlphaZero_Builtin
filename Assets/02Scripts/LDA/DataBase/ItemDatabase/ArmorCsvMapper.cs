// Armor CSV의 전용 분류와 공통 Item 값을 ArmorDTO로 변환한다.
internal static class ArmorCsvMapper
{
    public static readonly string[] Columns =
    {
        nameof(ItemDTO.Id),
        nameof(ItemDTO.Name),
        nameof(ArmorDTO.ArmorType),
        nameof(ItemDTO.IconKey),
        nameof(ItemDTO.PrefabKey),
        nameof(ItemDTO.Price),
        nameof(ItemDTO.IsStackable),
        nameof(ItemDTO.MaxStackCount),
        nameof(ItemDTO.Description)
    };

    public static ArmorDTO Map(CsvRow p_row)
    {
        EArmorType armorType =
            p_row.GetEnum<EArmorType>(nameof(ArmorDTO.ArmorType));

        if (armorType == EArmorType.None)
        {
            throw p_row.CreateFormatException(
                nameof(ArmorDTO.ArmorType),
                "장착 가능한 ArmorType이어야 합니다.");
        }

        ArmorDTO armor = new()
        {
            ArmorType = armorType
        };

        ItemCsvMapper.ApplyCommon(p_row, armor, EItemType.Armor);
        return armor;
    }
}
