using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDB : MonoBehaviour
{
    [SerializeField] private TextAsset _json;

    private Dictionary<int, WeaponDataDTO> _weaponDict;

    private void Awake()
    {
        _weaponDict = new();
        Load();
    }

    private void Load()
    {
        WeaponTableDTO table =
            JsonUtility.FromJson<WeaponTableDTO>(_json.text);

        foreach(var weapon in table.Weapons)
        {
            if (_weaponDict.ContainsKey(weapon.Id))
            {
                Debug.LogError($"Duplicate Item Id : {weapon.Id}");
                continue;
            }
            _weaponDict.Add(weapon.Id, weapon);
        }

        Debug.Log($"Weapon Loaded : {_weaponDict.Count}");
    }

    public WeaponDataDTO GetWeaponData(int p_id)
    {
        if (_weaponDict.TryGetValue(p_id, out WeaponDataDTO weapon))
            return weapon;

        Debug.LogError($"Weapon Not Found : {p_id}");
        return null;
    }
}
