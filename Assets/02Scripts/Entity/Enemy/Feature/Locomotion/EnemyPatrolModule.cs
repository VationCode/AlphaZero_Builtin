using UnityEngine;

namespace Alpha.Enemy
{
    // 순찰 영역·지점 생성·추적 경계를 독립적으로 관리한다.
    [DisallowMultipleComponent]
    public sealed class EnemyPatrolModule : MonoBehaviour
    {
        private const int GroundHitBufferCapacity = 16;
        private const int GizmoSegments = 48;

        [Tooltip("타깃이 없을 때 Patrol 지점을 생성하고 이동할지 여부입니다.")]
        [SerializeField]
        private bool _usePatrol = true;

        [Header("Patrol Area")]
        [SerializeField, Min(0f)]
        private float _areaRadius = 10f;

        [Tooltip("Patrol Area 외곽부터 추가로 추적할 수 있는 거리입니다.")]
        [SerializeField, Min(0f)]
        private float _maxChaseDistanceFromPatrolArea = 5f;

        [SerializeField, Min(0f)]
        private float _minimumPointDistance = 2f;

        [Header("Ground Probe")]
        [SerializeField]
        private LayerMask _groundMask = 1;

        [SerializeField, Min(0f)]
        private float _groundProbeHeight = 10f;

        [SerializeField, Min(0f)]
        private float _groundProbeDepth = 30f;

        [SerializeField, Min(1)]
        private int _sampleAttempts = 16;

        private readonly RaycastHit[] _groundHitBuffer =
            new RaycastHit[GroundHitBufferCapacity];

        private Transform _owner;
        private Vector3 _areaCenter;
        private Vector3 _pointA;
        private Vector3 _pointB;
        private bool _isMovingToA = true;

        public bool UsePatrol => _usePatrol;
        public Vector3 AreaCenter => _areaCenter;
        public float AreaRadius => _areaRadius;
        public float MaxChaseDistanceFromPatrolArea =>
            _maxChaseDistanceFromPatrolArea;
        public Vector3 PointA => _pointA;
        public Vector3 PointB => _pointB;
        public Vector3 CurrentPoint =>
            _isMovingToA ? _pointA : _pointB;
        public bool HasPatrolPoints { get; private set; }

        // Entity의 최초 위치를 순찰 중심으로 사용하고 두 지점을 준비한다.
        public void Bind(Transform p_owner)
        {
            _owner = p_owner != null
                ? p_owner
                : ResolvePreviewOwner();
            _areaCenter = _owner != null
                ? _owner.position
                : transform.position;
            _isMovingToA = true;
            HasPatrolPoints =
                _usePatrol && TryCreateInitialPoints();

            if (_usePatrol && !HasPatrolPoints)
            {
                Debug.LogWarning(
                    $"[{name}] 순찰 영역에서 Ground 지점을 생성하지 못했습니다.",
                    this);
            }
        }

        // 도착한 지점만 새 위치로 교체하고 다음 지점으로 진행한다.
        public void AdvancePoint()
        {
            if (!HasPatrolPoints)
                return;

            RefreshArrivedPoint();
            _isMovingToA = !_isMovingToA;
        }

        public bool IsOutsideChaseBoundary(Vector3 p_position)
        {
            float boundaryRadius =
                _areaRadius +
                _maxChaseDistanceFromPatrolArea;

            return HorizontalSqrDistance(
                       p_position,
                       _areaCenter) >
                   boundaryRadius * boundaryRadius;
        }

        public bool IsInsidePatrolArea(Vector3 p_position)
        {
            return HorizontalSqrDistance(
                       p_position,
                       _areaCenter) <=
                   _areaRadius * _areaRadius;
        }

        private void RefreshArrivedPoint()
        {
            if (_isMovingToA)
            {
                if (TryCreateRandomGroundPoint(
                        _pointB,
                        true,
                        out Vector3 nextPointA))
                {
                    _pointA = nextPointA;
                }

                return;
            }

            if (TryCreateRandomGroundPoint(
                    _pointA,
                    true,
                    out Vector3 nextPointB))
            {
                _pointB = nextPointB;
            }
        }

        private bool TryCreateInitialPoints()
        {
            if (!TryCreateRandomGroundPoint(
                    default,
                    false,
                    out _pointA))
            {
                return false;
            }

            return TryCreateRandomGroundPoint(
                _pointA,
                true,
                out _pointB);
        }

        private bool TryCreateRandomGroundPoint(
            Vector3 p_otherPoint,
            bool p_hasOtherPoint,
            out Vector3 p_point)
        {
            p_point = default;
            int attempts = Mathf.Max(1, _sampleAttempts);

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Vector2 randomOffset =
                    Random.insideUnitCircle *
                    Mathf.Max(0f, _areaRadius);

                Vector3 probeOrigin = _areaCenter +
                                      new Vector3(
                                          randomOffset.x,
                                          Mathf.Max(0f, _groundProbeHeight),
                                          randomOffset.y);

                if (!TryFindGround(probeOrigin, out Vector3 groundPoint))
                    continue;

                if (p_hasOtherPoint &&
                    HorizontalSqrDistance(groundPoint, p_otherPoint) <
                    _minimumPointDistance * _minimumPointDistance)
                {
                    continue;
                }

                p_point = groundPoint;
                return true;
            }

            return false;
        }

        private bool TryFindGround(
            Vector3 p_probeOrigin,
            out Vector3 p_groundPoint)
        {
            p_groundPoint = default;

            float probeDistance =
                Mathf.Max(0f, _groundProbeHeight) +
                Mathf.Max(0f, _groundProbeDepth);

            int hitCount = Physics.RaycastNonAlloc(
                p_probeOrigin,
                Vector3.down,
                _groundHitBuffer,
                probeDistance,
                _groundMask,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            bool foundGround = false;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _groundHitBuffer[index];

                if (hit.collider == null || IsOwnerCollider(hit.collider))
                    continue;

                if (hit.distance >= nearestDistance)
                    continue;

                nearestDistance = hit.distance;
                p_groundPoint = hit.point;
                foundGround = true;
            }

            return foundGround;
        }

        private bool IsOwnerCollider(Collider p_collider)
        {
            if (_owner == null || p_collider == null)
                return false;

            Transform candidate = p_collider.transform;

            return candidate == _owner ||
                   candidate.IsChildOf(_owner);
        }

        private static float HorizontalSqrDistance(
            Vector3 p_from,
            Vector3 p_to)
        {
            Vector3 offset = p_to - p_from;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        private void OnValidate()
        {
            _areaRadius = Mathf.Max(0f, _areaRadius);
            _maxChaseDistanceFromPatrolArea = Mathf.Max(
                0f,
                _maxChaseDistanceFromPatrolArea);
            _minimumPointDistance = Mathf.Clamp(
                _minimumPointDistance,
                0f,
                _areaRadius * 2f);
            _groundProbeHeight = Mathf.Max(0f, _groundProbeHeight);
            _groundProbeDepth = Mathf.Max(0f, _groundProbeDepth);
            _sampleAttempts = Mathf.Max(1, _sampleAttempts);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying && _owner != null
                ? _areaCenter
                : GetPreviewCenter();

            DrawHorizontalCircle(center, _areaRadius, Color.white);
            DrawHorizontalCircle(
                center,
                _areaRadius + _maxChaseDistanceFromPatrolArea,
                Color.yellow);

            if (!Application.isPlaying || !HasPatrolPoints)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_pointA, 0.15f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_pointB, 0.15f);

            if (_owner == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(_owner.position, CurrentPoint);
        }

        private Vector3 GetPreviewCenter()
        {
            Transform previewOwner = ResolvePreviewOwner();

            return previewOwner != null
                ? previewOwner.position
                : transform.position;
        }

        private Transform ResolvePreviewOwner()
        {
            Rigidbody ownerBody = GetComponentInParent<Rigidbody>();
            return ownerBody != null
                ? ownerBody.transform
                : transform;
        }

        private static void DrawHorizontalCircle(
            Vector3 p_center,
            float p_radius,
            Color p_color)
        {
            Gizmos.color = p_color;
            Vector3 previousPoint =
                p_center + Vector3.right * p_radius;

            for (int index = 1; index <= GizmoSegments; index++)
            {
                float angle = index / (float)GizmoSegments *
                              Mathf.PI * 2f;

                Vector3 nextPoint = p_center +
                                    new Vector3(
                                        Mathf.Cos(angle),
                                        0f,
                                        Mathf.Sin(angle)) * p_radius;

                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
    }
}
