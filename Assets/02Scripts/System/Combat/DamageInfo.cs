using UnityEngine;

namespace Alpha.Combat
{
    // 공격이 근접 또는 원거리 중 어떤 방식으로 전달됐는지 구분한다.
    public enum EDamageDeliveryType
    {
        Unknown,
        Melee,
        Ranged
    }

    // 한 번의 피해 적용에 필요한 정보를 보관한다.
    public readonly struct DamageInfo
    {
        public Transform Attacker { get; }
        public float Amount { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public Vector3 Direction { get; }
        public EDamageDeliveryType DeliveryType { get; }
        public AttackImpactInfo Impact { get; }
        public EHitType HitType => Impact.HitType;

        public bool IsValid =>
            Attacker != null &&
            Amount > 0f;

        public DamageInfo(
            Transform p_attacker,
            float p_amount,
            Vector3 p_hitPoint,
            Vector3 p_hitNormal,
            Vector3 p_direction,
            AttackImpactInfo p_impact = default,
            EDamageDeliveryType p_deliveryType =
                EDamageDeliveryType.Unknown)
        {
            Attacker = p_attacker;
            Amount = p_amount;
            HitPoint = p_hitPoint;
            HitNormal = p_hitNormal;
            DeliveryType = p_deliveryType;
            Impact = p_impact;

            Direction = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;
        }
    }
}
