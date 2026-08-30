using System;
using System.Collections.Generic;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 여러 Ray 탄도를 검사하고 같은 대상의 피해를 합산해 한 번만 전달한다.
    internal sealed class HitscanAttackModule
    {
        private readonly Dictionary<IDamageable, HitAggregate> _hitAggregates =
            new();

        public bool Execute(
            in RangeAttackRequest p_request,
            LayerMask p_hitMask,
            Action<RangeAttackResult> p_publishTrajectory,
            Action<RangeHitResult> p_publishHit)
        {
            _hitAggregates.Clear();

            for (int index = 0;
                 index < p_request.TrajectoryCount;
                 index++)
            {
                CollectRayTrajectory(
                    p_request,
                    RangeWeaponAttackModule.ResolveSpreadDirection(p_request),
                    p_hitMask,
                    p_publishTrajectory,
                    p_publishHit);
            }

            float damagePerProjectile =
                p_request.Damage / p_request.TrajectoryCount;

            foreach (HitAggregate aggregate in _hitAggregates.Values)
            {
                DamageInfo damageInfo = new(
                    p_request.Attacker,
                    damagePerProjectile * aggregate.HitCount,
                    aggregate.AveragePoint,
                    aggregate.AverageNormal,
                    aggregate.AverageDirection,
                    p_impact: p_request.Impact,
                    p_deliveryType: EDamageDeliveryType.Ranged);

                DamageSystem.TryApply(aggregate.Collider, damageInfo);
            }

            // 빗나가거나 지형에 맞아도 발사 자체는 성공이다.
            return true;
        }

        private void CollectRayTrajectory(
            in RangeAttackRequest p_request,
            Vector3 p_direction,
            LayerMask p_hitMask,
            Action<RangeAttackResult> p_publishTrajectory,
            Action<RangeHitResult> p_publishHit)
        {
            Ray attackRay = new(p_request.Origin, p_direction);

            bool hasHit = Physics.Raycast(
                attackRay,
                out RaycastHit hit,
                p_request.MaxDistance,
                p_hitMask,
                QueryTriggerInteraction.Ignore);

            Vector3 endPoint = hasHit
                ? hit.point
                : p_request.Origin + p_direction * p_request.MaxDistance;

            p_publishTrajectory?.Invoke(new RangeAttackResult(
                p_request.MuzzleOrigin,
                endPoint,
                hasHit,
                hasHit ? hit.normal : -p_direction));

            if (hasHit)
            {
                p_publishHit?.Invoke(new RangeHitResult(
                    hit.point,
                    hit.normal));
            }

            if (!hasHit ||
                !DamageSystem.TryGetDamageable(
                    hit.collider,
                    out IDamageable damageable))
            {
                return;
            }

            AddHit(
                damageable,
                hit.collider,
                hit.point,
                hit.normal,
                p_direction);
        }

        private void AddHit(
            IDamageable p_damageable,
            Collider p_collider,
            Vector3 p_point,
            Vector3 p_normal,
            Vector3 p_direction)
        {
            if (!_hitAggregates.TryGetValue(
                    p_damageable,
                    out HitAggregate aggregate))
            {
                aggregate = new HitAggregate(p_collider);
            }

            aggregate.Add(p_point, p_normal, p_direction);
            _hitAggregates[p_damageable] = aggregate;
        }

        private struct HitAggregate
        {
            private Vector3 _pointSum;
            private Vector3 _normalSum;
            private Vector3 _directionSum;

            public Collider Collider { get; }
            public int HitCount { get; private set; }
            public Vector3 AveragePoint =>
                HitCount > 0 ? _pointSum / HitCount : Vector3.zero;
            public Vector3 AverageNormal =>
                _normalSum.sqrMagnitude > 0.0001f
                    ? _normalSum.normalized
                    : Vector3.up;
            public Vector3 AverageDirection =>
                _directionSum.sqrMagnitude > 0.0001f
                    ? _directionSum.normalized
                    : Vector3.forward;

            public HitAggregate(Collider p_collider)
            {
                Collider = p_collider;
                HitCount = 0;
                _pointSum = Vector3.zero;
                _normalSum = Vector3.zero;
                _directionSum = Vector3.zero;
            }

            public void Add(
                Vector3 p_point,
                Vector3 p_normal,
                Vector3 p_direction)
            {
                HitCount++;
                _pointSum += p_point;
                _normalSum += p_normal;
                _directionSum += p_direction;
            }
        }
    }
}
