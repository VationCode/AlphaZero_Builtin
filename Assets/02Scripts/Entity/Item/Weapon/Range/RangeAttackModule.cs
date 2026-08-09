using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // RangeWeapon이 선택한 실제 공격 방식을 실행하는 기반 Module이다.
    public abstract class RangeAttackModule : MonoBehaviour
    {
        // 공통 요청 검증 후 구체 공격 방식에 실행을 위임한다.
        public bool TryExecute(in RangeAttackRequest p_request)
        {
            return p_request.IsValid &&
                   OnExecute(p_request);
        }

        // Hitscan과 Projectile이 각각 실제 공격을 구현한다.
        protected abstract bool OnExecute(
            in RangeAttackRequest p_request);
    }
}
