using System;
using System.Collections.Generic;
using UnityEngine;

// EItemType 관련 선택 값을 정의한다.
public enum EItemType
{
    None = -1,
    Weapon = 0,
    Armor = 1,
    Consumable = 2,
    Material = 3,
    QuestItem = 4
}

// ItemDTO 데이터 표현을 보관한다.
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

// JSON의 공용 아이템 배열 구조를 역직렬화한다.
[Serializable]
public class ItemWrapper
{
    // 변수명은 json에서의 목록 이름과 같아야한다
    public List<ItemDTO> ItemList;
}

#region ==================== Consumable
// EConsumableType 관련 선택 값을 정의한다.
public enum EConsumableType
{
    None = -1,
    Heal = 0,
    Mana = 1,
    Pack = 2,
}
// ConsumableDTO 데이터 표현을 보관한다.
[Serializable]
public class ConsumableDTO : ItemDTO
{
    [Header("ConsumableData")]
    public EConsumableType ConsumableType;
    public int HealAmount;
}
// JSON의 소비 아이템 배열 구조를 역직렬화한다.
[Serializable]
public class ConsumableWrapper
{
    public List<ConsumableDTO> ItemList;
}
#endregion ==================== /Consumable

#region ==================== Material
// EMaterialType 관련 선택 값을 정의한다.
public enum EMaterialType
{
    None = -1,
    Mineral = 0,
    Organic = 1,
    Essence = 2,
}
// MaterialDTO 데이터 표현을 보관한다.
[Serializable]
public class MaterialDTO : ItemDTO
{
    public EMaterialType MaterialType;
}

// JSON의 재료 아이템 배열 구조를 역직렬화한다.
[Serializable]
public class MaterialWrapper
{
    public List<MaterialDTO> ItemList;
}
#endregion ==================== /Material

#region ==================== Quest
// QuestItemDTO 데이터 표현을 보관한다.
[Serializable]
public class QuestItemDTO : ItemDTO
{

}
// JSON의 퀘스트 아이템 배열 구조를 역직렬화한다.
[Serializable]
public class QuestItemWrapper
{
    public List<QuestItemDTO> ItemList;
}
#endregion ==================== /Quest
