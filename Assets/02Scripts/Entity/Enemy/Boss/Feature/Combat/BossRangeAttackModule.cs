using Alpha.Item.Weapon.Range;
using UnityEngine;

namespace Alpha.Boss
{
    // 그룹의 유효한 발사점마다 투사체 또는 지면파 하나를 생성한다. 생성 후 수명은 각 Entity가 소유한다.
    public sealed class BossRangeAttackModule
    {
        public bool IsConfigured(BossRangeAttackSettings p_settings)
        {
            if (p_settings == null || p_settings.FireGroupCount == 0 ||
                !IsFinite(p_settings.MaximumDistance) || p_settings.MaximumDistance <= 0f)
                return false;

            bool validPrefab = p_settings.AttackMode switch
            {
                EBossRangeAttackMode.Projectile => p_settings.ProjectilePrefab != null &&
                    p_settings.ProjectilePrefab.IsConfigurationValid,
                EBossRangeAttackMode.GroundWave => p_settings.GroundWavePrefab != null &&
                    p_settings.GroundWavePrefab.IsConfigurationValid,
                _ => false
            };
            if (!validPrefab)
                return false;

            for (int index = 0; index < p_settings.FireGroupCount; index++)
            {
                BossRangeFireGroup group = p_settings.GetFireGroup(index);
                if (group == null || !group.HasSpawnPoint || !IsFinite(group.FireTimeSeconds) ||
                    group.FireTimeSeconds < 0f)
                    return false;
            }
            return true;
        }

        public bool Execute(Transform p_owner, Transform p_target, BossPatternData p_pattern, int p_groupIndex)
        {
            BossRangeAttackSettings settings = p_pattern?.RangeAttack;
            BossRangeFireGroup group = settings?.GetFireGroup(p_groupIndex);
            if (p_owner == null || group == null || !IsConfigured(settings) ||
                (settings.DirectionType == EBossRangeDirectionType.Target && p_target == null))
                return false;

            // 그룹 내 모든 발사점이 같은 타겟 위치를 사용한다.
            Vector3 targetPoint = ResolveTargetPoint(p_target);
            bool fired = false;
            for (int index = 0; index < group.SpawnPointCount; index++)
            {
                Transform spawn = group.GetSpawnPoint(index);
                if (spawn == null)
                    continue;
                Vector3 origin = spawn.position;
                Vector3 direction = settings.DirectionType == EBossRangeDirectionType.Target
                    ? targetPoint - origin : spawn.forward;
                if (direction.sqrMagnitude <= 0.0001f)
                    continue;

                if (settings.AttackMode == EBossRangeAttackMode.GroundWave)
                {
                    var wave = Object.Instantiate(settings.GroundWavePrefab, origin, Quaternion.identity);
                    bool initialized = wave.Initialize(p_owner, origin, direction,
                        p_pattern.DamageProfile.Damage, settings.MaximumDistance, p_pattern.DamageProfile.Impact);
                    fired |= initialized;
                    if (!initialized)
                        Object.Destroy(wave.gameObject);
                    continue;
                }

                Vector3 shotDirection = direction.normalized;
                var projectile = Object.Instantiate(settings.ProjectilePrefab, origin,
                    Quaternion.LookRotation(shotDirection));
                RangeAttackRequest request = new(p_owner, origin, origin, shotDirection,
                    p_pattern.DamageProfile.Damage, settings.MaximumDistance, p_pattern.DamageProfile.Impact);
                bool projectileInitialized = projectile.Initialize(request);
                fired |= projectileInitialized;
                if (!projectileInitialized)
                    Object.Destroy(projectile.gameObject);
            }
            return fired;
        }

        private static Vector3 ResolveTargetPoint(Transform p_target)
        {
            if (p_target == null)
                return Vector3.zero;
            Collider collider = p_target.GetComponent<Collider>() ?? p_target.GetComponentInChildren<Collider>();
            return collider != null && collider.enabled ? collider.bounds.center : p_target.position;
        }

        private static bool IsFinite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
    }
}
