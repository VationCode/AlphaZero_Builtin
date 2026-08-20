using Alpha.Utility;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossTargetRangeModule : MonoBehaviour
    {
        [SerializeField] private Transform _owner;

        [Header("Melee Attack")]
        [SerializeField, Min(0f)]
        private float _meleeAttackDistance = 5f;

        [SerializeField, Range(1f, 360f)]
        private float _meleeAttackAngle = 120f;

        [Header("Chase Allow")]
        [SerializeField, Min(0f)]
        private float _chaseAllowedDistance = 28f;

        [Header("Range Attack")]
        [SerializeField, Min(0f)]
        private float _rangeAttackDistance = 40f;

        [Header("Gizmo")]
        [SerializeField]
        private Color _rangeAttackColor = Color.cyan;

        [SerializeField]
        private Color _chaseAllowedRangeColor = Color.yellow;

        [SerializeField]
        private Color _meleeAttackRangeColor = Color.red;

        public float MeleeAttackDistance => _meleeAttackDistance;
        public float ChaseAllowedDistance => _chaseAllowedDistance;
        public float RangeAttackDistance => _rangeAttackDistance;

        private void Awake()
        {
            if (_owner == null)
            {
                CrabBossCore core = GetComponentInParent<CrabBossCore>();
                _owner = core != null ? core.transform : transform.parent;
            }
        }

        // 타겟 Collider 표면까지의 평면 방향과 거리를 한 번에 측정한다.
        public bool TryMeasure(
            Transform p_target,
            out Vector3 p_direction,
            out float p_distance)
        {
            p_direction = Vector3.zero;
            p_distance = float.PositiveInfinity;

            if (_owner == null || p_target == null)
                return false;

            Vector3 origin = _owner.position;
            Vector3 targetPoint = p_target.position;

            if (p_target.TryGetComponent(out Collider targetCollider))
            {
                targetPoint = ColliderPointUtility.GetClosestPoint(
                    targetCollider,
                    origin);
            }

            p_direction = targetPoint - origin;
            p_direction.y = 0f;
            p_distance = p_direction.magnitude;

            return true;
        }

        public bool IsWithinRangeAttackRange(float p_distance)
        {
            return p_distance <= _rangeAttackDistance;
        }

        public bool IsWithinChaseAllowedRange(float p_distance)
        {
            return p_distance <= _chaseAllowedDistance;
        }

        public bool IsWithinMeleeSector(
            Vector3 p_direction,
            float p_distance)
        {
            if (_owner == null || p_distance > _meleeAttackDistance)
                return false;

            if (p_distance <= 0.0001f)
                return true;

            Vector3 forward = _owner.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f ||
                p_direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            // 전체 각도의 절반을 기준으로 정면 좌우 범위를 확인한다.
            float minimumDot = Mathf.Cos(
                _meleeAttackAngle * 0.5f * Mathf.Deg2Rad);

            return Vector3.Dot(
                       forward.normalized,
                       p_direction.normalized) >= minimumDot;
        }

        private void OnDrawGizmosSelected()
        {
            Transform owner = ResolveOwner();

            if (owner == null)
                return;

            // 최초 전투 진입 거리와 추적 유지 거리를 서로 다른 색으로 표시한다.
            Gizmos.color = _rangeAttackColor;
            Gizmos.DrawWireSphere(owner.position, _rangeAttackDistance);

            Gizmos.color = _chaseAllowedRangeColor;
            Gizmos.DrawWireSphere(owner.position, _chaseAllowedDistance);

            Gizmos.color = _meleeAttackRangeColor;
            DrawMeleeAttackSector(owner);
        }

        private Transform ResolveOwner()
        {
            if (_owner != null)
                return _owner;

            CrabBossCore core = GetComponentInParent<CrabBossCore>();
            return core != null ? core.transform : transform.parent;
        }

        private void DrawMeleeAttackSector(Transform p_owner)
        {
            const int segmentCount = 24;

            Vector3 forward = p_owner.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f)
                return;

            forward.Normalize();

            float halfAngle = _meleeAttackAngle * 0.5f;
            float angleStep = _meleeAttackAngle / segmentCount;
            Vector3 origin = p_owner.position;
            Vector3 previousDirection =
                Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;

            Gizmos.DrawLine(
                origin,
                origin + previousDirection * _meleeAttackDistance);

            for (int index = 1; index <= segmentCount; index++)
            {
                float angle = -halfAngle + angleStep * index;
                Vector3 currentDirection =
                    Quaternion.AngleAxis(angle, Vector3.up) * forward;

                Gizmos.DrawLine(
                    origin + previousDirection * _meleeAttackDistance,
                    origin + currentDirection * _meleeAttackDistance);

                previousDirection = currentDirection;
            }

            Gizmos.DrawLine(
                origin,
                origin + previousDirection * _meleeAttackDistance);
        }
    }
}
