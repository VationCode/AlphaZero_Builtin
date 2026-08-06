using System.Threading.Tasks;


// MaterialDatabase 데이터를 등록하고 조회한다.
public class MaterialDatabase : Database<int, MaterialDTO>
{
    // 전달받은 값으로 초기 상태를 구성한다.
    public MaterialDatabase(IDataLoader p_loader) : base(p_loader){}

    // 초기 데이터와 내부 상태를 구성한다.
    public async Task InitializeAsync()
    {
        MaterialWrapper itemTable = await _Loader.LoadAsync<MaterialWrapper>("Material");

        foreach (MaterialDTO material in itemTable.ItemList)
        {
            Add(material.Id, material);
        }
    }
}
