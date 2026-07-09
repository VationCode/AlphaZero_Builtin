using System.Collections.Generic;
using UnityEngine;

public class ResourceLoadManager : MonoBehaviour
{
    private static ResourceLoadManager _instance;
    public static ResourceLoadManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ResourceLoadManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
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
}
