using UnityEngine;

namespace Alpha.Combat
{
    // 공격 형태에 맞는 Physics 검색을 실행하고 Damage 대상을 중복 없이 반환한다.
    public static class AttackAreaSystem
    {
        private const float ShapeEpsilon = 0.0001f;

        public static int Query(
            in AttackAreaRequest p_request,
            Collider[] p_overlapBuffer,
            AttackAreaHit[] p_hitBuffer)
        {
            if (!p_request.IsValid ||
                p_overlapBuffer == null ||
                p_overlapBuffer.Length == 0 ||
                p_hitBuffer == null ||
                p_hitBuffer.Length == 0)
            {
                return 0;
            }

            int overlapCount = CollectCandidates(
                p_request,
                p_overlapBuffer);

            int hitCount = 0;

            for (int index = 0;
                 index < overlapCount && hitCount < p_hitBuffer.Length;
                 index++)
            {
                Collider candidate = p_overlapBuffer[index];

                if (!TryCreateHit(
                        p_request,
                        candidate,
                        out AttackAreaHit hit) ||
                    ContainsDamageable(
                        p_hitBuffer,
                        hitCount,
                        hit.Damageable))
                {
                    continue;
                }

                p_hitBuffer[hitCount] = hit;
                hitCount++;
            }

            // 이전 검색 결과가 남지 않도록 사용하지 않은 첫 항목을 비운다.
            if (hitCount < p_hitBuffer.Length)
                p_hitBuffer[hitCount] = default;

            return hitCount;
        }

        private static int CollectCandidates(
            in AttackAreaRequest p_request,
            Collider[] p_overlapBuffer)
        {
            AttackAreaSettings settings = p_request.Settings;
            Vector3 areaOrigin = p_request.AreaOrigin;

            switch (settings.Shape)
            {
                case EAttackAreaShape.ForwardBox:
                {
                    Vector3 center = areaOrigin +
                                     p_request.Forward *
                                     (settings.Length * 0.5f);

                    Vector3 halfExtents = new(
                        settings.Width * 0.5f,
                        settings.Height * 0.5f,
                        settings.Length * 0.5f);

                    return Physics.OverlapBoxNonAlloc(
                        center,
                        halfExtents,
                        p_overlapBuffer,
                        p_request.Rotation,
                        settings.TargetMask,
                        settings.TriggerInteraction);
                }

                case EAttackAreaShape.ForwardSector:
                case EAttackAreaShape.Radial:
                {
                    // 원기둥을 감싸는 Box로 후보를 찾고 거리와 각도를 후처리한다.
                    Vector3 halfExtents = new(
                        settings.Radius,
                        settings.Height * 0.5f,
                        settings.Radius);

                    return Physics.OverlapBoxNonAlloc(
                        areaOrigin,
                        halfExtents,
                        p_overlapBuffer,
                        p_request.Rotation,
                        settings.TargetMask,
                        settings.TriggerInteraction);
                }

                default:
                    return 0;
            }
        }

        private static bool TryCreateHit(
            in AttackAreaRequest p_request,
            Collider p_candidate,
            out AttackAreaHit p_hit)
        {
            p_hit = default;

            if (p_candidate == null ||
                IsSelf(p_candidate.transform, p_request.Attacker))
            {
                return false;
            }

            IDamageable damageable =
                p_candidate.GetComponentInParent<IDamageable>();

            if (damageable == null)
                return false;

            Vector3 areaOrigin = p_request.AreaOrigin;
            Vector3 hitPoint = p_candidate.ClosestPoint(areaOrigin);

            if (!PassesShapeFilter(
                    p_request,
                    hitPoint))
            {
                return false;
            }

            Transform target = damageable is Component component
                ? component.transform
                : p_candidate.transform;

            Vector3 direction = hitPoint - areaOrigin;

            if (direction.sqrMagnitude <= ShapeEpsilon)
                direction = p_request.Forward;

            p_hit = new AttackAreaHit(
                p_candidate,
                damageable,
                target,
                hitPoint,
                direction);

            return true;
        }

        private static bool PassesShapeFilter(
            in AttackAreaRequest p_request,
            Vector3 p_hitPoint)
        {
            AttackAreaSettings settings = p_request.Settings;

            if (settings.Shape == EAttackAreaShape.ForwardBox)
                return true;

            Vector3 relative = p_hitPoint - p_request.AreaOrigin;
            float verticalDistance = Vector3.Dot(
                relative,
                p_request.Up);

            if (Mathf.Abs(verticalDistance) >
                settings.Height * 0.5f + ShapeEpsilon)
            {
                return false;
            }

            Vector3 planarDirection =
                relative - p_request.Up * verticalDistance;

            float planarDistanceSqr = planarDirection.sqrMagnitude;

            if (planarDistanceSqr >
                settings.Radius * settings.Radius + ShapeEpsilon)
            {
                return false;
            }

            if (settings.Shape == EAttackAreaShape.Radial ||
                settings.Angle >= 360f - ShapeEpsilon ||
                planarDistanceSqr <= ShapeEpsilon)
            {
                return true;
            }

            float minimumDot = Mathf.Cos(
                settings.Angle * 0.5f * Mathf.Deg2Rad);

            return Vector3.Dot(
                       p_request.Forward,
                       planarDirection.normalized) >=
                   minimumDot;
        }

        private static bool IsSelf(
            Transform p_candidate,
            Transform p_attacker)
        {
            if (p_candidate == null || p_attacker == null)
                return false;

            return p_candidate == p_attacker ||
                   p_candidate.IsChildOf(p_attacker) ||
                   p_attacker.IsChildOf(p_candidate);
        }

        private static bool ContainsDamageable(
            AttackAreaHit[] p_hitBuffer,
            int p_hitCount,
            IDamageable p_damageable)
        {
            for (int index = 0; index < p_hitCount; index++)
            {
                if (ReferenceEquals(
                        p_hitBuffer[index].Damageable,
                        p_damageable) ||
                    Equals(
                        p_hitBuffer[index].Damageable,
                        p_damageable))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
