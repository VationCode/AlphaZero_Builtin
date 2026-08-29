using Alpha.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.AI
{
    // 지정 영역에서 Ground 순찰 지점을 생성하고 다음 목적지를 관리한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PatrolGizmoView))]
    public sealed class PatrolModule : MonoBehaviour
    {
        [Tooltip("타깃이 없을 때 순찰 지점을 생성하고 이동할지 여부입니다.")]
        [SerializeField]
        private bool _usePatrol = true;

        [Header("Patrol Area")]
        [FormerlySerializedAs("_areaRadius")]
        [SerializeField, Min(0f)]
        private float _radius = 10f;

        [Header("Point Sampling")]
        [Tooltip("두 순찰 지점 사이에 확보할 최소 수평 거리입니다.")]
        [SerializeField, Min(0f)]
        private float _minimumPointDistance = 2f;

        [Tooltip("조건에 맞는 순찰 지점을 찾기 위해 시도할 최대 횟수입니다.")]
        [FormerlySerializedAs("_sampleAttempts")]
        [SerializeField, Min(1)]
        private int _pointSampleAttempts = 16;

        [Header("Ground Probe")]
        [Tooltip("무작위 후보 위치를 실제 지면 위치로 보정합니다.")]
        [SerializeField]
        private GroundProbe _groundProbe = new();

        private Transform _owner;
        private Vector3 _center;
        private Vector3 _pointA;
        private Vector3 _pointB;
        private bool _isMovingToA = true;

        public bool UsePatrol => _usePatrol;
        public Transform Owner => _owner;
        public Vector3 Center => _center;
        public float Radius => _radius;
        public Vector3 PointA => _pointA;
        public Vector3 PointB => _pointB;
        public Vector3 CurrentPoint =>
            _isMovingToA ? _pointA : _pointB;
        public bool HasPatrolPoints { get; private set; }

        // 소유자의 최초 위치를 순찰 중심으로 사용하고 두 지점을 준비한다.
        public void Bind(Transform p_owner)
        {
            _owner = p_owner != null
                ? p_owner
                : ResolvePreviewOwner();

            _center = _owner != null
                ? _owner.position
                : transform.position;

            _isMovingToA = true;
            HasPatrolPoints =
                _usePatrol && TryCreateInitialPoints();

            if (_usePatrol && !HasPatrolPoints)
            {
                Debug.LogWarning(
                    $"[{name}] 순찰 영역에서 지면 위 순찰 지점을 생성하지 못했습니다.",
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

        public bool Contains(Vector3 p_position)
        {
            return HorizontalSqrDistance(
                       p_position,
                       _center) <=
                   _radius * _radius;
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
            int attempts = Mathf.Max(1, _pointSampleAttempts);

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Vector2 randomOffset =
                    Random.insideUnitCircle *
                    Mathf.Max(0f, _radius);

                Vector3 candidatePosition = _center +
                                            new Vector3(
                                                randomOffset.x,
                                                0f,
                                                randomOffset.y);

                // Patrol은 후보 위치만 만들고 실제 지면 판정은 GroundProbe에 맡긴다.
                if (_groundProbe == null ||
                    !_groundProbe.TryFindGround(
                        candidatePosition,
                        _owner,
                        out Vector3 groundPoint))
                {
                    continue;
                }

                if (p_hasOtherPoint &&
                    HorizontalSqrDistance(
                        groundPoint,
                        p_otherPoint) <
                    _minimumPointDistance *
                    _minimumPointDistance)
                {
                    continue;
                }

                p_point = groundPoint;
                return true;
            }

            return false;
        }

        private Transform ResolvePreviewOwner()
        {
            Rigidbody ownerBody =
                GetComponentInParent<Rigidbody>();

            return ownerBody != null
                ? ownerBody.transform
                : transform;
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
            _radius = Mathf.Max(0f, _radius);
            _minimumPointDistance = Mathf.Clamp(
                _minimumPointDistance,
                0f,
                _radius * 2f);
            _pointSampleAttempts =
                Mathf.Max(1, _pointSampleAttempts);

            _groundProbe ??= new GroundProbe();
            _groundProbe.Validate();
        }
    }
}
