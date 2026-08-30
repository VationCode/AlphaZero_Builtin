using System;
using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Utility;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 발사 지점부터 유효 거리까지 관통 영역을 투영해 내부 대상 모두에게 피해를 준다.
    internal sealed class PenetrationAttackModule
    {
        private const int PathHitCapacity = 64;
        private const int VolumeCandidateCapacity = 128;

        private readonly Dictionary<IDamageable, HitAggregate> _hitAggregates =
            new();
        private readonly HashSet<IDamageable> _trajectoryTargets = new();
        private readonly RaycastHit[] _pathHits =
            new RaycastHit[PathHitCapacity];
        private readonly Collider[] _volumeCandidates =
            new Collider[VolumeCandidateCapacity];

        public bool Execute(
            in RangeAttackRequest p_request,
            PenetrationAttackSettings p_settings,
            LayerMask p_hitMask,
            Action<RangeAttackResult> p_publishTrajectory)
        {
            if (p_settings == null || !p_settings.IsValid)
            {
                return false;
            }

            _hitAggregates.Clear();

            for (int index = 0;
                 index < p_request.TrajectoryCount;
                 index++)
            {
                CollectPenetrationTrajectory(
                    p_request,
                    RangeWeaponAttackModule.ResolveSpreadDirection(p_request),
                    p_settings,
                    p_hitMask,
                    p_publishTrajectory);
            }

            float damagePerTrajectory =
                p_request.Damage / p_request.TrajectoryCount;

            foreach (HitAggregate aggregate in _hitAggregates.Values)
            {
                DamageInfo damageInfo = new(
                    p_request.Attacker,
                    damagePerTrajectory * aggregate.HitCount,
                    aggregate.AveragePoint,
                   aggregate.AverageNormal,
                   aggregate.AverageDirection,
                   p_impact: p_request.Impact,
                    p_deliveryType: EDamageDeliveryType.Ranged);

                DamageSystem.TryApply(aggregate.Collider, damageInfo);
            }

            return true;
        }

        // 적은 서로 가리지 않고 중앙 경로를 막는 벽까지만 대상을 수집한다.
        private void CollectPenetrationTrajectory(
            in RangeAttackRequest p_request,
            Vector3 p_direction,
            PenetrationAttackSettings p_settings,
            LayerMask p_hitMask,
            Action<RangeAttackResult> p_publishTrajectory)
        {
            float effectiveDistance = ResolveEffectiveDistance(
                p_request,
                p_direction,
                p_hitMask,
                out bool hasCollision,
                out Vector3 collisionNormal);

            if (effectiveDistance <= 0f)
                return;

            float effectiveEndRadius = EvaluateRadius(
                effectiveDistance,
                p_request.MaxDistance,
                p_settings);
            float broadphaseRadius = Mathf.Max(
                p_settings.StartRadius,
                effectiveEndRadius);
            Vector3 endPoint =
                p_request.Origin + p_direction * effectiveDistance;

            p_publishTrajectory?.Invoke(new RangeAttackResult(
                p_request.MuzzleOrigin,
                endPoint,
                hasCollision,
                collisionNormal));

            int candidateCount = Physics.OverlapCapsuleNonAlloc(
                p_request.Origin,
                endPoint,
                broadphaseRadius,
                _volumeCandidates,
                p_hitMask,
                QueryTriggerInteraction.Ignore);

            _trajectoryTargets.Clear();

            for (int index = 0; index < candidateCount; index++)
            {
                Collider candidate = _volumeCandidates[index];

                if (candidate == null ||
                    IsAttackerCollider(candidate, p_request.Attacker) ||
                    !DamageSystem.TryGetDamageable(
                        candidate,
                        out IDamageable damageable) ||
                    _trajectoryTargets.Contains(damageable) ||
                    !TryResolvePenetrationHit(
                        p_request,
                        p_direction,
                        effectiveDistance,
                        candidate,
                        p_settings,
                        out Vector3 hitPoint,
                        out Vector3 hitNormal))
                {
                    continue;
                }

                _trajectoryTargets.Add(damageable);
                AddHit(
                    damageable,
                    candidate,
                    hitPoint,
                    hitNormal,
                    p_direction);
            }
        }

        private float ResolveEffectiveDistance(
            in RangeAttackRequest p_request,
            Vector3 p_direction,
            LayerMask p_hitMask,
            out bool p_hasCollision,
            out Vector3 p_collisionNormal)
        {
            Ray pathRay = new(p_request.Origin, p_direction);
            int hitCount = Physics.RaycastNonAlloc(
                pathRay,
                _pathHits,
                p_request.MaxDistance,
                p_hitMask,
                QueryTriggerInteraction.Ignore);

            float effectiveDistance = p_request.MaxDistance;
            p_hasCollision = false;
            p_collisionNormal = -p_direction;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _pathHits[index];

                if (hit.collider == null ||
                    IsAttackerCollider(hit.collider, p_request.Attacker) ||
                    DamageSystem.TryGetDamageable(hit.collider, out _) ||
                    hit.distance > effectiveDistance)
                {
                    continue;
                }

                effectiveDistance = hit.distance;
                p_hasCollision = true;
                p_collisionNormal = hit.normal;
            }

            return effectiveDistance;
        }

        private static bool TryResolvePenetrationHit(
            in RangeAttackRequest p_request,
            Vector3 p_direction,
            float p_effectiveDistance,
            Collider p_candidate,
            PenetrationAttackSettings p_settings,
            out Vector3 p_hitPoint,
            out Vector3 p_hitNormal)
        {
            Vector3 toCenter =
                p_candidate.bounds.center - p_request.Origin;
            Vector3 extents = p_candidate.bounds.extents;
            float projectedExtent =
                Mathf.Abs(p_direction.x) * extents.x +
                Mathf.Abs(p_direction.y) * extents.y +
                Mathf.Abs(p_direction.z) * extents.z;
            float centerDistance = Vector3.Dot(toCenter, p_direction);

            if (centerDistance + projectedExtent < 0f ||
                centerDistance - projectedExtent > p_effectiveDistance)
            {
                p_hitPoint = Vector3.zero;
                p_hitNormal = Vector3.zero;
                return false;
            }

            float pathDistance = Mathf.Clamp(
                centerDistance,
                0f,
                p_effectiveDistance);
            Vector3 pathPoint =
                p_request.Origin + p_direction * pathDistance;

            p_hitPoint = ColliderPointUtility.GetClosestPoint(
                p_candidate,
                pathPoint);

            float allowedRadius = EvaluateRadius(
                pathDistance,
                p_request.MaxDistance,
                p_settings);
            Vector3 normal = pathPoint - p_hitPoint;

            if (normal.sqrMagnitude > allowedRadius * allowedRadius)
            {
                p_hitNormal = Vector3.zero;
                return false;
            }

            p_hitNormal = normal.sqrMagnitude > 0.0001f
                ? normal.normalized
                : -p_direction;
            return true;
        }

        private static float EvaluateRadius(
            float p_distance,
            float p_maxDistance,
            PenetrationAttackSettings p_settings)
        {
            float distanceRatio = Mathf.Clamp01(
                p_distance / p_maxDistance);

            return Mathf.Lerp(
                p_settings.StartRadius,
                p_settings.EndRadius,
                distanceRatio);
        }

        private static bool IsAttackerCollider(
            Collider p_collider,
            Transform p_attacker)
        {
            return p_collider != null &&
                   p_attacker != null &&
                   (p_collider.transform == p_attacker ||
                    p_collider.transform.IsChildOf(p_attacker));
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
