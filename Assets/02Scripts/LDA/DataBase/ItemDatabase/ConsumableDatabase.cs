using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ConsumableDatabase 데이터를 등록하고 조회한다.
public class ConsumableDatabase : Database<int, ConsumableDTO>
{
    private readonly ICsvDataLoader _loader;

    public ConsumableDatabase(ICsvDataLoader p_loader)
    {
        _loader = p_loader ?? throw new ArgumentNullException(nameof(p_loader));
    }

    // Consumable CSV 전체를 검증한 뒤 ID Dictionary를 교체한다.
    public async Task InitializeAsync()
    {
        CsvTable table = await _loader.LoadAsync("Consumable");
        table.ValidateColumns(ConsumableCsvMapper.Columns);

        List<ConsumableDTO> consumables = new(table.Rows.Count);
        HashSet<int> ids = new();

        foreach (CsvRow row in table.Rows)
        {
            ConsumableDTO consumable = ConsumableCsvMapper.Map(row);

            if (!ids.Add(consumable.Id))
            {
                throw row.CreateFormatException(
                    nameof(ItemDTO.Id),
                    $"중복된 Consumable ID입니다: {consumable.Id}");
            }

            consumables.Add(consumable);
        }

        if (consumables.Count == 0)
            throw new InvalidOperationException("Consumable CSV에 데이터 행이 없습니다.");

        Clear();

        foreach (ConsumableDTO consumable in consumables)
            Add(consumable.Id, consumable);
    }
}
