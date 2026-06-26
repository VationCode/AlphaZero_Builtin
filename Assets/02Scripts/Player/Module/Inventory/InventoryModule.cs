using System.Collections.Generic;
using UnityEngine;

public class InventoryModule : MonoBehaviour
{
    private List<int> _itemIdList;

    private void Awake()
    {
        _itemIdList= new List<int>();
    }
    public void AddItem(int p_id)
    {
        _itemIdList.Add(p_id);
    }
}
