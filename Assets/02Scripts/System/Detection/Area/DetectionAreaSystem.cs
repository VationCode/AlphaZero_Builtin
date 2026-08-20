using Alpha.Utility;
using UnityEngine;

namespace Alpha.Detection
{
    // 형태에 맞는 Physics 검색을 실행하고 Entity 계약과 무관한 Collider 결과를 반환한다.
    public static class DetectionAreaSystem
    {
        private const float ShapeEpsilon = 0.0001f;

        // 추가 계약이 필요 없는 호출자는 Collider 결과를 그대로 사용할 수 있다.
        public static int Query(
            in DetectionAreaRequest p_request,
            Collider[] p_colliderBuffer)
        {
            return CollectCandidates(
                p_request,
                p_colliderBuffer);
        }

        public static int Query(
            in DetectionAreaRequest p_request,
            Collider[] p_overlapBuffer,
            DetectionAreaHit[] p_hitBuffer)
        {
            if (p_hitBuffer == null || p_hitBuffer.Length == 0)
                return 0;

            int candidateCount = CollectCandidates(
                p_request,
                p_overlapBuffer);

            int hitCount = Mathf.Min(
                candidateCount,
                p_hitBuffer.Length);

            for (int index = 0; index < hitCount; index++)
            {
                Collider candidate = p_overlapBuffer[index];
                Vector3 hitPoint =
                    ColliderPointUtility.GetClosestPoint(
                        candidate,
                        p_request.AreaOrigin);
                Vector3 direction =
                    hitPoint - p_request.AreaOrigin;

                if (direction.sqrMagnitude <= ShapeEpsilon)
                    direction = p_request.Forward;

                p_hitBuffer[index] = new DetectionAreaHit(
                    candidate,
                    hitPoint,
                    direction);
            }

            if (hitCount < p_hitBuffer.Length)
                p_hitBuffer[hitCount] = default;

            return hitCount;
        }

        // 실제 형태를 통과한 Collider만 앞쪽부터 다시 채운다.
        private static int CollectCandidates(
            in DetectionAreaRequest p_request,
            Collider[] p_overlapBuffer)
        {
            if (!p_request.IsValid ||
                p_overlapBuffer == null ||
                p_overlapBuffer.Length == 0)
            {
                return 0;
            }

            int overlapCount = Overlap(
                p_request,
                p_overlapBuffer);

            int resultCount = 0;

            for (int index = 0; index < overlapCount; index++)
            {
                Collider candidate = p_overlapBuffer[index];

                if (candidate == null ||
                    IsSelf(candidate.transform, p_request.Owner))
                {
                    continue;
                }

                Vector3 hitPoint =
                    ColliderPointUtility.GetClosestPoint(
                        candidate,
                        p_request.AreaOrigin);

                if (!PassesShapeFilter(p_request, hitPoint))
                    continue;

                p_overlapBuffer[resultCount] = candidate;
                resultCount++;
            }

            if (resultCount < p_overlapBuffer.Length)
                p_overlapBuffer[resultCount] = null;

            return resultCount;
        }

        private static int Overlap(
            in DetectionAreaRequest p_request,
            Collider[] p_overlapBuffer)
        {
            DetectionAreaSettings settings = p_request.Settings;
            Vector3 areaOrigin = p_request.AreaOrigin;

            switch (settings.Shape)
            {
                case EDetectionAreaShape.ForwardBox:
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

                case EDetectionAreaShape.ForwardSector:
                case EDetectionAreaShape.Radial:
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

        private static bool PassesShapeFilter(
            in DetectionAreaRequest p_request,
            Vector3 p_hitPoint)
        {
            DetectionAreaSettings settings = p_request.Settings;

            if (settings.Shape == EDetectionAreaShape.ForwardBox)
                return true;

            Vector3 relative =
                p_hitPoint - p_request.AreaOrigin;

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

            float planarDistanceSqr =
                planarDirection.sqrMagnitude;

            if (planarDistanceSqr >
                settings.Radius * settings.Radius + ShapeEpsilon)
            {
                return false;
            }

            if (settings.Shape == EDetectionAreaShape.Radial ||
                settings.Angle >= 360f - ShapeEpsilon ||
                planarDistanceSqr <= ShapeEpsilon)
            {
                return true;
            }

            float minimumDot = Mathf.Cos(
                settings.Angle * 0.5f * Mathf.Deg2Rad);

            return Vector3.Dot(
                       p_request.Forward,
                       planarDirection.normalized) >= minimumDot;
        }

        private static bool IsSelf(
            Transform p_candidate,
            Transform p_owner)
        {
            if (p_candidate == null || p_owner == null)
                return false;

            return p_candidate == p_owner ||
                   p_candidate.IsChildOf(p_owner) ||
                   p_owner.IsChildOf(p_candidate);
        }
    }
}
