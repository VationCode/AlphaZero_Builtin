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
    QuestItem = 4
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

#region ==================== Consumable
public enum EConsumableType
{
    None = -1,
    Heal = 0,
    Mana = 1,
    Pack = 2,
}
[Serializable]
public class ConsumableDTO : ItemDTO
{
    [Header("ConsumableData")]
    public EConsumableType ConsumableType;
    public int HealAmount;
}
[Serializable]
public class ConsumableWrapper
{
    public List<ConsumableDTO> ItemList;
}
#endregion ==================== /Consumable

#region ==================== Material
public enum EMaterialType
{
    None = -1,
    Mineral = 0,
    Organic = 1,
    Essence = 2,
}
[Serializable]
public class MaterialDTO : ItemDTO
{
    EMaterialType MaterialType;
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