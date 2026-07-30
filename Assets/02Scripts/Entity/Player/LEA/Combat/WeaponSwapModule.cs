using Alpha.Player.Equipment;
using UnityEngine;

namespace Alpha.Player.Combat
{
    /// <summary>
    /// 무기 Swap 대상 검증, Pending 저장,
    /// 선택된 무기의 실제 적용을 담당한다.
    /// State 전환과 Swap 가능 시점은 판단하지 않는다.
    /// </summary>
    public class WeaponSwapModule : MonoBehaviour
    {
        private CombatContext _combatContext;
        private PlayerEquipmentFlow _equipmentFlow;
        private PlayerEquipmentModule _equipmentModule;

        public bool IsBound { get; private set; }

        public bool Bind(CombatContext p_combatContext, PlayerEquipmentFlow p_equipmentFlow, PlayerEquipmentModule p_equipmentModule)
        {
            if (p_combatContext == null || p_equipmentFlow == null ||
                !p_equipmentFlow.IsBound || p_equipmentModule == null ||
                !p_equipmentModule.IsBound)
            {
                Debug.LogError($"{nameof(WeaponSwapModule)}의 참조가 설정되지 않았습니다.", this);
                return false;
            }

            _combatContext = p_combatContext;
            _equipmentFlow = p_equipmentFlow;
            _equipmentModule = p_equipmentModule;

            IsBound = true;
            return true;
        }

        /// <summary>
        /// 입력 Slot을 Equipment 무기 종류로 변환하고
        /// 실제 장착된 무기인지 확인한 뒤 Pending으로 저장한다.
        /// </summary>
        public bool TryPrepare(int p_slotIndex)
        {
            if (!IsBound)
                return false;

            // 이전 Swap 요청이 다음 요청에 남지 않도록 먼저 제거한다.
            _combatContext.ClearPendingWeapon();

            if (p_slotIndex < (int)EWeaponType.Melee || p_slotIndex > (int)EWeaponType.Special)
            {
                return false;
            }

            EWeaponType weaponType = (EWeaponType)p_slotIndex;

            // 비어 있는 Equipment Slot은 Swap 대상으로 사용할 수 없다.
            if (!_equipmentFlow.TryGetEquippedWeapon(weaponType, out WeaponDTO weapon))
            {
                return false;
            }

            WeaponDTO currentWeapon = _equipmentModule.CurrentWeapon;

            // 이미 사용 중인 동일한 무기는 다시 선택하지 않는다.
            if (currentWeapon != null && currentWeapon.Id == weapon.Id)
            {
                return false;
            }

            _combatContext.PendingWeaponType = weaponType;

            return true;
        }

        /// <summary>
        /// Pending으로 저장된 무기를 Player의 활성 무기로 적용한다.
        /// 실행 직전에 Equipment Slot을 다시 조회해 상태 변경을 검증한다.
        /// </summary>
        public bool TryExecutePending()
        {
            if (!IsBound || !_combatContext.HasPendingWeapon)
            {
                return false;
            }

            EWeaponType weaponType = _combatContext.PendingWeaponType;

            return _equipmentFlow.TryApplyEquippedWeapon(weaponType);
        }

        public void Unbind()
        {
            _combatContext = null;
            _equipmentFlow = null;
            _equipmentModule = null;

            IsBound = false;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
