using System;
using System.Collections.Generic;
using UnityEngine;

public enum EItemType
{
    None = -1,
    Weapon = 0,
    Armor = 1,
    Consumable = 2,
    Material = 3,
    Quest = 4
}

public enum EArmorType
{
    None = -1,
    Helmet = 0,
    Chest = 1,
    Gloves = 2,
    Boots = 3
}

[Serializable]
public class ItemDTO
{
    public int Id;

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
public class ItemWrapper
{
    // 변수명은 json에서의 목록 이름과 같아야한다
    public List<ItemDTO> ItemList;
}
#region ==================== Weapon
public enum EWeaponType
{
    None = -1,
    Melee = 0,
    Range = 1,
    Special = 2     // 특수 장비(화염방사기, 유탄, 드론 등과 같은)
}

[Serializable]
public class WeaponDTO : ItemDTO
{
    [Header("WeaponData")]
    public EWeaponType WeaponType;

    public float BaseDamage;
}

[Serializable]
public class WeaponWrapper
{
    public List<WeaponDTO> ItemList;
}
#endregion ==================== /Weapon

#region ==================== Armor
[Serializable]
public class ArmorDTO : ItemDTO
{
    [Header("ArmorData")]
    public EArmorType ArmorType;
    public int Defense;
}
public class ArmorWrapper
{
    public List<ArmorDTO> ItemList;
}

#endregion ==================== /Armor

#region ==================== Consumable
[Serializable]
public class ConsumableDTO : ItemDTO
{
    [Header("ConsumableData")]
    public int HealAmount;
}
[Serializable]
public class ConsumableWrapper
{
    public List<ConsumableDTO> ItemList;
}
#endregion ==================== /Consumable

#region ==================== Material
[Serializable]
public class MaterialDTO : ItemDTO
{

}

[Serializable]
public class MaterialWrapper
{
    public List<MaterialDTO> ItemList;
}
#endregion ==================== /Material

#region ==================== Quest
[Serializable]
public class QuestItemDTO : ItemDTO
{

}
[Serializable]
public class QuestItemWrapper
{
    public List<QuestItemDTO> ItemList;
}
#endregion ==================== /Quest