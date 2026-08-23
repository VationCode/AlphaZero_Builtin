using System;
using UnityEngine;

namespace Alpha.Combat
{
    // 공격 대상을 찾아 공통 피해 계약으로 전달한다.
    public static class DamageSystem
    {
        // 실제 체력 감소가 확정된 공격만 공격자 측 후속 연출에 알린다.
        public static event Action<Collider, DamageInfo> OnDamageApplied;

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
                CombatTargetResolver.Find<IDamageable>(p_target);

            if (damageable == null ||
                !damageable.TryApplyDamage(p_damageInfo))
            {
                return false;
            }

            OnDamageApplied?.Invoke(
                p_target,
                p_damageInfo);

            return true;
        }

        // 다중 Collider 공격이 같은 Entity를 한 번만 처리할 수 있도록 계약 조회를 제공한다.
        public static bool TryGetDamageable(
            Collider p_target,
            out IDamageable p_damageable)
        {
            p_damageable =
                CombatTargetResolver.Find<IDamageable>(p_target);

            return p_damageable != null;
        }
    }

    // 명중 Collider를 기준으로 Entity가 소유한 전투 계약을 찾는다.
    internal static class CombatTargetResolver
    {
        public static T Find<T>(Component p_target) where T : class
        {
            if (p_target == null)
                return null;

            // Hitbox 자체 또는 부모가 계약을 소유하는 일반 구조를 먼저 확인한다.
            T contract = p_target.GetComponentInParent<T>();

            if (contract != null)
                return contract;

            // Rigidbody 루트를 Entity 경계로 우선 사용하고, CharacterController처럼
            // Rigidbody가 없는 대상은 최상위 Transform의 자식 Feature를 조회한다.
            Collider targetCollider = p_target as Collider;
            Transform entityRoot = targetCollider?.attachedRigidbody != null
                ? targetCollider.attachedRigidbody.transform
                : p_target.transform.root;

            return entityRoot != null
                ? entityRoot.GetComponentInChildren<T>(true)
                : null;
        }
    }
}
