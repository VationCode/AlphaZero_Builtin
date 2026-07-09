using NUnit.Framework.Interfaces;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public ItemParse ItemDB;
    public GameObject CategoryUI;
    public WeaponInventory WeaponInventory;

    public void Bind(ItemParse p_itemDB)
    {
        ItemDB = p_itemDB;
    }

    public void AddItem(int p_id)
    {
        
    }
}
