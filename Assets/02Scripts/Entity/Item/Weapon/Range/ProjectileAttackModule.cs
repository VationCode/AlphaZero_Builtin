using Alpha.Projectile;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Item.Weapon.Range
{
    // 조준 미리보기가 사용할 계산된 궤적 수와 예상 충돌 정보를 보관한다.
    public readonly struct ProjectileTrajectoryResult
    {
        public int PointCount { get; }
        public bool HasImpact { get; }
        public Vector3 ImpactPoint { get; }
        public Vector3 ImpactNormal { get; }

        public ProjectileTrajectoryResult(
            int p_pointCount,
            bool p_hasImpact,
            Vector3 p_impactPoint,
            Vector3 p_impactNormal)
        {
            PointCount = Mathf.Max(0, p_pointCount);
            HasImpact = p_hasImpact;
            ImpactPoint = p_impactPoint;
            ImpactNormal = p_impactNormal;
        }
    }

    // Projectile 생성·발사와 같은 구체적인 실행만 담당한다.
    public sealed class ProjectileAttackModule
    {
        public bool Execute(
            in RangeAttackRequest p_request,
            ProjectileLaunchSettings p_settings)
        {
            if (!p_settings.IsValid ||
                !p_settings.Prefab.IsConfigurationValid)
            {
                return false;
            }

            float damagePerProjectile =
                p_request.Damage / p_request.ProjectilesPerShot;
            bool didLaunch = false;

            for (int index = 0;
                 index < p_request.ProjectilesPerShot;
                 index++)
            {
                Vector3 launchDirection =
                    RangeAttackModule.ResolveSpreadDirection(p_request);
                RangeAttackRequest launchRequest =
                    p_request.CreateTrajectory(
                        launchDirection,
                        damagePerProjectile);

                ProjectileEntity projectile = Object.Instantiate(
                    p_settings.Prefab,
                    p_request.Origin,
                    Quaternion.LookRotation(launchDirection));

                if (projectile.Initialize(launchRequest, p_settings))
                {
                    didLaunch = true;
                    continue;
                }

                Object.Destroy(projectile.gameObject);
            }

            return didLaunch;
        }

        // 실제 Projectile의 이동·충돌·종료 조건으로 조준 궤적을 미리 계산한다.
        public bool TryPredictTrajectory(
            ProjectileLaunchSettings p_settings,
            Vector3 p_origin,
            Vector3 p_direction,
            float p_maxDistance,
            float p_simulationStep,
            Vector3[] p_points,
            out ProjectileTrajectoryResult p_result)
        {
            p_result = default;

            if (!p_settings.IsValid ||
                !p_settings.Prefab.IsConfigurationValid ||
                p_direction.sqrMagnitude <= 0.0001f ||
                p_maxDistance <= 0f ||
                p_simulationStep <= 0f ||
                p_points == null ||
                p_points.Length < 2)
            {
                return false;
            }

            ProjectileEntity projectilePrefab = p_settings.Prefab;
            Vector3 position = p_origin;
            Vector3 velocity =
                p_direction.normalized * p_settings.Speed;
            Vector3 gravity =
                Physics.gravity * projectilePrefab.GravityScale;
            float simulationStep = ResolveSimulationStep(
                p_simulationStep,
                p_maxDistance,
                p_settings.Speed,
                gravity.magnitude,
                p_points.Length);
            int pointCount = 1;

            p_points[0] = position;

            while (pointCount < p_points.Length)
            {
                Vector3 displacement =
                    ProjectileEntity.CalculateDisplacement(
                        velocity,
                        gravity,
                        simulationStep);
                float requestedDistance = displacement.magnitude;

                if (requestedDistance <= 0.0001f)
                {
                    break;
                }

                Vector3 moveDirection =
                    displacement / requestedDistance;
                float distanceToBoundary =
                    ProjectileEntity.CalculateDistanceToRangeBoundary(
                        position,
                        p_origin,
                        moveDirection,
                        p_maxDistance);

                if (distanceToBoundary <= 0f)
                    break;

                float moveDistance = Mathf.Min(
                    requestedDistance,
                    distanceToBoundary);
                bool reachesMaximumDistance =
                    moveDistance >= distanceToBoundary - 0.0001f;
                Quaternion flightRotation =
                    Quaternion.LookRotation(velocity.normalized);
                Vector3 collisionCenter =
                    projectilePrefab.CalculateCollisionCenter(
                        position,
                        flightRotation);
                Ray movementRay = new(collisionCenter, moveDirection);

                if (TryCastTrajectory(
                        p_settings,
                        movementRay,
                        moveDistance,
                        projectilePrefab.CollisionRadius,
                        out RaycastHit hit))
                {
                    p_points[pointCount++] = hit.point;
                    p_result = new ProjectileTrajectoryResult(
                        pointCount,
                        true,
                        hit.point,
                        hit.normal);
                    return true;
                }

                position += moveDirection * moveDistance;
                p_points[pointCount++] = position;

                if (reachesMaximumDistance)
                    break;

                velocity += gravity * simulationStep;
            }

            p_result = new ProjectileTrajectoryResult(
                pointCount,
                false,
                Vector3.zero,
                Vector3.zero);
            return pointCount > 1;
        }

        // 느린 Projectile도 Point Buffer 안에서 실제 MaxDistance 경계까지 표시한다.
        private static float ResolveSimulationStep(
            float p_requestedStep,
            float p_maxDistance,
            float p_speed,
            float p_gravityMagnitude,
            int p_pointCapacity)
        {
            float maximumFlightDuration;

            if (p_gravityMagnitude <= 0.0001f)
            {
                maximumFlightDuration =
                    p_maxDistance / Mathf.Max(0.01f, p_speed);
            }
            else
            {
                // 중력 반대 방향으로 발사했을 때가 거리 경계 도달 시간이 가장 길다.
                maximumFlightDuration =
                    (p_speed + Mathf.Sqrt(
                        p_speed * p_speed +
                        2f * p_gravityMagnitude * p_maxDistance)) /
                    p_gravityMagnitude;
            }

            float requiredStep =
                maximumFlightDuration /
                Mathf.Max(1, p_pointCapacity - 1);

            return Mathf.Max(p_requestedStep, requiredStep);
        }

        public bool TryGetRadialDamageRadius(
            ProjectileLaunchSettings p_settings,
            out float p_radius)
        {
            p_radius = 0f;

            if (!p_settings.IsValid)
                return false;

            ProjectileImpactSettings impactSettings =
                p_settings.Prefab.ImpactSettings;

            if (!impactSettings.IsRadial ||
                impactSettings.DamageRadius <= 0f)
            {
                return false;
            }

            p_radius = impactSettings.DamageRadius;
            return true;
        }

        private static bool TryCastTrajectory(
            ProjectileLaunchSettings p_settings,
            Ray p_ray,
            float p_distance,
            float p_collisionRadius,
            out RaycastHit p_hit)
        {
            if (p_collisionRadius > 0f)
            {
                return Physics.SphereCast(
                    p_ray,
                    p_collisionRadius,
                    out p_hit,
                    p_distance,
                    p_settings.HitMask,
                    ProjectileEntity.CollisionTriggerInteraction);
            }

            return Physics.Raycast(
                p_ray,
                out p_hit,
                p_distance,
                p_settings.HitMask,
                ProjectileEntity.CollisionTriggerInteraction);
        }
    }
}
