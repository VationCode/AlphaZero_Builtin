using UnityEngine;

namespace Alpha.Combat
{
    // 한 번의 피해 적용에 필요한 정보를 보관한다.
    public readonly struct DamageInfo
    {
        public Transform Attacker { get; }
        public float Amount { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public Vector3 Direction { get; }
        public EDamageType DamageType { get; }
        public EHitReaction HitReaction { get; }
        public float KnockbackDistance { get; }
        public float KnockbackDuration { get; }

        public bool IsValid =>
            Attacker != null &&
            Amount > 0f;

        public DamageInfo(
            Transform p_attacker,
            float p_amount,
            Vector3 p_hitPoint,
            Vector3 p_hitNormal,
            Vector3 p_direction,
            EDamageType p_damageType = EDamageType.Physical,
            EHitReaction p_hitReaction = EHitReaction.None,
            float p_knockbackDistance = 0f,
            float p_knockbackDuration = 0f)
        {
            Attacker = p_attacker;
            Amount = p_amount;
            HitPoint = p_hitPoint;
            HitNormal = p_hitNormal;
            DamageType = p_damageType;
            HitReaction = p_hitReaction;
            KnockbackDistance = Mathf.Max(0f, p_knockbackDistance);
            KnockbackDuration = Mathf.Max(0f, p_knockbackDuration);

            Direction = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;
        }
    }
}
