using UnityEngine;

namespace Alpha.Combat
{
    // 범위 안에서 찾은 하나의 Damage 대상과 대표 타격 지점이다.
    public readonly struct AttackAreaHit
    {
        public Collider Collider { get; }
        public IDamageable Damageable { get; }
        public Transform Target { get; }
        public Vector3 HitPoint { get; }
        public Vector3 Direction { get; }

        public bool IsValid => Collider != null && Damageable != null;

        public AttackAreaHit(
            Collider p_collider,
            IDamageable p_damageable,
            Transform p_target,
            Vector3 p_hitPoint,
            Vector3 p_direction)
        {
            Collider = p_collider;
            Damageable = p_damageable;
            Target = p_target;
            HitPoint = p_hitPoint;
            Direction = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;
        }
    }
}
