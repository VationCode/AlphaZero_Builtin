using UnityEngine;

namespace Alpha.Combat
{
    // 여러 Collider가 공유하는 한 번의 공격 실행 정보다.
    public readonly struct AttackSession
    {
        public int AttackId { get; }
        public Transform Attacker { get; }
        public DamageProfile Profile { get; }

        public AttackSession(
            int p_attackId,
            Transform p_attacker,
            DamageProfile p_profile)
        {
            AttackId = p_attackId;
            Attacker = p_attacker;
            Profile = p_profile;
        }
    }
}
