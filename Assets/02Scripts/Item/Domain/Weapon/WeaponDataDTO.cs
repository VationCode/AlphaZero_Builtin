using System;
using UnityEngine;

public enum EWeaponType
{
    Melee = 0,
    MainRange = 1,
    SubRange = 2
}


[Serializable]
public class WeaponDataDTO
{
    public int Id;

    public EWeaponType WeaponType;
    
    public float BaseDamage;
}

// Wrapper
[Serializable]
public class WeaponTableDTO
{
    public WeaponDataDTO[] Weapons;
}
