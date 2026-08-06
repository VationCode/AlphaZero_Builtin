
using System.Threading.Tasks;
using UnityEngine;

// 아이템 종류별 Database를 조립·초기화하고 공통 ItemDTO 조회 진입점을 제공한다.
public class ItemDatabaseManager : MonoBehaviour
{
    /*private static ItemDatabaseManager _instance;
    public static ItemDatabaseManager Instance
    {
        get
        {
            if(_instance == null) _instance = FindFirstObjectByType<ItemDatabaseManager>();
            return _instance;
        }
    }*/

    public WeaponDatabase Weapon { get; private set; }
    public ArmorDatabase Armor { get; private set; }
    public ConsumableDatabase Consumable { get; private set; }
    public MaterialDatabase Material { get; private set; }
    public QuestItemDatabase QuestItem { get; private set; }

    public bool IsInitialized { get; private set; }

    // Resources의 아이템 JSON 경로를 사용하는 Loader로 Database를 구성한다.
    private void Awake()
    {
        /*if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        DontDestroyOnLoad(gameObject);*/

        IDataLoader loader = new JsonDataLoader(Application.dataPath + "/Resources/DB/Items");

        Initialize(loader);
    }

    // 하나의 Loader를 공유하는 아이템 종류별 Database를 생성한다.
    public void Initialize(IDataLoader p_loader)
    {
        Weapon = new WeaponDatabase(p_loader);
        Armor = new ArmorDatabase(p_loader);
        Consumable = new ConsumableDatabase(p_loader);
        Material = new MaterialDatabase(p_loader);
        QuestItem = new QuestItemDatabase(p_loader);
    }

    // 모든 아이템 Database의 JSON 적재가 끝난 뒤 조회 가능 상태로 전환한다.
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        await Weapon.InitializeAsync();
        await Armor.InitializeAsync();
        await Consumable.InitializeAsync();
        await Material.InitializeAsync();
        await QuestItem.InitializeAsync();

        IsInitialized = true;
    }

    // 아이템 종류에 맞는 Database에서 ID를 조회해 공통 ItemDTO로 반환한다.
    public bool TryGetItem(EItemType p_type, int p_id, out ItemDTO p_data)
    {
        if (!IsInitialized)
        {
            p_data = null;
            return false;
        }

        switch (p_type)
        {
            // 종류별 DTO를 조회한 뒤 공통 기반 형식으로 반환한다.
            case EItemType.Weapon:
                if (Weapon.TryGet(p_id, out WeaponDTO weaponDTO))
                {
                    p_data = weaponDTO;
                    return true;
                }
                break;
            case EItemType.Armor:
                if (Armor.TryGet(p_id, out ArmorDTO armorDTO))
                {
                    p_data = armorDTO;
                    return true;
                }
                break;
            case EItemType.Consumable:
                if (Consumable.TryGet(p_id, out ConsumableDTO consumableDTO))
                {
                    p_data = consumableDTO;
                    return true;
                }
                break;

            case EItemType.Material:
                if (Material.TryGet(p_id, out MaterialDTO materialDTO))
                {
                    p_data = materialDTO;
                    return true;
                }
                break;
            case EItemType.QuestItem:
                if (QuestItem.TryGet(p_id, out QuestItemDTO questItemDTO))
                {
                    p_data = questItemDTO;
                    return true;
                }
                break;
        }

        p_data = null;
        Debug.LogWarning("데이터가 없습니다.");
        return false;
    }
}
