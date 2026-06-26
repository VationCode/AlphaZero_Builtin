using System.Collections.Generic;
using UnityEngine;

public class ResourceDBLoadModule : MonoBehaviour
{
    public static ResourceDBLoadModule Instance;

    private Dictionary<string, Sprite> _weaponIconDict;
    private Dictionary<string, GameObject> _weaponPrefabDict;
    
    private void Awake()
    {
        Instance = this;

        _weaponIconDict = new();
        _weaponPrefabDict = new();

        Sprite[] icons = Resources.LoadAll<Sprite>("Icons/Item/Weapon");
        foreach (var icon in icons)
        {
            _weaponIconDict.Add(icon.name, icon);
        }

        GameObject[] itemPrefabs = Resources.LoadAll<GameObject>("Prefabs/Item/Weapon");
        foreach (var weapon in itemPrefabs)
        {
            _weaponPrefabDict.Add(weapon.name, weapon);
        }
    }

    public Sprite GetWeaponIcon(string p_iconKey)
    {
        return _weaponIconDict[p_iconKey];
    }
    public GameObject GetWeaponPrefab(string p_prefabKey)
    {
        return _weaponPrefabDict[p_prefabKey];
    }
}
