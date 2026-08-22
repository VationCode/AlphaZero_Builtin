using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Enemy
{
    // Rush 패턴의 목적지 이동과 돌진 중 대상별 1회 피해를 수행한다.
    public sealed class EnemyRushAttackModule
    {
        private const int HitBufferCapacity = 32;

        private readonly Collider[] _overlapBuffer =
            new Collider[HitBufferCapacity];

        private readonly DetectionAreaHit[] _hitBuffer =
            new DetectionAreaHit[HitBufferCapacity];

        private readonly HashSet<IDamageable> _damagedTargets = new();

        private Vector3 _destination;

        public bool IsActive { get; private set; }

        public void Begin(
            Transform p_owner,
            Transform p_target,
            EnemyAttackPatternSetting p_pattern)
        {
            if (p_owner == null || p_pattern == null)
            {
                End();
                return;
            }

            Vector3 direction = p_target != null
                ? Vector3.ProjectOnPlane(
                    p_target.position - p_owner.position,
                    Vector3.up)
                : Vector3.zero;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = p_owner.forward;

            _destination = p_owner.position +
                           direction.normalized *
                           p_pattern.RushDistance;

            _damagedTargets.Clear();
            IsActive = true;
        }

        public void Tick(
            Transform p_owner,
            EnemyLocomotionModule p_locomotion,
            EnemyAttackPatternSetting p_pattern,
            float p_deltaTime)
        {
            if (!IsActive ||
                p_owner == null ||
                p_locomotion == null ||
                p_pattern == null)
            {
                return;
            }

            p_locomotion.MoveTo(
                _destination,
                p_deltaTime,
                p_pattern.RushSpeed);

            ApplyDamage(p_owner, p_pattern);
        }

        public void End()
        {
            IsActive = false;
            _damagedTargets.Clear();
        }

        private void ApplyDamage(
            Transform p_owner,
            EnemyAttackPatternSetting p_pattern)
        {
            Physics.SyncTransforms();

            DetectionAreaRequest request = new(
                p_owner.position,
                p_owner.forward,
                p_owner.up,
                p_owner,
                p_pattern.RushArea);

            int hitCount = DetectionAreaSystem.Query(
                request,
                _overlapBuffer,
                _hitBuffer);

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
                    hit.Direction,
                    profile.DamageType,
                    profile.HitReaction,
                    profile.KnockbackDistance,
                    profile.KnockbackDuration,
                    EDamageDeliveryType.Melee);

                DamageSystem.TryApply(
                    hit.Collider,
                    damageInfo);
            }
        }
    }
}
