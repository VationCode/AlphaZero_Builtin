using UnityEngine;

namespace Alpha.Combat
{
    // 명중 Collider에서 넉백 계약을 찾아 유효한 대상에게 요청을 전달한다.
    public static class KnockbackSystem
    {
        public static bool TryApply(
            Collider p_target,
            in KnockbackInfo p_knockbackInfo)
        {
            if (p_target == null || !p_knockbackInfo.IsValid)
                return false;

            Transform targetTransform = p_target.transform;

            if (targetTransform == p_knockbackInfo.Attacker ||
                targetTransform.IsChildOf(p_knockbackInfo.Attacker))
            {
                return false;
            }

            IKnockbackable knockbackable =
                CombatTargetResolver.Find<IKnockbackable>(p_target);

            return knockbackable != null &&
                   knockbackable.CanReceiveKnockback &&
                   knockbackable.TryApplyKnockback(p_knockbackInfo);
        }
    }
}
