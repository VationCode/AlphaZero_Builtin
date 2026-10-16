using System;
using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Item.Weapon.Range;
using UnityEngine;

namespace Alpha.Projectile
{
    public enum EProjectileCollisionMode
    {
        StopOnHit,
        PierceUntilDuration
    }

    // Projectile이 실제 충돌한 위치와 표면 방향을 자신의 View에 전달한다.
    public readonly struct ProjectileImpactResult
    {
        public Vector3 Point { get; }
        public Vector3 Normal { get; }

        public ProjectileImpactResult(
            Vector3 p_point,
            Vector3 p_normal)
        {
            Point = p_point;
            Normal = p_normal.sqrMagnitude > 0.0001f
                ? p_normal.normalized
                : Vector3.up;
        }
    }

    // 발사 후 이동, 충돌, 피해 전달, 발사점 기준 사거리 폭발을 관리한다.
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour
    {
        public event Action<ProjectileImpactResult> OnImpacted;

        private const int MaximumPredictionSteps = 2048;
        private const int PiercingHitBufferCapacity = 128;

        // 공격 대상의 Trigger 피격 영역도 항상 충돌 검색에 포함한다.
        private const QueryTriggerInteraction TargetTriggerInteraction =
            QueryTriggerInteraction.Collide;

        [Header("Movement")]
        [Tooltip("Prefab이 소유하는 초당 이동 속도입니다.")]
        [SerializeField, Min(0.01f)]
        private float _speed = 20f;

        [Tooltip("Physics.gravity에 적용할 배율입니다.")]
        [SerializeField, Min(0f)]
        private float _gravityScale;

        [Header("Collision")]
        [Tooltip("비행 중 충돌을 검색할 Layer입니다.")]
        [SerializeField]
        private LayerMask _hitMask = 65;

        [Tooltip("지정된 공격 대상 외에 충돌 시 투사체를 종료할 환경 Layer입니다.")]
        [SerializeField]
        private LayerMask _environmentHitMask;

        [Tooltip("비행 Cast 형상으로 사용할 Sphere, Box 또는 Capsule Collider입니다. 실제 물리 충돌에는 사용하지 않습니다.")]
        [SerializeField]
        private Collider _collisionShape;

        [Header("Impact")]
        [Tooltip("명중 시 활성화할 자식 피해 Collider입니다. 비어 있으면 직접 명중 피해를 적용합니다.")]
        [SerializeField]
        private ProjectileDamageArea _damageArea;

        [Tooltip("InitializeFromPrefab으로 발사할 때 사용할 피해 설정입니다. 무기 발사 요청의 피해는 기존 요청 값을 사용합니다.")]
        [SerializeField] private DamageProfile _prefabDamage = new();

        [Tooltip("충돌 시 종료하거나, 대상을 관통하며 Hit Duration 동안 비행합니다.")]
        [SerializeField] private EProjectileCollisionMode _collisionMode;

        [Tooltip("관통 모드에서 발사 후 유지할 시간입니다. 애니메이션 HitEnd와 무관합니다.")]
        [SerializeField, Min(0.01f)] private float _hitDuration = 1f;

        [Header("Debug")]
        [Tooltip("발사, 충돌 대상, 피해 적용 결과와 종료 사유를 Console에 출력합니다.")]
        [SerializeField]
        private bool _logLifecycle;

        private Transform _attacker;
        private Transform _requiredHitTarget;
        private bool _restrictStopHits;
        private Vector3 _launchOrigin;
        private Vector3 _velocity;
        private Vector3 _gravity;

        private float _damage;
        private AttackImpactInfo _attackImpact;
        private float _maximumDistance;
        private float _damageAreaRemainingTime;
        private float _hitTimeRemaining;

        private readonly RaycastHit[] _piercingHitBuffer =
            new RaycastHit[PiercingHitBufferCapacity];
        private readonly Collider[] _overlapHitBuffer =
            new Collider[PiercingHitBufferCapacity];
        private readonly HashSet<IDamageable> _piercedTargets = new();

        private bool _isActive;
        private bool _isWaitingForDamageArea;

        public float Speed => Mathf.Max(0.01f, _speed);
        public float GravityScale => Mathf.Max(0f, _gravityScale);
        public Vector3 Gravity => Physics.gravity * GravityScale;
        public LayerMask HitMask => _hitMask;
        public float CollisionPreviewRadius =>
            TryCalculateCollisionPreviewRadius(out float radius)
                ? radius
                : 0f;
        public float DamageAreaPreviewRadius =>
            _damageArea != null
                ? _damageArea.PreviewRadius
                : 0f;
        public bool HasDamageArea =>
            _damageArea != null &&
            _damageArea.IsConfigurationValid;
        public bool IsConfigurationValid =>
            Speed > 0f &&
            TryCalculateCollisionPreviewRadius(out _) &&
            (_collisionMode != EProjectileCollisionMode.PierceUntilDuration ||
             (_hitDuration > 0f && _damageArea == null)) &&
            (_damageArea == null || _damageArea.IsConfigurationValid);

        public bool IsPrefabDamageValid =>
            IsConfigurationValid && _prefabDamage != null && _prefabDamage.IsValid;

        // Effect 프리팹이 피해를 소유하는 공격에서 사용한다.
        public bool InitializeFromPrefab(Transform attacker, Transform target,
            Vector3 origin, Vector3 direction, float maxDistance)
        {
            if (!IsPrefabDamageValid || target == null)
                return false;

            RangeAttackRequest request = new(attacker, origin, origin, direction,
                _prefabDamage.Damage, maxDistance, _prefabDamage.Impact);
            if (!Initialize(request))
                return false;

            _requiredHitTarget = target;
            _restrictStopHits = true;
            return true;
        }

        // 종료 모드의 충돌점 또는 관통 모드의 지속 시간 종료 위치를 예측한다.
        public bool TryPredictImpact(
            Vector3 p_origin,
            Vector3 p_direction,
            float p_maximumDistance,
            float p_simulationStep,
            out ProjectileImpactResult p_result)
        {
            p_result = default;

            if (!IsConfigurationValid ||
                p_direction.sqrMagnitude <= 0.0001f ||
                p_maximumDistance <= 0f)
            {
                return false;
            }

            if (_collisionMode == EProjectileCollisionMode.PierceUntilDuration)
            {
                Vector3 initialVelocity = p_direction.normalized * Speed;
                Vector3 end = p_origin + CalculateDisplacement(initialVelocity,
                    Gravity, _hitDuration);
                Vector3 endVelocity = initialVelocity + Gravity * _hitDuration;
                p_result = new ProjectileImpactResult(end,
                    endVelocity.sqrMagnitude > 0.0001f
                        ? -endVelocity.normalized : -p_direction.normalized);
                return true;
            }

            Vector3 position = p_origin;
            Vector3 velocity =
                p_direction.normalized * Speed;
            Vector3 gravity = Gravity;
            Quaternion rotation =
                Quaternion.LookRotation(velocity.normalized);
            float simulationStep = Mathf.Max(
                0.005f,
                p_simulationStep);

            for (int stepIndex = 0;
                 stepIndex < MaximumPredictionSteps;
                 stepIndex++)
            {
                Vector3 displacement = CalculateDisplacement(
                    velocity,
                    gravity,
                    simulationStep);
                float requestedDistance = displacement.magnitude;

                if (requestedDistance <= 0.0001f)
                    return false;

                Vector3 moveDirection =
                    displacement / requestedDistance;
                float distanceToBoundary =
                    CalculateDistanceToRangeBoundary(
                        position,
                        p_origin,
                        moveDirection,
                        p_maximumDistance);

                if (distanceToBoundary <= 0f)
                {
                    p_result = new ProjectileImpactResult(
                        position,
                        -moveDirection);
                    return true;
                }

                float moveDistance = Mathf.Min(
                    requestedDistance,
                    distanceToBoundary);
                bool reachesMaximumDistance =
                    moveDistance >= distanceToBoundary - 0.0001f;

                if (TryCastMovementAtPose(
                        position,
                        rotation,
                        moveDirection,
                        moveDistance,
                        _hitMask,
                        out RaycastHit hit))
                {
                    p_result = new ProjectileImpactResult(
                        hit.point,
                        hit.normal);
                    return true;
                }

                position += moveDirection * moveDistance;

                if (reachesMaximumDistance)
                {
                    p_result = new ProjectileImpactResult(
                        position,
                        -moveDirection);
                    return true;
                }

                velocity += gravity * simulationStep;

                if (velocity.sqrMagnitude > 0.0001f)
                    rotation = Quaternion.LookRotation(velocity.normalized);
            }

            return false;
        }

        // 공격 요청과 Prefab 자체 설정으로 런타임 비행 상태를 시작한다.
        public bool Initialize(in RangeAttackRequest p_request)
        {
            if (!p_request.IsValid ||
                !IsConfigurationValid)
            {
                return false;
            }

            _attacker = p_request.Attacker;
            _requiredHitTarget = null;
            _restrictStopHits = false;
            _launchOrigin = p_request.Origin;
            _velocity = p_request.Direction * Speed;
            _gravity = Gravity;
            _damage = p_request.Damage;
            _attackImpact = p_request.Impact;
            _maximumDistance = p_request.MaxDistance;
            _damageAreaRemainingTime = 0f;
            _hitTimeRemaining = _hitDuration;
            _piercedTargets.Clear();
            _isWaitingForDamageArea = false;

            if (_damageArea != null)
                _damageArea.Deactivate();

            // Collider는 Cast 형상 데이터로만 사용해 자기 자신과의 중복 물리 판정을 막는다.
            _collisionShape.enabled = false;

            transform.SetPositionAndRotation(
                p_request.Origin,
                Quaternion.LookRotation(
                    _velocity.normalized));

            _isActive = true;

            LogLifecycle(
                $"Launch | Attacker={_attacker.name}, " +
                $"Damage={_damage}, Speed={Speed}, " +
                $"MaxDistance={_maximumDistance}, " +
                $"GravityScale={GravityScale}, " +
                $"HitMask={_hitMask.value}, " +
                $"Origin={transform.position}, Direction={p_request.Direction}");

            return true;
        }

        private void OnValidate()
        {
            _collisionShape ??= GetComponent<Collider>();
            _damageArea ??=
                GetComponentInChildren<ProjectileDamageArea>(true);
            _speed = Mathf.Max(0.01f, _speed);
            _gravityScale = Mathf.Max(0f, _gravityScale);
            _hitDuration = Mathf.Max(0.01f, _hitDuration);
            _prefabDamage ??= new DamageProfile();
            _prefabDamage.Validate();
        }

        private void Update()
        {
            if (_isWaitingForDamageArea)
            {
                TickDamageArea();
                return;
            }

            if (!_isActive)
                return;

            float deltaTime = Time.deltaTime;

            // Cinematic 정지 중에는 투사체 수명과 위치를 그대로 유지한다.
            if (deltaTime <= 0f)
                return;

            if (_collisionMode == EProjectileCollisionMode.PierceUntilDuration)
            {
                TickPiercing(deltaTime);
                return;
            }

            Vector3 displacement =
                CalculateDisplacement(
                    _velocity,
                    _gravity,
                    deltaTime);

            float requestedDistance = displacement.magnitude;

            if (requestedDistance <= 0.0001f)
            {
                Release("NoMovement");
                return;
            }

            Vector3 moveDirection =
                displacement / requestedDistance;

            float distanceToBoundary =
                CalculateDistanceToRangeBoundary(
                    transform.position,
                    _launchOrigin,
                    moveDirection,
                    _maximumDistance);

            if (distanceToBoundary <= 0f)
            {
                DetonateAtMaximumDistance(moveDirection);
                return;
            }

            float moveDistance = Mathf.Min(
                requestedDistance,
                distanceToBoundary);
            bool reachesMaximumDistance =
                moveDistance >= distanceToBoundary - 0.0001f;

            if (TryGetOverlappingStopHit(moveDirection,
                    out Collider overlappingCollider,
                    out Vector3 overlapPoint,
                    out Vector3 overlapNormal))
            {
                HandleHit(overlappingCollider, overlapPoint, overlapNormal,
                    moveDirection);
                return;
            }

            if (TryCastMovement(
                    moveDirection,
                    moveDistance,
                    out RaycastHit hit))
            {
                HandleHit(hit.collider, hit.point, hit.normal,
                    moveDirection);
                return;
            }

            transform.position +=
                moveDirection * moveDistance;

            if (reachesMaximumDistance)
            {
                DetonateAtMaximumDistance(moveDirection);
                return;
            }

            _velocity +=
                _gravity * deltaTime;

            if (_velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        _velocity.normalized);
            }
        }

        private bool TryCastMovement(
            Vector3 p_moveDirection,
            float p_moveDistance,
            out RaycastHit p_hit)
        {
            return TryCastMovementAtPose(
                transform.position,
                transform.rotation,
                p_moveDirection,
                p_moveDistance,
                _hitMask,
                out p_hit);
        }

        // SphereCast는 시작 위치에서 이미 겹친 대상을 보고하지 않아 별도로 검사한다.
        private bool TryGetOverlappingStopHit(Vector3 direction,
            out Collider hitCollider, out Vector3 hitPoint,
            out Vector3 hitNormal)
        {
            hitCollider = null;
            hitPoint = default;
            hitNormal = default;
            if (!_restrictStopHits || _collisionShape is not SphereCollider sphere ||
                !TryGetSphereWorldShapeAtPose(sphere, transform.position,
                    transform.rotation, out Vector3 center, out float radius))
                return false;

            int hitCount = Physics.OverlapSphereNonAlloc(center, radius,
                _overlapHitBuffer, _hitMask, TargetTriggerInteraction);
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                Collider candidate = _overlapHitBuffer[index];
                if (candidate == null || IsAttackerCollider(candidate) ||
                    !IsAllowedStopHit(candidate))
                    continue;

                Vector3 point = candidate.ClosestPoint(center);
                float distance = (point - center).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;

                hitCollider = candidate;
                hitPoint = point;
                nearestDistance = distance;
            }

            if (hitCollider == null)
                return false;

            hitNormal = center - hitPoint;
            hitNormal = hitNormal.sqrMagnitude > 0.0001f
                ? hitNormal.normalized : -direction;
            return true;
        }

        // 관통 모드는 이번 이동 경로의 모든 대상을 검사하고 같은 Entity에는 한 번만 피해를 준다.
        private void TickPiercing(float deltaTime)
        {
            float step = Mathf.Min(deltaTime, _hitTimeRemaining);
            if (step <= 0f)
            {
                Release("HitDurationCompleted");
                return;
            }

            Vector3 displacement = CalculateDisplacement(_velocity, _gravity, step);
            float distance = displacement.magnitude;
            if (distance > 0.0001f)
            {
                Vector3 direction = displacement / distance;
                int hitCount = CollectPiercingHits(direction, distance);
                for (int index = 0; index < hitCount; index++)
                {
                    RaycastHit hit = _piercingHitBuffer[index];
                    if (hit.collider == null || hit.collider.transform == _attacker ||
                        hit.collider.transform.IsChildOf(_attacker) ||
                        !DamageSystem.TryGetDamageable(hit.collider,
                            out IDamageable damageable) ||
                        !_piercedTargets.Add(damageable))
                        continue;

                    DamageInfo damageInfo = new(_attacker, _damage, hit.point,
                        hit.normal, direction, p_impact: _attackImpact,
                        p_deliveryType: EDamageDeliveryType.Ranged);
                    DamageSystem.TryApply(hit.collider, damageInfo);
                }

                transform.position += displacement;
            }

            _velocity += _gravity * step;
            if (_velocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_velocity.normalized);

            _hitTimeRemaining -= step;
            if (_hitTimeRemaining <= 0f)
                Release("HitDurationCompleted");
        }

        private int CollectPiercingHits(Vector3 direction, float distance)
        {
            switch (_collisionShape)
            {
                case SphereCollider sphere when TryGetSphereWorldShapeAtPose(
                    sphere, transform.position, transform.rotation,
                    out Vector3 center, out float radius):
                    return Physics.SphereCastNonAlloc(center, radius, direction,
                        _piercingHitBuffer, distance, _hitMask, TargetTriggerInteraction);

                case BoxCollider box when TryGetBoxWorldShapeAtPose(
                    box, transform.position, transform.rotation,
                    out Vector3 boxCenter, out Vector3 extents, out Quaternion orientation):
                    return Physics.BoxCastNonAlloc(boxCenter, extents, direction,
                        _piercingHitBuffer, orientation, distance, _hitMask,
                        TargetTriggerInteraction);

                case CapsuleCollider capsule when TryGetCapsuleWorldShapeAtPose(
                    capsule, transform.position, transform.rotation,
                    out Vector3 point1, out Vector3 point2, out float capsuleRadius):
                    return Physics.CapsuleCastNonAlloc(point1, point2, capsuleRadius,
                        direction, _piercingHitBuffer, distance, _hitMask,
                        TargetTriggerInteraction);

                default:
                    return 0;
            }
        }

        private bool TryCastMovementAtPose(
            Vector3 p_rootPosition,
            Quaternion p_rootRotation,
            Vector3 p_moveDirection,
            float p_moveDistance,
            LayerMask p_hitMask,
            out RaycastHit p_hit)
        {
            int hitCount;
            switch (_collisionShape)
            {
                case SphereCollider sphere:
                    if (TryGetSphereWorldShapeAtPose(
                            sphere,
                            p_rootPosition,
                            p_rootRotation,
                            out Vector3 sphereCenter,
                            out float sphereRadius))
                    {
                        hitCount = Physics.SphereCastNonAlloc(
                            sphereCenter,
                            sphereRadius,
                            p_moveDirection,
                            _piercingHitBuffer,
                            p_moveDistance,
                            p_hitMask,
                            TargetTriggerInteraction);
                        break;
                    }

                    p_hit = default;
                    return false;

                case BoxCollider box:
                    if (TryGetBoxWorldShapeAtPose(
                            box,
                            p_rootPosition,
                            p_rootRotation,
                            out Vector3 boxCenter,
                            out Vector3 halfExtents,
                            out Quaternion orientation))
                    {
                        hitCount = Physics.BoxCastNonAlloc(
                            boxCenter,
                            halfExtents,
                            p_moveDirection,
                            _piercingHitBuffer,
                            orientation,
                            p_moveDistance,
                            p_hitMask,
                            TargetTriggerInteraction);
                        break;
                    }

                    p_hit = default;
                    return false;

                case CapsuleCollider capsule:
                    if (TryGetCapsuleWorldShapeAtPose(
                            capsule,
                            p_rootPosition,
                            p_rootRotation,
                            out Vector3 point1,
                            out Vector3 point2,
                            out float capsuleRadius))
                    {
                        hitCount = Physics.CapsuleCastNonAlloc(
                            point1,
                            point2,
                            capsuleRadius,
                            p_moveDirection,
                            _piercingHitBuffer,
                            p_moveDistance,
                            p_hitMask,
                            TargetTriggerInteraction);
                        break;
                    }

                    p_hit = default;
                    return false;

                default:
                    p_hit = default;
                    return false;
            }

            p_hit = default;
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit candidate = _piercingHitBuffer[index];
                Collider collider = candidate.collider;
                if (collider == null || IsAttackerCollider(collider) ||
                    !IsAllowedStopHit(collider) ||
                    candidate.distance >= nearestDistance)
                    continue;

                p_hit = candidate;
                nearestDistance = candidate.distance;
            }

            return p_hit.collider != null;
        }

        private bool IsAttackerCollider(Collider collider)
        {
            return _attacker != null &&
                   (collider.transform == _attacker ||
                    collider.transform.IsChildOf(_attacker));
        }

        private bool IsAllowedStopHit(Collider collider)
        {
            if (!_restrictStopHits)
                return true;

            Transform hitTransform = collider.transform;
            return (_requiredHitTarget != null &&
                    (hitTransform == _requiredHitTarget ||
                     hitTransform.IsChildOf(_requiredHitTarget))) ||
                   (_environmentHitMask.value &
                    (1 << collider.gameObject.layer)) != 0;
        }

        private bool TryGetSphereWorldShapeAtPose(
            SphereCollider p_sphere,
            Vector3 p_rootPosition,
            Quaternion p_rootRotation,
            out Vector3 p_center,
            out float p_radius)
        {
            if (!TryGetSphereWorldShape(
                    p_sphere,
                    out Vector3 sourceCenter,
                    out p_radius))
            {
                p_center = default;
                return false;
            }

            p_center = TransformPointToRootPose(
                sourceCenter,
                p_rootPosition,
                p_rootRotation);
            return true;
        }

        private bool TryGetBoxWorldShapeAtPose(
            BoxCollider p_box,
            Vector3 p_rootPosition,
            Quaternion p_rootRotation,
            out Vector3 p_center,
            out Vector3 p_halfExtents,
            out Quaternion p_orientation)
        {
            if (!TryGetBoxWorldShape(
                    p_box,
                    out Vector3 sourceCenter,
                    out p_halfExtents,
                    out Quaternion sourceOrientation))
            {
                p_center = default;
                p_orientation = Quaternion.identity;
                return false;
            }

            p_center = TransformPointToRootPose(
                sourceCenter,
                p_rootPosition,
                p_rootRotation);
            p_orientation = p_rootRotation *
                            Quaternion.Inverse(transform.rotation) *
                            sourceOrientation;
            return true;
        }

        private bool TryGetCapsuleWorldShapeAtPose(
            CapsuleCollider p_capsule,
            Vector3 p_rootPosition,
            Quaternion p_rootRotation,
            out Vector3 p_point1,
            out Vector3 p_point2,
            out float p_radius)
        {
            if (!TryGetCapsuleWorldShape(
                    p_capsule,
                    out Vector3 sourcePoint1,
                    out Vector3 sourcePoint2,
                    out p_radius))
            {
                p_point1 = default;
                p_point2 = default;
                return false;
            }

            p_point1 = TransformPointToRootPose(
                sourcePoint1,
                p_rootPosition,
                p_rootRotation);
            p_point2 = TransformPointToRootPose(
                sourcePoint2,
                p_rootPosition,
                p_rootRotation);
            return true;
        }

        private Vector3 TransformPointToRootPose(
            Vector3 p_sourceWorldPoint,
            Vector3 p_rootPosition,
            Quaternion p_rootRotation)
        {
            Vector3 rootRelativePoint =
                Quaternion.Inverse(transform.rotation) *
                (p_sourceWorldPoint - transform.position);

            return p_rootPosition +
                   p_rootRotation * rootRelativePoint;
        }

        // 현재 속도와 중력으로 이번 Frame의 이동량을 계산한다.
        private static Vector3 CalculateDisplacement(
            Vector3 p_velocity,
            Vector3 p_gravity,
            float p_deltaTime)
        {
            return p_velocity * p_deltaTime +
                   0.5f * p_gravity *
                   p_deltaTime * p_deltaTime;
        }

        // 현재 이동 방향이 발사점 기준 MaxDistance 경계까지 갈 수 있는 거리를 계산한다.
        private static float CalculateDistanceToRangeBoundary(
            Vector3 p_position,
            Vector3 p_launchOrigin,
            Vector3 p_moveDirection,
            float p_maxDistance)
        {
            if (p_maxDistance <= 0f ||
                p_moveDirection.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            Vector3 direction = p_moveDirection.normalized;
            Vector3 originOffset = p_position - p_launchOrigin;
            float squaredRadius = p_maxDistance * p_maxDistance;
            float boundaryOffset =
                originOffset.sqrMagnitude - squaredRadius;

            if (boundaryOffset >= -0.0001f)
                return 0f;

            float projection = Vector3.Dot(
                originOffset,
                direction);
            float discriminant =
                projection * projection - boundaryOffset;

            if (discriminant <= 0f)
                return 0f;

            return Mathf.Max(
                0f,
                -projection + Mathf.Sqrt(discriminant));
        }

        // Scene Preview는 Collider 전체를 감싸는 보수적인 반경을 사용한다.
        private bool TryCalculateCollisionPreviewRadius(
            out float p_radius)
        {
            p_radius = 0f;

            switch (_collisionShape)
            {
                case SphereCollider sphere:
                    if (!TryGetSphereWorldShape(
                            sphere,
                            out Vector3 sphereCenter,
                            out float sphereRadius))
                    {
                        return false;
                    }

                    p_radius =
                        Vector3.Distance(transform.position, sphereCenter) +
                        sphereRadius;
                    return true;

                case BoxCollider box:
                    if (!TryGetBoxWorldShape(
                            box,
                            out Vector3 boxCenter,
                            out Vector3 halfExtents,
                            out _))
                    {
                        return false;
                    }

                    p_radius =
                        Vector3.Distance(transform.position, boxCenter) +
                        halfExtents.magnitude;
                    return true;

                case CapsuleCollider capsule:
                    if (!TryGetCapsuleWorldShape(
                            capsule,
                            out Vector3 point1,
                            out Vector3 point2,
                            out float capsuleRadius))
                    {
                        return false;
                    }

                    Vector3 capsuleCenter = (point1 + point2) * 0.5f;
                    float halfSegment =
                        Vector3.Distance(point1, point2) * 0.5f;

                    p_radius =
                        Vector3.Distance(transform.position, capsuleCenter) +
                        halfSegment +
                        capsuleRadius;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryGetSphereWorldShape(
            SphereCollider p_sphere,
            out Vector3 p_center,
            out float p_radius)
        {
            p_center = default;
            p_radius = 0f;

            if (p_sphere == null)
                return false;

            Transform shapeTransform = p_sphere.transform;
            Vector3 scale = Abs(shapeTransform.lossyScale);
            float maximumScale = Mathf.Max(
                scale.x,
                scale.y,
                scale.z);

            p_center = shapeTransform.TransformPoint(p_sphere.center);
            p_radius =
                Mathf.Max(0f, p_sphere.radius) * maximumScale;
            return p_radius > 0.0001f;
        }

        private static bool TryGetBoxWorldShape(
            BoxCollider p_box,
            out Vector3 p_center,
            out Vector3 p_halfExtents,
            out Quaternion p_orientation)
        {
            p_center = default;
            p_halfExtents = default;
            p_orientation = Quaternion.identity;

            if (p_box == null)
                return false;

            Transform shapeTransform = p_box.transform;
            Vector3 scale = Abs(shapeTransform.lossyScale);

            p_center = shapeTransform.TransformPoint(p_box.center);
            p_halfExtents = Vector3.Scale(
                p_box.size * 0.5f,
                scale);
            p_orientation = shapeTransform.rotation;

            return p_halfExtents.x > 0.0001f &&
                   p_halfExtents.y > 0.0001f &&
                   p_halfExtents.z > 0.0001f;
        }

        private static bool TryGetCapsuleWorldShape(
            CapsuleCollider p_capsule,
            out Vector3 p_point1,
            out Vector3 p_point2,
            out float p_radius)
        {
            p_point1 = default;
            p_point2 = default;
            p_radius = 0f;

            if (p_capsule == null)
                return false;

            Transform shapeTransform = p_capsule.transform;
            Vector3 scale = Abs(shapeTransform.lossyScale);
            Vector3 localAxis;
            float heightScale;
            float radiusScale;

            switch (p_capsule.direction)
            {
                case 0:
                    localAxis = Vector3.right;
                    heightScale = scale.x;
                    radiusScale = Mathf.Max(scale.y, scale.z);
                    break;

                case 1:
                    localAxis = Vector3.up;
                    heightScale = scale.y;
                    radiusScale = Mathf.Max(scale.x, scale.z);
                    break;

                case 2:
                    localAxis = Vector3.forward;
                    heightScale = scale.z;
                    radiusScale = Mathf.Max(scale.x, scale.y);
                    break;

                default:
                    return false;
            }

            p_radius =
                Mathf.Max(0f, p_capsule.radius) * radiusScale;

            if (p_radius <= 0.0001f)
                return false;

            float height = Mathf.Max(
                Mathf.Max(0f, p_capsule.height) * heightScale,
                p_radius * 2f);
            float halfSegment =
                Mathf.Max(0f, height * 0.5f - p_radius);
            Vector3 center =
                shapeTransform.TransformPoint(p_capsule.center);
            Vector3 axis =
                shapeTransform.TransformDirection(localAxis).normalized;

            p_point1 = center + axis * halfSegment;
            p_point2 = center - axis * halfSegment;
            return true;
        }

        private static Vector3 Abs(Vector3 p_value)
        {
            return new Vector3(
                Mathf.Abs(p_value.x),
                Mathf.Abs(p_value.y),
                Mathf.Abs(p_value.z));
        }

        private void HandleHit(Collider collider, Vector3 point,
            Vector3 normal, Vector3 direction)
        {
            transform.position = point;

            bool didApplyDirectDamage =
                _damageArea == null &&
                ApplyDirectDamage(collider, point, normal, direction);

            string layerName = LayerMask.LayerToName(
                collider.gameObject.layer);

            DamageSystem.TryGetDamageable(
                collider,
                out IDamageable damageable);

            LogLifecycle(
                $"Hit | Collider={collider.name}, " +
                $"Layer={layerName}({collider.gameObject.layer}), " +
                $"Damageable={damageable?.GetType().Name ?? "None"}, " +
                $"DirectDamageApplied={didApplyDirectDamage}, " +
                $"DamageArea={_damageArea != null}, " +
                $"Point={point}, " +
                $"DistanceFromOrigin={Vector3.Distance(_launchOrigin, point)}");

            // 지형이나 피해 불가능 대상에 명중해도 투사체는 종료한다.
            CompleteImpact(point, normal, "Impact");
        }

        // 충돌 대상이 없는 최대 사거리에서도 자식 피해 Collider와 폭발 표현을 실행한다.
        private void DetonateAtMaximumDistance(
            Vector3 p_fallbackDirection)
        {
            Vector3 direction =
                p_fallbackDirection.sqrMagnitude > 0.0001f
                    ? p_fallbackDirection.normalized
                    : transform.forward;

            LogLifecycle(
                $"Detonate | Reason=MaximumDistance, " +
                $"DamageArea={_damageArea != null}, " +
                $"Point={transform.position}, " +
                $"DistanceFromOrigin={Vector3.Distance(_launchOrigin, transform.position)}");

            CompleteImpact(
                transform.position,
                -direction,
                "MaximumDistance");
        }

        private void CompleteImpact(
            Vector3 p_point,
            Vector3 p_normal,
            string p_releaseReason)
        {
            OnImpacted?.Invoke(new ProjectileImpactResult(
                p_point,
                p_normal));

            if (_damageArea != null &&
                _damageArea.Activate(
                    _attacker,
                    _damage,
                    _attackImpact))
            {
                _isActive = false;
                _isWaitingForDamageArea = true;
                _damageAreaRemainingTime =
                    _damageArea.ActiveDuration;
                return;
            }

            Release(p_releaseReason);
        }

        private bool ApplyDirectDamage(Collider collider, Vector3 point,
            Vector3 normal, Vector3 direction)
        {
            DamageInfo damageInfo = new(
                _attacker,
                _damage,
                point,
                normal,
                direction,
                p_impact: _attackImpact,
                p_deliveryType:
                    EDamageDeliveryType.Ranged);

            if (!DamageSystem.TryApply(
                    collider,
                    damageInfo))
            {
                return false;
            }

            return true;
        }

        private void TickDamageArea()
        {
            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
                return;

            _damageAreaRemainingTime -= deltaTime;

            if (_damageAreaRemainingTime > 0f)
                return;

            _damageArea?.Deactivate();
            Release("DamageAreaCompleted");
        }

        // 이후 ObjectPool을 적용할 때 이 메서드만 반환 처리로 교체한다.
        private void Release(string p_reason)
        {
            LogLifecycle(
                $"Release | Reason={p_reason}, " +
                $"Position={transform.position}, " +
                $"DistanceFromOrigin={Vector3.Distance(_launchOrigin, transform.position)}, " +
                $"MaxDistance={_maximumDistance}");

            _isActive = false;
            _isWaitingForDamageArea = false;
            _damageAreaRemainingTime = 0f;
            _hitTimeRemaining = 0f;
            _piercedTargets.Clear();
            _damageArea?.Deactivate();
            Destroy(gameObject);
        }

        private void LogLifecycle(string p_message)
        {
            // Prefab 편집 중에도 Sniper 발사체 진단은 항상 유지한다.
            bool isSniperProjectile =
                name.StartsWith("BulletProjectile");

            if (!_logLifecycle &&
                !isSniperProjectile)
            {
                return;
            }

            //Debug.Log($"[{nameof(Projectile)}:{name}] {p_message}", this);
        }
    }
}
