using System;
using UnityEngine;

namespace Alpha.Utility
{
    // 주어진 월드 위치의 아래쪽을 탐색하여 가장 가까운 유효 지면을 반환한다.
    [Serializable]
    public sealed class GroundProbe
    {
        private const int HitBufferCapacity = 16;

        [Tooltip("지면으로 판정할 레이어입니다.")]
        [SerializeField]
        private LayerMask _groundLayers = 1;

        [Tooltip("탐색 기준 위치에서 Raycast를 시작할 높이입니다.")]
        [SerializeField, Min(0f)]
        private float _castHeight = 10f;

        [Tooltip("탐색 기준 위치 아래로 확인할 깊이입니다.")]
        [SerializeField, Min(0f)]
        private float _castDepth = 30f;

        [NonSerialized]
        private RaycastHit[] _hitBuffer;

        // 기준 위치 위에서 아래로 탐색하며 제외 대상과 그 자식 Collider는 건너뛴다.
        public bool TryFindGround(
            Vector3 p_position,
            Transform p_ignoredRoot,
            out Vector3 p_groundPoint)
        {
            p_groundPoint = default;

            float castHeight = Mathf.Max(0f, _castHeight);
            float castDistance =
                castHeight + Mathf.Max(0f, _castDepth);

            if (castDistance <= 0f)
                return false;

            Vector3 castOrigin =
                p_position + Vector3.up * castHeight;

            int hitCount = UnityEngine.Physics.RaycastNonAlloc(
                castOrigin,
                Vector3.down,
                ResolveHitBuffer(),
                castDistance,
                _groundLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            bool foundGround = false;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _hitBuffer[index];

                if (hit.collider == null ||
                    IsIgnoredCollider(
                        hit.collider,
                        p_ignoredRoot) ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                p_groundPoint = hit.point;
                foundGround = true;
            }

            return foundGround;
        }

        // Inspector 값이 런타임 탐색 범위를 벗어나지 않도록 보정한다.
        public void Validate()
        {
            _castHeight = Mathf.Max(0f, _castHeight);
            _castDepth = Mathf.Max(0f, _castDepth);
        }

        private RaycastHit[] ResolveHitBuffer()
        {
            _hitBuffer ??= new RaycastHit[HitBufferCapacity];
            return _hitBuffer;
        }

        private static bool IsIgnoredCollider(
            Collider p_collider,
            Transform p_ignoredRoot)
        {
            if (p_collider == null || p_ignoredRoot == null)
                return false;

            Transform candidate = p_collider.transform;

            return candidate == p_ignoredRoot ||
                   candidate.IsChildOf(p_ignoredRoot);
        }
    }
}
