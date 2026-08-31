using Alpha.Projectile;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Item.Weapon.Range
{
    // Effect Prefab이 아닌 이동·충돌·피해를 소유한 Projectile Entity를 생성한다.
    internal sealed class ProjectileAttackModule
    {
        public bool Execute(
            in RangeAttackRequest p_request,
            ProjectileAttackSettings p_settings,
            LayerMask p_hitMask,
            System.Action<ProjectileEntity> p_publishProjectile)
        {
            if (p_settings == null || !p_settings.IsValid)
            {
                return false;
            }

            ProjectileLaunchSettings launchSettings =
                p_settings.CreateLaunchSettings(p_hitMask);

            if (!launchSettings.Prefab.IsConfigurationValid)
                return false;

            float damagePerProjectile =
                p_request.Damage / p_request.TrajectoryCount;
            bool didLaunch = false;

            for (int index = 0;
                 index < p_request.TrajectoryCount;
                 index++)
            {
                Vector3 launchDirection =
                    RangeWeaponAttackModule.ResolveSpreadDirection(p_request);
                RangeAttackRequest launchRequest =
                    p_request.CreateTrajectory(
                        launchDirection,
                        damagePerProjectile);

                ProjectileEntity projectile = Object.Instantiate(
                    launchSettings.Prefab,
                    p_request.Origin,
                    Quaternion.LookRotation(launchDirection));

                if (projectile.Initialize(launchRequest, launchSettings))
                {
                    didLaunch = true;
                    p_publishProjectile?.Invoke(projectile);
                    continue;
                }

                Object.Destroy(projectile.gameObject);
            }

            return didLaunch;
        }

    }
}
