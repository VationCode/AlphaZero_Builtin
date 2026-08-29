// Weapon CSV의 Category와 Type을 검증하고 공통 Item 값을 구성한다.
internal static class WeaponCsvMapper
{
    public static readonly string[] Columns =
    {
        nameof(ItemDTO.Id),
        nameof(ItemDTO.Name),
        nameof(WeaponDTO.WeaponCategory),
        nameof(WeaponDTO.WeaponType),
        nameof(ItemDTO.IconKey),
        nameof(ItemDTO.PrefabKey),
        nameof(ItemDTO.Price),
        nameof(ItemDTO.IsStackable),
        nameof(ItemDTO.MaxStackCount),
        nameof(ItemDTO.Description)
    };

    public static WeaponDTO Map(CsvRow p_row)
    {
        EWeaponCategory category =
            p_row.GetEnum<EWeaponCategory>(nameof(WeaponDTO.WeaponCategory));
        EWeaponType weaponType =
            p_row.GetEnum<EWeaponType>(nameof(WeaponDTO.WeaponType));

        if (!IsValidWeaponType(category, weaponType))
        {
            throw p_row.CreateFormatException(
                nameof(WeaponDTO.WeaponType),
                $"{category}에 속하는 WeaponType이어야 합니다.");
        }

        WeaponDTO weapon = new()
        {
            WeaponCategory = category,
            WeaponType = weaponType
        };

        ItemCsvMapper.ApplyCommon(p_row, weapon, EItemType.Weapon);
        return weapon;
    }

    // 잘못된 Category와 Type 조합을 데이터 로드 시점에 차단한다.
    private static bool IsValidWeaponType(
        EWeaponCategory p_category,
        EWeaponType p_weaponType)
    {
        return p_category switch
        {
            EWeaponCategory.Melee =>
                p_weaponType == EWeaponType.Sword ||
                p_weaponType == EWeaponType.Polearm,

            EWeaponCategory.Range =>
                p_weaponType == EWeaponType.Rifle ||
                p_weaponType == EWeaponType.SniperRifle ||
                p_weaponType == EWeaponType.PenetrationRifle ||
                p_weaponType == EWeaponType.Shotgun ||
                p_weaponType == EWeaponType.GrenadeLauncher,

            EWeaponCategory.Special =>
                p_weaponType == EWeaponType.SpecialDevice,

            _ => false
        };
    }
}
