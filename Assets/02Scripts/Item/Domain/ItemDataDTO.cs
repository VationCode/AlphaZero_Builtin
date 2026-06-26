using System;
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
    public int Id;

    public string Name;

    public string ItemType;

    public string IconKey;

    public string PrefabKey;

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
    public ItemDataDTO[] Items;
}
