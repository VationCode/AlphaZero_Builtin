// Consumable CSV의 사용 종류와 회복 값을 ConsumableDTO로 변환한다.
internal static class ConsumableCsvMapper
{
    public static readonly string[] Columns =
    {
        nameof(ItemDTO.Id),
        nameof(ItemDTO.Name),
        nameof(ConsumableDTO.ConsumableType),
        nameof(ConsumableDTO.HealAmount),
        nameof(ItemDTO.IconKey),
        nameof(ItemDTO.PrefabKey),
        nameof(ItemDTO.Price),
        nameof(ItemDTO.IsStackable),
        nameof(ItemDTO.MaxStackCount),
        nameof(ItemDTO.Description)
    };

    public static ConsumableDTO Map(CsvRow p_row)
    {
        EConsumableType consumableType =
            p_row.GetEnum<EConsumableType>(nameof(ConsumableDTO.ConsumableType));

        if (consumableType == EConsumableType.None)
        {
            throw p_row.CreateFormatException(
                nameof(ConsumableDTO.ConsumableType),
                "사용 가능한 ConsumableType이어야 합니다.");
        }

        int healAmount = p_row.GetInt(nameof(ConsumableDTO.HealAmount));

        if (healAmount < 0)
        {
            throw p_row.CreateFormatException(
                nameof(ConsumableDTO.HealAmount),
                "회복량은 0 이상이어야 합니다.");
        }

        ConsumableDTO consumable = new()
        {
            ConsumableType = consumableType,
            HealAmount = healAmount
        };

        ItemCsvMapper.ApplyCommon(p_row, consumable, EItemType.Consumable);
        return consumable;
    }
}
