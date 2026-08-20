using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Enemy
{
    // Melee 패턴의 공간 탐지와 대상별 1회 피해 적용을 수행한다.
    public sealed class EnemyMeleeAttackModule
    {
        private const int HitBufferCapacity = 32;

        private readonly Collider[] _overlapBuffer =
            new Collider[HitBufferCapacity];

        private readonly DetectionAreaHit[] _hitBuffer =
            new DetectionAreaHit[HitBufferCapacity];

        private readonly HashSet<IDamageable> _damagedTargets = new();

        public bool Execute(
            Transform p_owner,
            EnemyAttackPatternSetting p_pattern)
        {
            if (p_owner == null ||
                p_pattern == null ||
                !p_pattern.IsExecutable)
            {
                return false;
            }

            Physics.SyncTransforms();

            DetectionAreaRequest request = new(
                p_owner.position,
                p_owner.forward,
                p_owner.up,
                p_owner,
                p_pattern.MeleeArea);

            int hitCount = DetectionAreaSystem.Query(
                request,
                _overlapBuffer,
                _hitBuffer);

            bool hasAppliedDamage = false;
            _damagedTargets.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];
                IDamageable damageable =
                    hit.Collider.GetComponentInParent<IDamageable>();

                if (damageable == null ||
                    !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                DamageProfile profile = p_pattern.DamageProfile;
                DamageInfo damageInfo = new(
                    p_owner,
                    profile.Damage,
                    hit.HitPoint,
                    -hit.Direction,
                    hit.Direction,
                    profile.DamageType,
                    profile.HitReaction,
                    profile.KnockbackDistance,
                    profile.KnockbackDuration);

                hasAppliedDamage |= DamageSystem.TryApply(
                    hit.Collider,
                    damageInfo);
            }

            return hasAppliedDamage;
        }
    }
}
