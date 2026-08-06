using UnityEngine;

namespace Alpha.Player.Inventory
{
    // WeaponInventorySlot 상태와 아이템 수용 규칙을 관리한다.
    public class WeaponInventorySlot : InventorySlot
    {
        public EWeaponType WeaponType { get; }

        // 전달받은 값으로 초기 상태를 구성한다.
        public WeaponInventorySlot(int p_index, EWeaponType p_weaponType) : base(p_index)
        {
            WeaponType = p_weaponType;
        }

        // CanStore 실행 가능 조건을 검사한다.
        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is WeaponDTO weapon &&
                   weapon.WeaponType == WeaponType;
        }
    }
}
