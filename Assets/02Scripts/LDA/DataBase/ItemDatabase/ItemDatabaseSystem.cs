
using UnityEngine;

public class ItemDatabaseSystem : MonoBehaviour
{
    private static ItemDatabaseSystem _instance;
    public static ItemDatabaseSystem Instance
    {
        get
        {
            if(_instance == null) _instance = FindObjectOfType<ItemDatabaseSystem>();
            return _instance;
        }
    }

    public WeaponDatabase Weapon { get; private set; }
    public ArmorDatabase Armor { get; private set; }
    public ConsumableDatabase Consumable { get; private set; }
    public MaterialDatabase Material { get; private set; }
    public QuestItemDatabase QuestItem { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        DontDestroyOnLoad(gameObject);

        IDataLoader loader = new JsonDataLoader(Application.dataPath + "/Resources/DB/Items");

        Initialize(loader);
    }

    public void Initialize(IDataLoader p_loader)
    {
        Weapon = new WeaponDatabase(p_loader);
        Armor = new ArmorDatabase(p_loader);
        Consumable = new ConsumableDatabase(p_loader);
        Material = new MaterialDatabase(p_loader);
        QuestItem = new QuestItemDatabase(p_loader);
    }

    private async void Start()
    {
        await Weapon.InitializeAsync();

        /*await Armor.InitializeAsync();
        await Consumable.InitializeAsync();
        await Material.InitializeAsync();
        await QuestItem.InitializeAsync();
        var weaponDict = Weapon.GetAll();
        foreach (var weapon in weaponDict)
        {
            Debug.Log($"ID : {weapon.Key}, Name : {weapon.Value.Name}");
        }*/
    }

    public bool TryGetItem(EItemType p_type, int p_id, out ItemDTO p_data)
    {
        switch (p_type)
        {
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
            case EItemType.Quest:
                if (QuestItem.TryGet(p_id, out QuestItemDTO questItemDTO))
                {
                    p_data = questItemDTO;
                    return true;
                }
                break;
        }

        p_data = null;
        return false;
    }
}