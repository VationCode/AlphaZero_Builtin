using System.Runtime.CompilerServices;
using UnityEngine;

namespace Alpha.Player.Combat
{
    /// <summary>
    /// Player Combat 기능을 하나의 진입점으로 조합한다.
    /// 외부에서는 세부 Combat Module을 직접 사용하지 않는다.
    /// </summary>
    [RequireComponent(typeof(WeaponSwapModule))]
    public class PlayerCombatModule : MonoBehaviour
    {
        private WeaponSwapModule _weaponSwapModule;

        public bool IsBound { get; private set; }

        private void Awake()
        {
            _weaponSwapModule = GetComponent<WeaponSwapModule>();
        }

        public bool Bind(PlayerCore p_core)
        {
            if (p_core == null || _weaponSwapModule == null ||
                p_core.EquipmentFlow == null || !p_core.EquipmentFlow.IsBound ||
                p_core.EquipmentModule == null || !p_core.EquipmentModule.IsBound)
            {
                Debug.LogError($"{nameof(PlayerCombatModule)}의 참조가 설정되지 않았습니다.", this);
                return false;
            }

            if (!_weaponSwapModule.Bind(p_core.CombatContext, p_core.EquipmentFlow, p_core.EquipmentModule))
            {
                return false;
            }

            IsBound = true;
            return true;
        }

        #region ============================== Weapon Swap
        public bool TryPrepareWeaponSwap(int p_slotIndex)
        {
            return IsBound &&
                   _weaponSwapModule.TryPrepare(p_slotIndex);
        }

        public bool TryExecutePendingWeaponSwap()
        {
            return IsBound &&
                   _weaponSwapModule.TryExecutePending();
        }
        #endregion ============================== /Weapon Swap

        public void Unbind()
        {
            _weaponSwapModule?.Unbind();
            IsBound = false;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
