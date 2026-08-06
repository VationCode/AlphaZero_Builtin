using System;
using System.Collections.Generic;
using UnityEngine;
// EWeaponType 관련 선택 값을 정의한다.
public enum EWeaponType
{
    None = -1,
    Melee = 0,
    Range = 1,
    Special = 2     // 특수 장비(화염방사기, 유탄, 드론 등과 같은)
}
// WeaponDTO 데이터 표현을 보관한다.
[Serializable]
public abstract class WeaponDTO : ItemDTO
{
    [Header("WeaponData")]
    public abstract EWeaponType WeaponType { get; }

    public float BaseDamage;

}
// MeleeWeaponDTO 데이터 표현을 보관한다.
[Serializable]
public sealed class MeleeWeaponDTO : WeaponDTO
{
    // 이후 근접 전용 데이터
    public override EWeaponType WeaponType => EWeaponType.Melee;
}

// RangeWeaponDTO 데이터 표현을 보관한다.
[Serializable]
public sealed class RangeWeaponDTO : WeaponDTO
{
    public override EWeaponType WeaponType => EWeaponType.Range;

    public float Rate;
    public float MaxDistance;
}

// SpecialWeaponDTO 데이터 표현을 보관한다.
[Serializable]
public sealed class SpecialWeaponDTO : WeaponDTO
{
    // 이후 특수 무기 전용 데이터
    public override EWeaponType WeaponType => EWeaponType.Special;
}
// JSON의 무기 타입별 배열 구조를 역직렬화한다.
[Serializable]
public class WeaponWrapper
{
    public List<MeleeWeaponDTO> MeleeList;
    public List<RangeWeaponDTO> RangeList;
    public List<SpecialWeaponDTO> SpecialList;
}
