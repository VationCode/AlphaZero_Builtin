using System;
using UnityEngine;

public enum EWeaponType
{
    Melee = 0,
    Range = 1,
    SPRange = 2
}


[Serializable]
public class WeaponDTO : ItemDataDTO
{
    [Header("WeaponData")]
    public EWeaponType WeaponType;
    
    public float BaseDamage;
}

// Wrapper
[Serializable]
public class WeaponTableDTO
{
    public WeaponDTO[] Weapons;
}
