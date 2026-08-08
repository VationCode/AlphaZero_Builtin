using UnityEngine;
using Alpha.Item.Weapon;

namespace Alpha.Player.Combat
{
    /// <summary>
    /// Player Combat 기능을 하나의 진입점으로 조합한다.
    /// 외부에서는 세부 Combat Module을 직접 사용하지 않는다.
    /// </summary>
    [RequireComponent(typeof(WeaponSwapModule))]
    public class CombatModule : MonoBehaviour
    {
        private WeaponSwapModule _weaponSwapModule;

        // 현재 전투에 사용 가능한 무기를 대표 진입점으로 제공한다.
        public Weapon CurrentWeapon => _weaponSwapModule?.CurrentWeapon;
        public bool HasWeapon => CurrentWeapon != null;

        public EWeaponActionType ActiveActionType =>
            CurrentWeapon?.ActiveActionType ?? EWeaponActionType.None;

        public bool HasActiveAction =>
            CurrentWeapon != null && CurrentWeapon.HasActiveAction;

        private void Awake()
        {
            _weaponSwapModule = GetComponent<WeaponSwapModule>();
        }

        // Player 전투 기능과 런타임 무기 생성 의존성을 연결한다.
        public bool Bind(PlayerCore p_core)
        {
            if (p_core == null ||
                _weaponSwapModule == null ||
                !_weaponSwapModule.Bind(p_core.ResourceLoader))
            {
                Debug.LogError($"{nameof(CombatModule)}의 참조가 설정되지 않았습니다.", this);
                return false;
            }

            return true;
        }

        #region ============================== Weapon Swap
        // 공통 무기 교체 요청을 실제 무기 생성 Module에 전달한다.
        public bool ApplyWeaponChange(WeaponDTO p_weapon)
        {
            // 기존 무기가 교체되기 전에 진행 중인 행동을 정리한다.
            CancelWeaponAction();
            return _weaponSwapModule.Apply(p_weapon);
        }

        #endregion ============================== /Weapon Swap

        #region ============================== CombatAction
        // 현재 무기의 Action을 선택하고 행동을 시작한다.
        public bool TryBeginWeaponAction(EWeaponActionType p_type)
        {
            return CurrentWeapon != null &&
                   CurrentWeapon.TryBeginAction(p_type);
        }

        // 진행 중인 무기 행동을 갱신한다.
        public void TickWeaponAction(
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            CurrentWeapon?.TickAction(
                p_isInputHeld,
                p_isInputPressed,
                p_deltaTime);
        }

        public void CancelWeaponAction()
        {
            CurrentWeapon?.CancelAction();
        }
        #endregion ============================== /CombatAction
    }
}
