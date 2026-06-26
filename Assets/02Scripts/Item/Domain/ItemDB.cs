using System.Collections.Generic;
using UnityEngine;

public class ItemDB : MonoBehaviour
{
    // 추후에는 어드레서블로 관리
    [SerializeField] private TextAsset _json;

    private Dictionary<int, ItemDataDTO> _itemDict;

    private void Awake()
    {
        _itemDict = new();

        Load();
    }

    private void Load()
    {
        ItemTableDTO table = JsonUtility.FromJson<ItemTableDTO>(_json.text);

        foreach (var item in table.Items)
        {
            // 중복 체크
            if (_itemDict.ContainsKey(item.Id))
            {
                Debug.LogError($"Duplicate Item Id : {item.Id}");
                continue;
            }
            _itemDict.Add(item.Id, item);
        }

        Debug.Log($"Item Loaded : {_itemDict.Count}");
    }

    public ItemDataDTO GetItem(int p_id)
    {
        // 예외 대비
        if (_itemDict.TryGetValue(p_id, out ItemDataDTO item))
            return item;

        Debug.LogError($"Item Not Found : {p_id}");
        return null;
    }
}
