using Alpha.Combat;
using UnityEngine;

namespace Alpha.Enemy
{
    // Rigidbody Ground 이동과 현재 위치 중심의 랜덤 A/B 순찰을 담당한다.
    [DisallowMultipleComponent]
    public sealed class EnemyLocomotionModule : MonoBehaviour
    {
        private const float DirectionEpsilon = 0.0001f;
        private const float RotationCompleteAngle = 0.1f;
        private const int GroundHitBufferCapacity = 16;
        private const int GizmoSegments = 48;

        [Header("Physics")]
        [SerializeField]
        private Rigidbody _rigidbody;

        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float _moveSpeed = 3f;

        [SerializeField, Min(0f)]
        private float _rotationSpeed = 360f;

        [SerializeField, Min(0f)]
        private float _arrivalDistance = 0.05f;

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
        private bool _hasPatrolPoints;
        private bool _isPatrolEnabled = true;
        private bool _hasLoggedMissingRigidbody;
        private bool _isKnockbackActive;
        private Vector3 _knockbackVelocity;
        private float _knockbackRemainingTime;

        public Vector3 AreaCenter => _areaCenter;
        public float AreaRadius => _areaRadius;
        public float MaxChaseDistanceFromPatrolArea =>
            _maxChaseDistanceFromPatrolArea;
        public Vector3 PointA => _pointA;
        public Vector3 PointB => _pointB;
        public bool HasPatrolPoints => _hasPatrolPoints;
        public bool IsPatrolEnabled => _isPatrolEnabled;
        public bool IsKnockbackActive => _isKnockbackActive;
        public bool CanApplyKnockback =>
            isActiveAndEnabled &&
            _rigidbody != null &&
            !_rigidbody.isKinematic;

        public void Bind(Transform p_owner)
        {
            _owner = p_owner;
            _rigidbody ??= p_owner != null
                ? p_owner.GetComponent<Rigidbody>()
                : GetComponentInParent<Rigidbody>();

            ValidateRigidbody();

            _areaCenter = p_owner != null
                ? p_owner.position
                : transform.position;
            _isMovingToA = true;
            _isPatrolEnabled = true;
            _hasPatrolPoints = TryCreateInitialPoints();

            if (!_hasPatrolPoints)
            {
                Debug.LogWarning(
                    $"[{name}] 순찰 영역에서 Ground 지점을 생성하지 못했습니다.",
                    this);
            }
        }

        private void FixedUpdate()
        {
            // 넉백 이동은 순찰·추적 이동보다 우선한다.
            if (TickKnockback(Time.fixedDeltaTime))
                return;

            if (!_isPatrolEnabled || !_hasPatrolPoints)
                return;

            Vector3 destination = _isMovingToA
                ? _pointA
                : _pointB;

            if (!MoveTo(destination, Time.fixedDeltaTime))
                return;

            RefreshArrivedPoint();
            _isMovingToA = !_isMovingToA;
        }

        // 현재 위치에서 목적지까지 수평 속도를 적용하고 진행 방향으로 회전한다.
        public bool MoveTo(
            Vector3 p_destination,
            float p_deltaTime)
        {
            return MoveTo(
                p_destination,
                p_deltaTime,
                _moveSpeed);
        }

        // Rush처럼 패턴이 이동 속도를 소유할 때 지정 속도로 이동한다.
        public bool MoveTo(
            Vector3 p_destination,
            float p_deltaTime,
            float p_moveSpeed)
        {
            if (_isKnockbackActive)
                return false;

            if (!TryGetRigidbody(out Rigidbody body))
                return false;

            Vector3 direction = Vector3.ProjectOnPlane(
                p_destination - body.position,
                Vector3.up);
            float arrivalDistance = Mathf.Max(0f, _arrivalDistance);

            if (direction.sqrMagnitude <=
                arrivalDistance * arrivalDistance)
            {
                StopHorizontalMovement(body);
                return true;
            }

            float deltaTime = Mathf.Max(0f, p_deltaTime);
            float distance = direction.magnitude;
            Vector3 moveDirection = direction / distance;

            RotateDirection(body, moveDirection, deltaTime);

            float moveSpeed = Mathf.Max(0f, p_moveSpeed);

            if (deltaTime > 0f)
                moveSpeed = Mathf.Min(moveSpeed, distance / deltaTime);

            Vector3 velocity = body.linearVelocity;
            velocity.x = moveDirection.x * moveSpeed;
            velocity.z = moveDirection.z * moveSpeed;
            body.linearVelocity = velocity;

            return false;
        }

        // 위치는 변경하지 않고 목적지 방향으로만 회전한다.
        public bool RotateTo(
            Vector3 p_destination,
            float p_deltaTime)
        {
            if (_isKnockbackActive)
                return false;

            if (!TryGetRigidbody(out Rigidbody body))
                return false;

            return RotateDirection(
                body,
                Vector3.ProjectOnPlane(
                    p_destination - body.position,
                    Vector3.up),
                p_deltaTime);
        }

        public void Stop()
        {
            // 일반 행동의 정지 요청이 진행 중인 넉백 속도를 지우지 않게 한다.
            if (_isKnockbackActive)
                return;

            if (TryGetRigidbody(out Rigidbody body))
                StopHorizontalMovement(body);
        }

        // 공격 방향과 거리/시간을 수평 Rigidbody 속도로 변환한다.
        public bool TryApplyKnockback(
            in KnockbackInfo p_knockbackInfo)
        {
            if (!p_knockbackInfo.IsValid ||
                !TryGetRigidbody(out Rigidbody body) ||
                body.isKinematic)
            {
                return false;
            }

            Vector3 direction = Vector3.ProjectOnPlane(
                p_knockbackInfo.Direction,
                Vector3.up);

            if (direction.sqrMagnitude <= DirectionEpsilon &&
                _owner != null)
            {
                direction = Vector3.ProjectOnPlane(
                    _owner.position -
                    p_knockbackInfo.Attacker.position,
                    Vector3.up);
            }

            if (direction.sqrMagnitude <= DirectionEpsilon)
                return false;

            _knockbackVelocity =
                direction.normalized *
                (p_knockbackInfo.Distance /
                 p_knockbackInfo.Duration);
            _knockbackRemainingTime =
                p_knockbackInfo.Duration;
            _isKnockbackActive = true;

            SetHorizontalVelocity(body, _knockbackVelocity);
            return true;
        }

        // 사망처럼 강제 종료가 필요한 경우 넉백과 수평 이동을 함께 정리한다.
        public void CancelKnockback()
        {
            _isKnockbackActive = false;
            _knockbackVelocity = Vector3.zero;
            _knockbackRemainingTime = 0f;

            if (TryGetRigidbody(out Rigidbody body))
                StopHorizontalMovement(body);
        }

        public void SetPatrolEnabled(bool p_enabled)
        {
            if (_isPatrolEnabled == p_enabled)
                return;

            _isPatrolEnabled = p_enabled;

            if (!p_enabled)
                Stop();
        }

        private bool TickKnockback(float p_deltaTime)
        {
            if (!_isKnockbackActive)
                return false;

            if (_knockbackRemainingTime <= 0f)
            {
                CancelKnockback();
                return false;
            }

            if (!TryGetRigidbody(out Rigidbody body) ||
                body.isKinematic)
            {
                _isKnockbackActive = false;
                _knockbackVelocity = Vector3.zero;
                _knockbackRemainingTime = 0f;
                return false;
            }

            float fixedDeltaTime = Mathf.Max(
                Mathf.Epsilon,
                p_deltaTime);
            float activeTime = Mathf.Min(
                _knockbackRemainingTime,
                fixedDeltaTime);

            // 마지막 물리 프레임은 남은 시간 비율만큼 속도를 줄여 설정 거리를 맞춘다.
            SetHorizontalVelocity(
                body,
                _knockbackVelocity *
                (activeTime / fixedDeltaTime));

            _knockbackRemainingTime = Mathf.Max(
                0f,
                _knockbackRemainingTime - activeTime);

            return true;
        }

        // 현재 위치가 Patrol Area와 추가 추적 허용 범위를 벗어났는지 반환한다.
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

        // ReturnToPatrol 상태가 순찰 영역 복귀 완료 여부를 판단할 때 사용한다.
        public bool IsInsidePatrolArea(Vector3 p_position)
        {
            return HorizontalSqrDistance(
                       p_position,
                       _areaCenter) <=
                   _areaRadius * _areaRadius;
        }

        private void RefreshArrivedPoint()
        {
            // 방금 도착한 지점만 다음 순환에서 사용할 새 위치로 교체한다.
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
                    Random.insideUnitCircle * Mathf.Max(0f, _areaRadius);

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

        private bool RotateDirection(
            Rigidbody p_body,
            Vector3 p_direction,
            float p_deltaTime)
        {
            if (p_direction.sqrMagnitude <= DirectionEpsilon)
                return true;

            Quaternion targetRotation = Quaternion.LookRotation(
                p_direction.normalized,
                Vector3.up);

            Quaternion nextRotation = Quaternion.RotateTowards(
                p_body.rotation,
                targetRotation,
                Mathf.Max(0f, _rotationSpeed) *
                Mathf.Max(0f, p_deltaTime));

            p_body.MoveRotation(nextRotation);

            return Quaternion.Angle(
                       nextRotation,
                       targetRotation) <=
                   RotationCompleteAngle;
        }

        private bool TryGetRigidbody(out Rigidbody p_body)
        {
            p_body = _rigidbody;

            if (p_body != null)
                return true;

            if (!_hasLoggedMissingRigidbody)
            {
                Debug.LogError(
                    $"[{name}] Enemy 루트에 Rigidbody가 필요합니다.",
                    this);
                _hasLoggedMissingRigidbody = true;
            }

            return false;
        }

        private static void StopHorizontalMovement(Rigidbody p_body)
        {
            Vector3 velocity = p_body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            p_body.linearVelocity = velocity;
        }

        private static void SetHorizontalVelocity(
            Rigidbody p_body,
            Vector3 p_horizontalVelocity)
        {
            Vector3 velocity = p_body.linearVelocity;
            velocity.x = p_horizontalVelocity.x;
            velocity.z = p_horizontalVelocity.z;
            p_body.linearVelocity = velocity;
        }

        private static float HorizontalSqrDistance(
            Vector3 p_from,
            Vector3 p_to)
        {
            Vector3 offset = p_to - p_from;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        private void ValidateRigidbody()
        {
            if (_rigidbody == null)
                return;

            if (!_rigidbody.useGravity || _rigidbody.isKinematic)
            {
                Debug.LogWarning(
                    $"[{name}] Ground 이동에는 Use Gravity가 켜진 " +
                    "Dynamic Rigidbody가 필요합니다.",
                    _rigidbody);
            }

            RigidbodyConstraints requiredConstraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            if ((_rigidbody.constraints & requiredConstraints) !=
                requiredConstraints)
            {
                Debug.LogWarning(
                    $"[{name}] 지면 충돌로 넘어지지 않도록 Rigidbody의 " +
                    "Rotation X/Z 고정을 권장합니다.",
                    _rigidbody);
            }
        }

        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0f, _moveSpeed);
            _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
            _arrivalDistance = Mathf.Max(0f, _arrivalDistance);
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

            if (_rigidbody == null && _owner != null)
                _rigidbody = _owner.GetComponent<Rigidbody>();
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

            if (!Application.isPlaying || !_hasPatrolPoints)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_pointA, 0.15f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_pointB, 0.15f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                _owner.position,
                _isMovingToA ? _pointA : _pointB);
        }

        private Vector3 GetPreviewCenter()
        {
            EnemyCore core = GetComponentInParent<EnemyCore>();
            return core != null
                ? core.transform.position
                : transform.position;
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
