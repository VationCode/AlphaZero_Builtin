using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// WeaponDatabase 데이터를 등록하고 조회한다.
public class WeaponDatabase : Database<int, WeaponDTO>
{
    private readonly ICsvDataLoader _loader;

    public WeaponDatabase(ICsvDataLoader p_loader)
    {
        _loader = p_loader ?? throw new ArgumentNullException(nameof(p_loader));
    }

    // Weapon CSV를 구체 DTO로 변환하고 검증이 끝난 데이터만 등록한다.
    public async Task InitializeAsync()
    {
        CsvTable table = await _loader.LoadAsync("Weapon");
        table.ValidateColumns(WeaponCsvMapper.Columns);

        List<WeaponDTO> weapons = new(table.Rows.Count);
        HashSet<int> ids = new();

        foreach (CsvRow row in table.Rows)
        {
            WeaponDTO weapon = WeaponCsvMapper.Map(row);

            if (!ids.Add(weapon.Id))
            {
                throw row.CreateFormatException(
                    nameof(ItemDTO.Id),
                    $"중복된 Weapon ID입니다: {weapon.Id}");
            }

            weapons.Add(weapon);
        }

        if (weapons.Count == 0)
            throw new InvalidOperationException("Weapon CSV에 데이터 행이 없습니다.");

        Clear();

        foreach (WeaponDTO weapon in weapons)
            Add(weapon.Id, weapon);
    }
}
