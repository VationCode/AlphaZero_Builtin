using Alpha.Item.Weapon.Range;
using Alpha.Projectile;
using Alpha.Utility;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Enemy
{
    // Range 패턴의 조준 방향을 계산하고 Projectile Entity를 생성한다.
    public sealed class EnemyRangeAttackModule
    {
        public bool Execute(
            Transform p_owner,
            Transform p_target,
            EnemyAttackPatternSetting p_pattern)
        {
            if (p_owner == null ||
                p_target == null ||
                p_pattern == null ||
                !p_pattern.IsExecutable)
            {
                return false;
            }

            Vector3 origin = p_pattern.ProjectileSpawnPoint != null
                ? p_pattern.ProjectileSpawnPoint.position
                : p_owner.TransformPoint(
                    new Vector3(0f, 0.9f, 0.75f));

            Vector3 targetPoint = ResolveTargetPoint(
                p_target,
                origin);

            Vector3 direction = targetPoint - origin;

            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            direction.Normalize();

            ProjectileLaunchSettings launchSettings =
                p_pattern.ProjectileLaunchSettings;
            float projectileTravelDistance =
                launchSettings.Speed * launchSettings.Lifetime;

            RangeAttackRequest request = new(
                p_owner,
                origin,
                origin,
                targetPoint,
                direction,
                p_pattern.DamageProfile.Damage,
                projectileTravelDistance,
                p_pattern.DamageProfile.Impact);

            ProjectileEntity projectile = UnityEngine.Object.Instantiate(
                launchSettings.Prefab,
                origin,
                Quaternion.LookRotation(direction));

            bool initialized = projectile.Initialize(
                request,
                launchSettings);

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
