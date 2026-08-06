using System.Threading.Tasks;

// ArmorDatabase 데이터를 등록하고 조회한다.
public class ArmorDatabase : Database<int, ArmorDTO>
{
    // 전달받은 값으로 초기 상태를 구성한다.
    public ArmorDatabase(IDataLoader p_loader) : base(p_loader){}

    // 초기 데이터와 내부 상태를 구성한다.
    public async Task InitializeAsync()
    {
        ArmorWrapper itemTable = await _Loader.LoadAsync<ArmorWrapper>("Armor");

        foreach (ArmorDTO armor in itemTable.ItemList)
        {
            Add(armor.Id, armor);
        }
    }
}