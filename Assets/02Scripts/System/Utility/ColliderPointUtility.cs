using UnityEngine;

namespace Alpha.Utility
{
    // Collider 종류에 맞는 안전한 대표 지점을 반환한다.
    public static class ColliderPointUtility
    {
        public static Vector3 GetClosestPoint(
            Collider p_collider,
            Vector3 p_position)
        {
            if (p_collider == null)
                return p_position;

            if (SupportsClosestPoint(p_collider))
                return p_collider.ClosestPoint(p_position);

            // 비볼록 MeshCollider 등은 Unity ClosestPoint를 지원하지 않는다.
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
