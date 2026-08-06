using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// WeaponDatabase 데이터를 등록하고 조회한다.
public class WeaponDatabase : Database<int, WeaponDTO>
{
    // 전달받은 값으로 초기 상태를 구성한다.
    public WeaponDatabase(IDataLoader p_loader) : base(p_loader) { }

    // 무기 종류별 목록을 읽어 하나의 ID Dictionary로 합친다.
    public async Task InitializeAsync()
    {
        WeaponWrapper table = await _Loader.LoadAsync<WeaponWrapper>("Weapon");

        Register(table.MeleeList);
        Register(table.RangeList);
        Register(table.SpecialList);
    }

    // 비어 있거나 ID가 중복된 무기 데이터를 거부하며 목록을 등록한다.
        private void Register(IEnumerable<WeaponDTO> p_weapons)
        {
            if (p_weapons == null)
                throw new InvalidOperationException("Weapon 목록이 없습니다.");

            // 잘못된 행과 중복 ID를 즉시 알리고 유효한 DTO만 공통 Dictionary에 넣는다.
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
