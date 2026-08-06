using System.Threading.Tasks;

// ConsumableDatabase 데이터를 등록하고 조회한다.
public class ConsumableDatabase : Database<int, ConsumableDTO>
{
    // 전달받은 값으로 초기 상태를 구성한다.
    public ConsumableDatabase(IDataLoader p_loader) : base(p_loader){}

    // 초기 데이터와 내부 상태를 구성한다.
    public async Task InitializeAsync()
    {
        ConsumableWrapper itemTable = await _Loader.LoadAsync<ConsumableWrapper>("Consumable");

        foreach (ConsumableDTO consumable in itemTable.ItemList)
        {
            Add(consumable.Id, consumable);
        }
    }
}
