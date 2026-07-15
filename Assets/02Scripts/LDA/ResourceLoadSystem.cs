using System.Collections.Generic;
using UnityEngine;

public class ResourceLoadSystem : MonoBehaviour
{
    private static ResourceLoadSystem _instance;
    public static ResourceLoadSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ResourceLoadSystem>();
            }
            return _instance;
        }
    }
    private readonly Dictionary<EItemType, Dictionary<string, Sprite>>
    _iconCache = new Dictionary<EItemType, Dictionary<string, Sprite>>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadIconCache();
    }

    public GameObject GetItemPrefab(EItemType p_itemType, string p_key)
    {
        string itemPath = $"Prefab/Items/{p_itemType}/{p_key}";

        if(itemPath == null)
        {
            Debug.LogError($"Item prefab not found for item type: {p_itemType}, id: {p_key}");
            return null;
        }

        GameObject itemPrefab = Resources.Load<GameObject>(itemPath);

        return itemPrefab;
    }

    public Sprite GetIcon(EItemType p_itemType, string p_key)
    {
        if (!_iconCache.TryGetValue(p_itemType, out Dictionary<string, Sprite> typeCache))
        {
            Debug.LogError($"Icon type not loaded: {p_itemType}");

            return null;
        }

        if (!typeCache.TryGetValue(p_key, out Sprite sprite))
        {
            Debug.LogError($"Icon not found: {p_itemType}/{p_key}");

            return null;
        }

        return sprite;
    }

    private void LoadIconCache()
    {
        EItemType[] itemTypes =
            {EItemType.Weapon, EItemType.Armor,
            EItemType.Consumable,EItemType.Material,
            EItemType.QuestItem};

        foreach (EItemType itemType in itemTypes)
        {
            string sheetPath =
                $"Icon/Items/{itemType}/{itemType}Icons";

            Sprite[] sprites =
                Resources.LoadAll<Sprite>(sheetPath);

            Dictionary<string, Sprite> typeCache = new Dictionary<string, Sprite>();

            foreach (Sprite sprite in sprites)
            {
                if (typeCache.ContainsKey(sprite.name))
                {
                    Debug.LogError($"Duplicated sprite name: {itemType}/{sprite.name}");

                    continue;
                }

                typeCache.Add(sprite.name, sprite);
            }

            _iconCache[itemType] = typeCache;
        }
    }
}
