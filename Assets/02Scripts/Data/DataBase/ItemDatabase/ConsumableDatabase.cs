using System.Threading.Tasks;

public class ConsumableDatabase : Database<int, ConsumableDTO>
{
    public ConsumableDatabase(IDataLoader p_loader) : base(p_loader){}

    public async Task InitializeAsync()
    {
        ConsumableWrapper itemTable = await _Loader.LoadAsync<ConsumableWrapper>("ConsumablePage");

        foreach (ConsumableDTO consumable in itemTable.ItemList)
        {
            Add(consumable.Id, consumable);
        }
    }
}
