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

        public bool IsBound { get; private set; }

        // 무기 교체 요청을 기록할 CombatContext를 연결한다.
        public bool Bind(CombatContext p_combatContext)
        {
            if (p_combatContext == null)
            {
                Debug.LogError($"{nameof(WeaponSwapModule)}의 참조가 설정되지 않았습니다.", this);
                return false;
            }

            _combatContext = p_combatContext;


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

            // 유효한 숫자 입력을 무기 타입으로 변환해 다음 State가 사용할 Pending 값으로 둔다.
            EWeaponType weaponType = (EWeaponType)p_slotIndex;

            _combatContext.PendingWeaponType = weaponType;

            return true;
        }

        // 현재 직접 구독하는 이벤트는 없어 별도 해제 처리가 없다.
        private void OnDestroy()
        {
            
        }
    }
}
