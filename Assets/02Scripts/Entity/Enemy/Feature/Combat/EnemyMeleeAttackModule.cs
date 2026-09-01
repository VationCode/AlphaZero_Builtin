using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Enemy
{
    // Melee, Area, Arena가 공유하는 직접 범위 판정과 1회 피해를 수행한다.
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
            return Execute(
                p_owner,
                p_pattern,
                p_pattern?.MeleeArea);
        }

        public bool Execute(
            Transform p_owner,
            EnemyAttackPatternSetting p_pattern,
            DetectionAreaSettings p_area)
        {
            if (p_owner == null ||
                p_pattern == null ||
                !p_pattern.IsExecutable ||
                p_area == null ||
                !p_area.IsValid)
            {
                return false;
            }

            Physics.SyncTransforms();

            DetectionAreaRequest request = new(
                p_owner.position,
                p_owner.forward,
                p_owner.up,
                p_owner,
                p_area);

            int hitCount = DetectionAreaSystem.CollectHits(
                request,
                _overlapBuffer,
                _hitBuffer);

            bool hasAppliedDamage = false;
            _damagedTargets.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];
                if (!DamageSystem.TryGetDamageable(
                        hit.Collider,
                        out IDamageable damageable) ||
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
                    p_owner.forward,
                    p_impact: profile.Impact,
                    p_deliveryType: EDamageDeliveryType.Melee);

                if (!DamageSystem.TryApply(
                        hit.Collider,
                        damageInfo))
                {
                    continue;
                }

                hasAppliedDamage = true;
            }

            return hasAppliedDamage;
        }
    }
}
