using UnityEngine;

namespace Alpha.Detection
{
    // 영역 형태에 맞는 Physics 검색을 실행하고 감지 결과를 호출자 버퍼에 수집한다.
    public static class DetectionAreaSystem
    {
        private const float ShapeEpsilon = 0.0001f;

        // 일반 Collider와 Trigger 피격 영역을 동일한 감지 대상으로 취급한다.
        private const QueryTriggerInteraction TargetTriggerInteraction =
            QueryTriggerInteraction.Collide;

        // 형태 검사를 통과한 결과를 Hit 버퍼에 채우고 실제 저장된 개수를 반환한다.
        public static int CollectHits(in DetectionAreaRequest p_request, Collider[] p_overlapBuffer, DetectionAreaHit[] p_hitBuffer)
        {
            if (!p_request.IsValid ||
                p_overlapBuffer == null ||
                p_overlapBuffer.Length == 0 ||
                p_hitBuffer == null ||
                p_hitBuffer.Length == 0)
            {
                return 0;
            }

            int overlapCount = Overlap(p_request, p_overlapBuffer);

            int hitCount = 0;

            for (int index = 0; index < overlapCount; index++)
            {
                Collider candidate = p_overlapBuffer[index];

                if (candidate == null || IsSelf(candidate.transform, p_request.Owner))
                {
                    continue;
                }

                Vector3 hitPoint =
                    GetClosestPoint(
                        candidate,
                        p_request.AreaOrigin);

                if (!PassesShapeFilter(p_request, hitPoint))
                    continue;

                Vector3 direction =
                    hitPoint - p_request.AreaOrigin;

                if (direction.sqrMagnitude <= ShapeEpsilon)
                    direction = p_request.Forward;

                p_hitBuffer[hitCount] =
                    new DetectionAreaHit(candidate, hitPoint, direction);

                hitCount++;

                if (hitCount >= p_hitBuffer.Length)
                    break;
            }

            if (hitCount < p_hitBuffer.Length)
                p_hitBuffer[hitCount] = default;

            return hitCount;
        }

        private static int Overlap(in DetectionAreaRequest p_request, Collider[] p_overlapBuffer)
        {
            DetectionAreaSettings settings = p_request.Settings;
            Vector3 areaOrigin = p_request.AreaOrigin;

            switch (settings.Shape)
            {
                case EDetectionAreaShape.ForwardBox:
                {
                    Vector3 center =
                            areaOrigin + p_request.Forward * (settings.Length * 0.5f);

                    Vector3 halfExtents =
                            new(settings.Width * 0.5f, settings.Height * 0.5f, settings.Length * 0.5f);

                    return Physics.OverlapBoxNonAlloc(
                        center,
                        halfExtents,
                        p_overlapBuffer,
                        p_request.Rotation,
                        settings.TargetLayers,
                        TargetTriggerInteraction);
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
                        settings.TargetLayers,
                        TargetTriggerInteraction);
                }

                default:
                    return 0;
            }
        }

        private static bool PassesShapeFilter(in DetectionAreaRequest p_request, Vector3 p_hitPoint)
        {
            DetectionAreaSettings settings = p_request.Settings;

            if (settings.Shape == EDetectionAreaShape.ForwardBox)
                return true;

            Vector3 relative =
                p_hitPoint - p_request.AreaOrigin;

            float verticalDistance = Vector3.Dot(relative, p_request.Up);

            if (Mathf.Abs(verticalDistance) > settings.Height * 0.5f + ShapeEpsilon)
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

            float minimumDot = Mathf.Cos(settings.Angle * 0.5f * Mathf.Deg2Rad);

            return Vector3.Dot(p_request.Forward, planarDirection.normalized) >= minimumDot;
        }

        private static bool IsSelf(Transform p_candidate, Transform p_owner)
        {
            if (p_candidate == null || p_owner == null)
                return false;

            return p_candidate == p_owner ||
                   p_candidate.IsChildOf(p_owner) ||
                   p_owner.IsChildOf(p_candidate);
        }

        // 지원하지 않는 Collider는 Bounds를 사용해 안전한 대표 지점을 구한다.
        private static Vector3 GetClosestPoint(
            Collider p_collider,
            Vector3 p_position)
        {
            if (p_collider == null)
                return p_position;

            if (SupportsClosestPoint(p_collider))
                return p_collider.ClosestPoint(p_position);

            return p_collider.bounds.ClosestPoint(p_position);
        }

        private static bool SupportsClosestPoint(Collider p_collider)
        {
            return p_collider is BoxCollider ||
                   p_collider is SphereCollider ||
                   p_collider is CapsuleCollider ||
                   p_collider is MeshCollider meshCollider &&
                   meshCollider.convex;
        }
    }
}
