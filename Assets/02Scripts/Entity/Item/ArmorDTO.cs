using System;
using System.Collections.Generic;
using UnityEngine;

// EArmorType 관련 선택 값을 정의한다.
public enum EArmorType
{
    None = -1,
    Helmet = 0,
    Chest = 1,
    Gloves = 2,
    Boots = 3
}
// ArmorDTO 데이터 표현을 보관한다.
[Serializable]
public class ArmorDTO : ItemDTO
{
    [Header("ArmorData")]
    public EArmorType ArmorType;
}
// JSON의 방어구 배열 구조를 역직렬화한다.
public class ArmorWrapper
{
    public List<ArmorDTO> ItemList;
}
