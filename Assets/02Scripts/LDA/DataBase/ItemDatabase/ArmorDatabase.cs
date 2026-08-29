using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ArmorDatabase 데이터를 등록하고 조회한다.
public class ArmorDatabase : Database<int, ArmorDTO>
{
    private readonly ICsvDataLoader _loader;

    public ArmorDatabase(ICsvDataLoader p_loader)
    {
        _loader = p_loader ?? throw new ArgumentNullException(nameof(p_loader));
    }

    // Armor CSV 전체를 검증한 뒤 ID Dictionary를 교체한다.
    public async Task InitializeAsync()
    {
        CsvTable table = await _loader.LoadAsync("Armor");
        table.ValidateColumns(ArmorCsvMapper.Columns);

        List<ArmorDTO> armors = new(table.Rows.Count);
        HashSet<int> ids = new();

        foreach (CsvRow row in table.Rows)
        {
            ArmorDTO armor = ArmorCsvMapper.Map(row);

            if (!ids.Add(armor.Id))
            {
                throw row.CreateFormatException(
                    nameof(ItemDTO.Id),
                    $"중복된 Armor ID입니다: {armor.Id}");
            }

            armors.Add(armor);
        }

        if (armors.Count == 0)
            throw new InvalidOperationException("Armor CSV에 데이터 행이 없습니다.");

        Clear();

        foreach (ArmorDTO armor in armors)
            Add(armor.Id, armor);
    }
}
