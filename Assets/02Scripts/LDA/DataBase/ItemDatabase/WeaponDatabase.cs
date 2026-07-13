using System.Threading.Tasks;

public class WeaponDatabase : Database<int, WeaponDTO>
{
    public WeaponDatabase(IDataLoader p_loader) : base(p_loader) { }

    public async Task InitializeAsync()
    {
        WeaponWrapper itemTable = await _Loader.LoadAsync<WeaponWrapper>("Weapon");

        foreach (WeaponDTO weapon in itemTable.ItemList)
        {
            Add(weapon.Id, weapon);
        }
    }
}