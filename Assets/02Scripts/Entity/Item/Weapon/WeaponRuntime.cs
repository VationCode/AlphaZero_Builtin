public class WeaponRuntime
{
    public EWeaponType Type { get; }

    public IWeaponAction PrimaryAction { get; }
    public IWeaponAction SecondaryAction { get; }
}
