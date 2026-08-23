using Alpha.Combat;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 한 번의 원거리 공격에 필요한 최종 발사 정보를 보관한다.
    public readonly struct RangeAttackRequest
    {
        public Transform Attacker { get; }
        public Vector3 MuzzleOrigin { get; }
        public Vector3 Origin { get; }
        public Vector3 TargetPoint { get; }
        public Vector3 Direction { get; }
        public float Damage { get; }
        public float MaxDistance { get; }
        public AttackImpactInfo Impact { get; }

        public bool IsValid =>
            Attacker != null &&
            Direction.sqrMagnitude > 0.0001f &&
            Damage >= 0f &&
            MaxDistance > 0f;

        public RangeAttackRequest(
            Transform p_attacker,
            Vector3 p_muzzleOrigin,
            Vector3 p_origin,
            Vector3 p_targetPoint,
            Vector3 p_direction,
            float p_damage,
            float p_maxDistance,
            AttackImpactInfo p_impact = default)
        {
            Attacker = p_attacker;
            MuzzleOrigin = p_muzzleOrigin;
            Origin = p_origin;
            TargetPoint = p_targetPoint;

            Direction = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;

            Damage = p_damage;
            MaxDistance = p_maxDistance;
            Impact = p_impact;
        }
    }
}
