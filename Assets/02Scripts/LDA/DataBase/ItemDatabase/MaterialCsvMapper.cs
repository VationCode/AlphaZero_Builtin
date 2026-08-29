// Material CSV의 소재 종류와 공통 Item 값을 MaterialDTO로 변환한다.
internal static class MaterialCsvMapper
{
    public static readonly string[] Columns =
    {
        nameof(ItemDTO.Id),
        nameof(ItemDTO.Name),
        nameof(MaterialDTO.MaterialType),
        nameof(ItemDTO.IconKey),
        nameof(ItemDTO.PrefabKey),
        nameof(ItemDTO.Price),
        nameof(ItemDTO.IsStackable),
        nameof(ItemDTO.MaxStackCount),
        nameof(ItemDTO.Description)
    };

    public static MaterialDTO Map(CsvRow p_row)
    {
        EMaterialType materialType =
            p_row.GetEnum<EMaterialType>(nameof(MaterialDTO.MaterialType));

        if (materialType == EMaterialType.None)
        {
            throw p_row.CreateFormatException(
                nameof(MaterialDTO.MaterialType),
                "사용 가능한 MaterialType이어야 합니다.");
        }

        MaterialDTO material = new()
        {
            MaterialType = materialType
        };

        ItemCsvMapper.ApplyCommon(p_row, material, EItemType.Material);
        return material;
    }
}
