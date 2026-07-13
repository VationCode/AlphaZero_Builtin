using System.Threading.Tasks;

public class ArmorDatabase : Database<int, ArmorDTO>
{
    public ArmorDatabase(IDataLoader p_loader) : base(p_loader){}

    public async Task InitializeAsync()
    {
        ArmorWrapper itemTable = await _Loader.LoadAsync<ArmorWrapper>("Armor");

        foreach (ArmorDTO armor in itemTable.ItemList)
        {
            Add(armor.Id, armor);
        }
    }
}