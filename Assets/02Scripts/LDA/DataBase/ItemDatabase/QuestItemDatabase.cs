using System.Threading.Tasks;

public class QuestItemDatabase : Database<int, QuestItemDTO>
{
    public QuestItemDatabase(IDataLoader p_loader) : base(p_loader){}

    public async Task InitializeAsync()
    {
        QuestItemWrapper itemTable = await _Loader.LoadAsync<QuestItemWrapper>("QuestItem");

        foreach (QuestItemDTO questItem in itemTable.ItemList)
        {
            Add(questItem.Id, questItem);
        }
    }
}
