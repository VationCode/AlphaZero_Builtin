using UnityEngine;

namespace Alpha.Combat
{
    // 공격 대상을 찾아 공통 피해 계약으로 전달한다.
    public static class DamageSystem
    {
        public static bool TryApply(
            Collider p_target,
            in DamageInfo p_damageInfo)
        {
            if (p_target == null ||
                !p_damageInfo.IsValid)
            {
                return false;
            }

            Transform targetTransform =
                p_target.transform;

            // 공격자 자신의 Collider에는 피해를 적용하지 않는다.
            if (targetTransform == p_damageInfo.Attacker ||
                targetTransform.IsChildOf(p_damageInfo.Attacker))
            {
                return false;
            }

            IDamageable damageable =
                p_target.GetComponentInParent<IDamageable>();

            return damageable != null &&
                   damageable.TryApplyDamage(p_damageInfo);
        }
    }
}
