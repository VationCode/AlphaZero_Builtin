using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryModule : MonoBehaviour
{
    private PlayerCore _core;
    private ItemDB _itemDB;
    private List<int> _itemIdList;

    public event Action OnInventoryActivate;

    private void Awake()
    {
        _itemIdList= new List<int>();
    }

    public void Bind(PlayerCore p_core)
    {
        _core = p_core;
        _itemDB = p_core.ItemDB;
    }
    public void AddItem(int p_id)
    {
        _itemIdList.Add(p_id);
        ItemDataDTO item = _itemDB.GetItem(p_id);

        Debug.Log(item.PrefabKey);

    }

    public void OnInventory()
    {
        _core.UIManager.Open<InventoryUI>();
        //OnInventoryActivate?.Invoke();
    }

    private void Update()
    {
        if(_core.InputManager.IsInventory)
        {
            _core.CameraCore.Cursour(true);
            OnInventory();
        }
    }
}
