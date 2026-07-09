using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemParse : MonoBehaviour
{
    // 추후에는 어드레서블로 관리
    [SerializeField] private TextAsset _json;

    private Dictionary<int, ItemDataDTO> _itemDBDict;

    private void Awake()
    {
        _itemDBDict = new();

        Load();
    }

    private void Load()
    {
        /*ItemTableDTO table = null;
        try
        {
            table = JsonUtility.FromJson<ItemTableDTO>(_json.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"ItemDB Load Failed\n{e}");
            return;
        }

        foreach (var item in table.ItemList)
        {
            // 중복 체크
            if (_itemDBDict.ContainsKey(item.ID))
            {
                Debug.LogError($"Duplicate ItemSO Id : {item.ID}");
                continue;
            }
            _itemDBDict.Add(item.ID, item);
        }

        Debug.Log($"ItemSO Loaded : {_itemDBDict.Count}");*/
    }

    public ItemDataDTO GetItem(int p_id)
    {
        // 예외 대비
        if (_itemDBDict.TryGetValue(p_id, out ItemDataDTO item))
            return item;

        Debug.LogError($"ItemSO Not Found : {p_id}");
        return null;
    }
}
