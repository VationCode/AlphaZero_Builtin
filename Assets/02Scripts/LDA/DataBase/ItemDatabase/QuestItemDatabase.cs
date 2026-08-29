using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// QuestItemDatabase 데이터를 등록하고 조회한다.
public class QuestItemDatabase : Database<int, QuestItemDTO>
{
    private readonly ICsvDataLoader _loader;

    public QuestItemDatabase(ICsvDataLoader p_loader)
    {
        _loader = p_loader ?? throw new ArgumentNullException(nameof(p_loader));
    }

    // QuestItem CSV 전체를 검증한 뒤 ID Dictionary를 교체한다.
    public async Task InitializeAsync()
    {
        CsvTable table = await _loader.LoadAsync("QuestItem");
        table.ValidateColumns(QuestItemCsvMapper.Columns);

        List<QuestItemDTO> questItems = new(table.Rows.Count);
        HashSet<int> ids = new();

        foreach (CsvRow row in table.Rows)
        {
            QuestItemDTO questItem = QuestItemCsvMapper.Map(row);

            if (!ids.Add(questItem.Id))
            {
                throw row.CreateFormatException(
                    nameof(ItemDTO.Id),
                    $"중복된 QuestItem ID입니다: {questItem.Id}");
            }

            questItems.Add(questItem);
        }

        if (questItems.Count == 0)
            throw new InvalidOperationException("QuestItem CSV에 데이터 행이 없습니다.");

        Clear();

        foreach (QuestItemDTO questItem in questItems)
            Add(questItem.Id, questItem);
    }
}
