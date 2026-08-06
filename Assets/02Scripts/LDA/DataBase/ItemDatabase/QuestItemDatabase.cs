using System.Threading.Tasks;

// QuestItemDatabase 데이터를 등록하고 조회한다.
public class QuestItemDatabase : Database<int, QuestItemDTO>
{
    // 전달받은 값으로 초기 상태를 구성한다.
    public QuestItemDatabase(IDataLoader p_loader) : base(p_loader){}

    // 초기 데이터와 내부 상태를 구성한다.
    public async Task InitializeAsync()
    {
        QuestItemWrapper itemTable = await _Loader.LoadAsync<QuestItemWrapper>("QuestItem");

        foreach (QuestItemDTO questItem in itemTable.ItemList)
        {
            Add(questItem.Id, questItem);
        }
    }
}
