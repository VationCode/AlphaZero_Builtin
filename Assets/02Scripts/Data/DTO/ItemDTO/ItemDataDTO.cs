using System;
using System.Collections.Generic;
using UnityEngine;

public enum EItemType
{
    Weapon = 0,
    Armor = 1,
    Consumable = 2,
    Material = 3,
    Quest = 4
}

[Serializable]
public class ItemDataDTO
{
    public int ID;

    public string Name;

    public EItemType ItemType;

    public string IconKey;

    public string PrefabKey;

    public int Price;

    public bool IsStackable;

    public int MaxStackCount;

    [TextArea]
    public string Description;
}

// Wrapper
[Serializable]
public class ItemTableDTO
{
    // 변수명은 json에서의 목록 이름과 같아야한다
    public List<ItemTableDTO> ItemList;
}
#region ==================== Weapon
public enum EWeaponType
{
    Melee = 0,
    Range = 1,
    Special = 2     // 특수 장비(화염방사기, 유탄, 드론 등과 같은)
}

[Serializable]
public class WeaponDTO : ItemDataDTO
{
    [Header("WeaponData")]
    public EWeaponType WeaponType;

    public float BaseDamage;
}

[Serializable]
public class WeaponTableDTO
{
    public List<WeaponDTO> WeaponList;
}
#endregion ==================== /Weapon

#region ==================== Armor
[Serializable]
public class ArmorDTO : ItemDataDTO
{
    [Header("ArmorData")]
    public int Defense;
}
public class ArmorTableDTO
{
    public List<ArmorDTO> ArmorList;
}

#endregion ==================== /Armor

#region ==================== Consumable
[Serializable]
public class ConsumableDTO : ItemDataDTO
{
    [Header("ConsumableData")]
    public int HealAmount;
}
[Serializable]
public class ConsumableTableDTO
{
    public List<ConsumableDTO> ConsumableList;
}
#endregion ==================== /Consumable

#region ==================== Material
[Serializable]
public class MaterialDTO : ItemDataDTO
{

}

[Serializable]
public class MaterialTableDTO
{
    public List<MaterialDTO> MaterialList;
}
#endregion ==================== /Material

#region ==================== Quest
[Serializable]
public class QuestDTO : ItemDataDTO
{

}
[Serializable]
public class QuestTableDTO
{
    public List<QuestDTO> QuestItemList;
}
#endregion ==================== /Quest