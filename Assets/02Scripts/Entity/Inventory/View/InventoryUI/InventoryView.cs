using System.Collections.Generic;
using UnityEngine;

public class InventoryView : MonoBehaviour
{

    [SerializeField] private PageUIBase _category;
    [SerializeField] private PageUIBase _weaponInventory;
    [SerializeField] private PageUIBase _armorInventory;
    [SerializeField] private PageUIBase _consumableInventory;
    [SerializeField] private PageUIBase _materialInventory;
    [SerializeField] private PageUIBase _questInventory;

    private Dictionary<EInventoryPage, PageUIBase> _pageDict;
    private void Awake()
    {
        _pageDict = new Dictionary<EInventoryPage, PageUIBase>
        {
            { EInventoryPage.Category, _category },
            { EInventoryPage.Weapon, _weaponInventory },
            { EInventoryPage.Armor, _armorInventory },
            { EInventoryPage.Consumable, _consumableInventory },
            { EInventoryPage.Material, _materialInventory },
            { EInventoryPage.Quest, _questInventory }
        };
    }

    public void HandlePageChanged(EInventoryPage p_page)
    {
        foreach (var page in _pageDict)
        {
            bool isOpen = page.Key == p_page;

            page.Value.gameObject.SetActive(isOpen);

            if (isOpen)
                page.Value.OnOpen();
            else
                page.Value.OnClose();
        }
    }
}
