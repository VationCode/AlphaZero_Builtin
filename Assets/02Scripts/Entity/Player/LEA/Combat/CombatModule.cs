using Alpha.Player.Equipment;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player의 전투 기능을 실제로 실행한다.
    // Swap 여부와 실행 시점은 CombatFlow와 State가 판단한다.
    public class CombatModule : MonoBehaviour
    {
        private PlayerEquipmentContext _equipmentContext;

        public bool IsBound { get; private set; }
        public void Bind(PlayerEquipmentContext p_equipmentContext)
        {
            if (p_equipmentContext == null)
            {
                Debug.LogError($"{nameof(CombatModule)}에 EquipmentContext가 설정되지 않았습니다.");

                return;
            }

            _equipmentContext = p_equipmentContext;
            IsBound = true;
        }

        // 선택된 무기를 Player의 현재 사용 무기로 변경한다.
        // 실제 현재 무기 변경
        public bool TrySwapWeapon(WeaponDTO p_weapon)
        {
            if (!IsBound || p_weapon == null)
                return false;

            return _equipmentContext.TrySetWeapon(p_weapon);
        }

        // 현재 사용 중인 무기를 해제한다.
        public bool TryClearWeapon()
        {
            if (!IsBound)
                return false;

            return _equipmentContext.TryClearWeapon();
        }

    }
}
