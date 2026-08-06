using System.Collections.Generic;
using UnityEngine;

// Resources의 아이템 Prefab과 Icon을 조회하고 Icon Sprite를 종류별로 캐시한다.
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

    // 중복 인스턴스를 제거하고 Scene 전환에도 유지할 Icon Cache를 구성한다.
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

    // 아이템 종류와 Key로 Resources 경로를 구성해 Prefab을 반환한다.
    public GameObject GetItemPrefab(EItemType p_itemType, string p_key)
    {
        // 유효하지 않은 종류와 빈 Key로 Resources를 조회하지 않는다.
        if (p_itemType == EItemType.None || string.IsNullOrWhiteSpace(p_key))
        {
            Debug.LogError($"Invalid item prefab information: {p_itemType}/{p_key}");

            return null;
        }

        // Project의 아이템 Prefab 경로 규칙에 맞춰 Resources Key를 구성한다.
        string itemPath = $"Prefab/Items/{p_itemType}/{p_key}";
        GameObject itemPrefab = Resources.Load<GameObject>(itemPath);
        
        if (itemPrefab == null)
        {
            Debug.LogError($"Item prefab not found: Resources/{itemPath}");

            return null;
        }

        return itemPrefab;
    }

    // 미리 적재한 종류별 Cache에서 아이템 Icon을 반환한다.
    public Sprite GetIcon(EItemType p_itemType, string p_key)
    {
        // 먼저 아이템 종류 Cache를 찾고 그 안에서 Sprite Key를 조회한다.
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

    // 아이템 종류별 Sprite Sheet를 읽어 Sprite 이름을 Key로 캐시한다.
    private void LoadIconCache()
    {
        EItemType[] itemTypes =
            {EItemType.Weapon, EItemType.Armor,
            EItemType.Consumable,EItemType.Material,
            EItemType.QuestItem};

        foreach (EItemType itemType in itemTypes)
        {
            // 종류별 Sprite Sheet의 모든 Sub-Sprite를 한 번에 적재한다.
            string sheetPath =
                $"Icon/Items/{itemType}/{itemType}Icons";

            Sprite[] sprites =
                Resources.LoadAll<Sprite>(sheetPath);

            Dictionary<string, Sprite> typeCache = new Dictionary<string, Sprite>();

            foreach (Sprite sprite in sprites)
            {
                // 이름 중복은 잘못된 아이템 Icon을 반환할 수 있으므로 등록하지 않는다.
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
