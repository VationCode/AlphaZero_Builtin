// 런타임 무기 타입과 주·보조 행동 참조를 함께 보관한다.
public class WeaponRuntime
{
    public EWeaponType Type { get; }

    public IWeaponAction PrimaryAction { get; }
    public IWeaponAction SecondaryAction { get; }
}
