using System;
using System.Collections.Generic;
using UnityEngine;

public enum EArmorType
{
    None = -1,
    Helmet = 0,
    Chest = 1,
    Gloves = 2,
    Boots = 3
}
[Serializable]
public class ArmorDTO : ItemDTO
{
    [Header("ArmorData")]
    public EArmorType ArmorType;
    public int BaseDefense;
}
public class ArmorWrapper
{
    public List<ArmorDTO> ItemList;
}