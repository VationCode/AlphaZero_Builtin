using UnityEngine;

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

        public bool IsBound { get; private set; }

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        private void Awake()
        {
            _weaponSwapModule = GetComponent<WeaponSwapModule>();
        }

        // Player 전투 기능을 사용할 수 있도록 대표 Module을 활성화한다.
        public bool Bind(PlayerCore p_core)
        {
            if (p_core == null || _weaponSwapModule == null)
            {
                Debug.LogError($"{nameof(CombatModule)}의 참조가 설정되지 않았습니다.", this);
                return false;
            }

            IsBound = true;
            return true;
        }

        #region ============================== Weapon Swap
        // 세부 Module에 무기 교체 대상 준비를 위임한다.
        public bool TryPrepareWeaponSwap(int p_slotIndex)
        {
            return IsBound &&
                   _weaponSwapModule.TryPrepare(p_slotIndex);
        }

        #endregion ============================== /Weapon Swap

        // 외부 전투 요청을 더 이상 받지 않도록 연결 상태를 해제한다.
        public void Unbind()
        {
            IsBound = false;
        }

        // 객체 해제 시 등록한 이벤트와 참조를 정리한다.
        private void OnDestroy()
        {
            Unbind();
        }
    }
}
