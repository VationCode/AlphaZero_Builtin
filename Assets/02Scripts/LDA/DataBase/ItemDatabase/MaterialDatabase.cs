using System;
using System.Collections.Generic;
using System.Threading.Tasks;


// MaterialDatabase 데이터를 등록하고 조회한다.
public class MaterialDatabase : Database<int, MaterialDTO>
{
    private readonly ICsvDataLoader _loader;

    public MaterialDatabase(ICsvDataLoader p_loader)
    {
        _loader = p_loader ?? throw new ArgumentNullException(nameof(p_loader));
    }

    // Material CSV 전체를 검증한 뒤 ID Dictionary를 교체한다.
    public async Task InitializeAsync()
    {
        CsvTable table = await _loader.LoadAsync("Material");
        table.ValidateColumns(MaterialCsvMapper.Columns);

        List<MaterialDTO> materials = new(table.Rows.Count);
        HashSet<int> ids = new();

        foreach (CsvRow row in table.Rows)
        {
            MaterialDTO material = MaterialCsvMapper.Map(row);

            if (!ids.Add(material.Id))
            {
                throw row.CreateFormatException(
                    nameof(ItemDTO.Id),
                    $"중복된 Material ID입니다: {material.Id}");
            }

            materials.Add(material);
        }

        if (materials.Count == 0)
            throw new InvalidOperationException("Material CSV에 데이터 행이 없습니다.");

        Clear();

        foreach (MaterialDTO material in materials)
            Add(material.Id, material);
    }
}
