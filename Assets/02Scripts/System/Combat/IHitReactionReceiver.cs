using UnityEngine;

namespace Alpha.Combat
{
    // 대상 Entity가 면역·행동 상태를 확인하고 피격 반응을 실행하는 계약이다.
    public interface IHitReactionReceiver
    {
        bool TryApplyHitReaction(in AttackImpactInfo p_impact, Transform p_attacker, Vector3 p_direction);
    }
}
