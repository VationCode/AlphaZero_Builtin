using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponParse : MonoBehaviour
{
    [SerializeField] private TextAsset _json;

    private Dictionary<int, WeaponDTO> _weaponDict;

    private void Awake()
    {
        _weaponDict = new();
        Load();
    }

    private void Load()
    {
        WeaponTableDTO table =
            JsonUtility.FromJson<WeaponTableDTO>(_json.text);

        foreach(var weapon in table.WeaponList)
        {
            if (_weaponDict.ContainsKey(weapon.ID))
            {
                Debug.LogError($"Duplicate ItemSO ID : {weapon.ID}");
                continue;
            }
            _weaponDict.Add(weapon.ID, weapon);
        }

        Debug.Log($"Weapon Loaded : {_weaponDict.Count}");
    }

    public WeaponDTO GetWeaponData(int p_id)
    {
        if (_weaponDict.TryGetValue(p_id, out WeaponDTO weapon))
            return weapon;

        Debug.LogError($"Weapon Not Found : {p_id}");
        return null;
    }
}
