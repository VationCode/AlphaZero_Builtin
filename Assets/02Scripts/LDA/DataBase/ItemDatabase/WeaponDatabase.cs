using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WeaponDatabase : Database<int, WeaponDTO>
{
    public WeaponDatabase(IDataLoader p_loader) : base(p_loader) { }

    public async Task InitializeAsync()
    {
        WeaponWrapper table = await _Loader.LoadAsync<WeaponWrapper>("Weapon");

        Register(table.MeleeList);
        Register(table.RangeList);
        Register(table.SpecialList);
    }

    private void Register(IEnumerable<WeaponDTO> p_weapons)
    {
        if (p_weapons == null)
            throw new InvalidOperationException("Weapon 목록이 없습니다.");

        foreach (WeaponDTO weapon in p_weapons)
        {
            if (weapon == null)
                throw new InvalidOperationException("비어 있는 Weapon 데이터입니다.");

            if (Contains(weapon.Id))
            {
                throw new InvalidOperationException($"중복된 Weapon ID입니다: {weapon.Id}");
            }

            Add(weapon.Id, weapon);
        }
    }
}