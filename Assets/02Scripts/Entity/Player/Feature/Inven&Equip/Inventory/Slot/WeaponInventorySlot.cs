using UnityEngine;

namespace Alpha.Player.Inventory
{
    // WeaponInventorySlot 상태와 아이템 수용 규칙을 관리한다.
    public class WeaponInventorySlot : InventorySlot
    {
        public EWeaponCategory WeaponCategory { get; }

        // 전달받은 값으로 초기 상태를 구성한다.
        public WeaponInventorySlot(
            int p_index,
            EWeaponCategory p_weaponCategory) : base(p_index)
        {
            WeaponCategory = p_weaponCategory;
        }

        // CanStore 실행 가능 조건을 검사한다.
        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is WeaponDTO weapon &&
                   weapon.WeaponCategory == WeaponCategory;
        }
    }
}
