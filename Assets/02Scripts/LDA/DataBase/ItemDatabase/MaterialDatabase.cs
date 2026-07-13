using System.Threading.Tasks;


public class MaterialDatabase : Database<int, MaterialDTO>
{
    public MaterialDatabase(IDataLoader p_loader) : base(p_loader){}

    public async Task InitializeAsync()
    {
        MaterialWrapper itemTable = await _Loader.LoadAsync<MaterialWrapper>("Material");

        foreach (MaterialDTO material in itemTable.ItemList)
        {
            Add(material.Id, material);
        }
    }
}
