using Alpha.Item.Weapon.Range;
using Alpha.Projectile;
using Alpha.Utility;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Enemy
{
    // Range 패턴의 모든 FirePos에서 발사 방향을 계산하고 Projectile을 동시에 생성한다.
    public sealed class EnemyRangeAttackModule
    {
        public bool Execute(
            Transform p_owner,
            Transform p_target,
            EnemyAttackPatternSetting p_pattern)
        {
            if (p_owner == null ||
                p_pattern == null ||
                !p_pattern.IsExecutable ||
                (p_pattern.RangeDirectionType ==
                     EEnemyRangeDirectionType.Target &&
                 p_target == null))
            {
                return false;
            }

            bool hasConfiguredFirePosition = false;
            bool hasFired = false;

            for (int index = 0;
                 index < p_pattern.ProjectileSpawnPointSlotCount;
                 index++)
            {
                Transform firePosition =
                    p_pattern.GetProjectileSpawnPoint(index);

                if (firePosition == null)
                    continue;

                hasConfiguredFirePosition = true;
                hasFired |= TryFire(
                    p_owner,
                    p_target,
                    p_pattern,
                    firePosition.position,
                    firePosition.forward);
            }

            if (hasConfiguredFirePosition)
                return hasFired;

            // FirePos가 없으면 기존 Owner 기준 기본 위치와 +Z 방향을 사용한다.
            Vector3 fallbackOrigin = p_owner.TransformPoint(
                new Vector3(0f, 0.9f, 0.75f));

            return TryFire(
                p_owner,
                p_target,
                p_pattern,
                fallbackOrigin,
                p_owner.forward);
        }

        private static bool TryFire(
            Transform p_owner,
            Transform p_target,
            EnemyAttackPatternSetting p_pattern,
            Vector3 p_origin,
            Vector3 p_fireForward)
        {
            Vector3 direction;

            if (p_pattern.RangeDirectionType ==
                EEnemyRangeDirectionType.FirePositionForward)
            {
                direction = p_fireForward;
            }
            else
            {
                Vector3 targetPoint = ResolveTargetPoint(
                    p_target,
                    p_origin);

                direction = targetPoint - p_origin;
            }

            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            direction.Normalize();

            RangeAttackRequest request = new(
                p_owner,
                p_origin,
                p_origin,
                direction,
                p_pattern.DamageProfile.Damage,
                p_pattern.ProjectileMaximumDistance,
                p_pattern.DamageProfile.Impact);

            ProjectileEntity projectile = UnityEngine.Object.Instantiate(
                p_pattern.ProjectilePrefab,
                p_origin,
                Quaternion.LookRotation(direction));

            bool initialized = projectile.Initialize(request);

            if (initialized)
                return true;

            UnityEngine.Object.Destroy(projectile.gameObject);
            return false;
        }

        private static Vector3 ResolveTargetPoint(
            Transform p_target,
            Vector3 p_origin)
        {
            Collider targetCollider =
                p_target.GetComponent<Collider>() ??
                p_target.GetComponentInChildren<Collider>(true);

            return targetCollider != null && targetCollider.enabled
                ? ColliderPointUtility.GetClosestPoint(
                    targetCollider,
                    p_origin)
                : p_target.position;
        }
    }
}
