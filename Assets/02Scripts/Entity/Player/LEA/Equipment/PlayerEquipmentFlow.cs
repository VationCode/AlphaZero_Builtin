using Alpha.Equipment;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    /// <summary>
    /// Equipment Slot 변경을 감지하고 Player 장비 적용 Module의 실행 시점을 결정한다.
    /// </summary> 
    public class PlayerEquipmentFlow
    {
        private EquipmentCore _equipmentCore;
        private PlayerEquipmentModule _equipmentModule;

        public bool IsBound { get; private set; }

        public bool Bind(EquipmentCore p_equipmentCore, PlayerEquipmentModule p_equipmentModule)
        {
            if (p_equipmentCore == null || !p_equipmentCore.IsInitialized ||
                p_equipmentModule == null || !p_equipmentModule.IsBound)
            {
                Debug.LogError($"{nameof(PlayerEquipmentFlow)}의 참조가 설정되지 않았습니다.");
                return false;
            }

            Unbind();

            _equipmentCore = p_equipmentCore;
            _equipmentModule = p_equipmentModule;

            _equipmentCore.OnEquippedWeaponChanged += HandleEquippedWeaponChanged;
            _equipmentCore.OnEquippedArmorChanged += HandleEquippedArmorChanged;

            IsBound = true;
            return true;
        }

        public void Unbind()
        {
            if (_equipmentCore != null)
            {
                _equipmentCore.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
                _equipmentCore.OnEquippedArmorChanged -= HandleEquippedArmorChanged;
            }

            _equipmentCore = null;
            _equipmentModule = null;
            IsBound = false;
        }

        #region ============================== Weapon
        /// <summary>
        /// CombatFlow가 Swap 대상으로 사용할 장착 무기를 조회한다.
        /// </summary>
        public bool TryGetEquippedWeapon(EWeaponType p_type, out WeaponDTO p_weapon)
        {
            p_weapon = null;

            return IsBound &&
                   _equipmentCore.TryGetEquippedWeapon(p_type, out p_weapon);
        }

        /// <summary>
        /// 지정한 Equipment 무기 Slot의 무기를
        /// Player의 현재 사용 무기로 적용한다.
        /// </summary>
        public bool TryApplyEquippedWeapon(EWeaponType p_type)
        {
            if (!TryGetEquippedWeapon(p_type, out WeaponDTO weapon))
            {
                return false;
            }

            return _equipmentModule.TryApplyWeapon(weapon);
        }

        private void HandleEquippedWeaponChanged(EWeaponType p_type, WeaponDTO p_weapon)
        {
            if (!IsBound)
                return;

            if (p_weapon == null)
            {
                // 현재 사용 중인 무기 Slot이 해제된 경우에만 제거한다.
                if (_equipmentModule.CurrentWeaponType == p_type)
                {
                    _equipmentModule.TryClearWeapon();
                }

                return;
            }

            // 이미 사용 중인 동일 무기라면 다시 생성하지 않는다.
            if (_equipmentModule.CurrentWeapon?.Id == p_weapon.Id)
            {
                return;
            }

            if (!_equipmentModule.TryApplyWeapon(p_weapon))
            {
                Debug.LogWarning($"Player 무기 적용에 실패했습니다: " + $"{p_type}/{p_weapon.Id}");
            }
        }
        #endregion ============================== /Weapon

        #region ============================== Armor
        /// <summary>
        /// 방어구 장착 상태에 따라 해당 부위의 외형을 적용하거나 제거한다.
        /// </summary>
        private void HandleEquippedArmorChanged(EArmorType p_type, ArmorDTO p_armor)
        {
            if (!IsBound)
                return;

            if (p_armor == null)
            {
                _equipmentModule.TryClearArmor(p_type);
                return;
            }

            if (!_equipmentModule.TryApplyArmor(p_type, p_armor))
            {
                Debug.LogWarning($"Player 방어구 외형 적용에 실패했습니다: " + $"{p_type}/{p_armor.Id}");
            }
        }
        #endregion ============================== /Armor
    }
}
