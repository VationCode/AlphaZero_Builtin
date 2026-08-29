using System;

// 무기의 장착 위치와 상위 전투 방식을 구분한다.
public enum EWeaponCategory
{
    None = -1,
    Melee = 0,
    Range = 1,
    Special = 2
}

// Category 안에서 검·소총·산탄총 같은 구체적인 무기 종류를 구분한다.
public enum EWeaponType
{
    None = 0,
    Sword,
    Polearm,
    Rifle,
    SniperRifle,
    PenetrationRifle,
    Shotgun,
    GrenadeLauncher,
    SpecialDevice
}

// 무기의 상위 Category와 구체 Type을 보관한다.
[Serializable]
public sealed class WeaponDTO : ItemDTO
{
    public EWeaponCategory WeaponCategory;
    public EWeaponType WeaponType;
}
