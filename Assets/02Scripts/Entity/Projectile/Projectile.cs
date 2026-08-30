using System;
using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Item.Weapon.Range;
using Alpha.Utility;
using UnityEngine;

namespace Alpha.Projectile
{
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

    // 발사 후 이동, 충돌, 피해 전달, 발사점 기준 사거리 종료를 관리한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public class Projectile : MonoBehaviour
    {
        public event Action<ProjectileImpactResult> OnImpacted;

        private const int ImpactBufferCapacity = 32;

        // 공격 대상의 Trigger 피격 영역도 항상 충돌 검색에 포함한다.
        private const QueryTriggerInteraction TargetTriggerInteraction =
            QueryTriggerInteraction.Collide;

        [Header("Flight")]
        [Tooltip("Physics Gravity에 곱할 중력 배율입니다. 0이면 직선으로 이동합니다.")]
        [SerializeField, Min(0f)]
        private float _gravityScale;

        [Header("Collision")]
        [Tooltip("비행 SphereCast의 중심과 반경으로 사용할 Collider입니다. 실제 물리 충돌에는 사용하지 않습니다.")]
        [SerializeField]
        private SphereCollider _collisionShape;

        [Header("Impact")]
        [SerializeField]
        private ProjectileImpactSettings _impactSettings = new(
            EProjectileImpactType.Direct,
            0f);

        [Header("Debug")]
        [Tooltip("발사, 충돌 대상, 피해 적용 결과와 종료 사유를 Console에 출력합니다.")]
        [SerializeField]
        private bool _logLifecycle;

        [Header("Scene Preview")]
        [SerializeField]
        private bool _showDamageRadius = true;

        [SerializeField]
        private Color _damageRadiusColor =
            new(1f, 0.25f, 0.1f, 0.2f);

        private readonly Collider[] _impactBuffer =
            new Collider[ImpactBufferCapacity];

        private readonly HashSet<IDamageable> _damagedTargets = new();

        private Transform _attacker;
        private Vector3 _launchOrigin;
        private Vector3 _velocity;
        private Vector3 _gravity;

        private float _damage;
        private AttackImpactInfo _attackImpact;
        private float _activeCollisionRadius;
        private float _maximumDistance;

        private LayerMask _activeHitMask;

        private bool _isActive;

        public float GravityScale => _gravityScale;
        public static QueryTriggerInteraction CollisionTriggerInteraction =>
            TargetTriggerInteraction;
        public float CollisionRadius =>
            CalculateCollisionRadius();
        public ProjectileImpactSettings ImpactSettings =>
            _impactSettings;
        public bool IsConfigurationValid =>
            _collisionShape != null &&
            CollisionRadius > 0f &&
            _impactSettings.IsValid;

        // 발사 주체가 소유한 공용 설정을 런타임 이동·충돌 상태로 복사한다.
        public bool Initialize(
            in RangeAttackRequest p_request,
            in ProjectileLaunchSettings p_launchSettings)
        {
            if (!p_request.IsValid ||
                !p_launchSettings.IsValid ||
                !IsConfigurationValid)
            {
                return false;
            }

            _attacker = p_request.Attacker;
            _launchOrigin = p_request.Origin;
            _velocity = p_request.Direction * p_launchSettings.Speed;
            _gravity = Physics.gravity * _gravityScale;
            _damage = p_request.Damage;
            _attackImpact = p_request.Impact;
            _activeCollisionRadius = CollisionRadius;
            _maximumDistance = p_request.MaxDistance;
            _activeHitMask = p_launchSettings.HitMask;

            // Collider는 SphereCast 형상 데이터로만 사용해 자기 자신과의 중복 물리 판정을 막는다.
            _collisionShape.enabled = false;

            transform.SetPositionAndRotation(
                p_request.Origin,
                Quaternion.LookRotation(
                    _velocity.normalized));

            _isActive = true;

            LogLifecycle(
                $"Launch | Attacker={_attacker.name}, " +
                $"Damage={_damage}, Speed={p_launchSettings.Speed}, " +
                $"MaxDistance={_maximumDistance}, " +
                $"GravityScale={_gravityScale}, HitMask={_activeHitMask.value}, " +
                $"Origin={transform.position}, Direction={p_request.Direction}");

            return true;
        }

        private void OnValidate()
        {
            _collisionShape ??= GetComponent<SphereCollider>();
            _gravityScale = Mathf.Max(0f, _gravityScale);
            _impactSettings.Validate();
        }

        // 설정을 소유한 Projectile에서 개발용 피해 반경을 직접 표시한다.
        private void OnDrawGizmosSelected()
        {
            if (!_showDamageRadius ||
                !_impactSettings.IsRadial ||
                _impactSettings.DamageRadius <= 0f)
            {
                return;
            }

            Color previousColor = Gizmos.color;
            float radius = _impactSettings.DamageRadius;

            Gizmos.color = _damageRadiusColor;
            Gizmos.DrawSphere(transform.position, radius);

            Color wireColor = _damageRadiusColor;
            wireColor.a = 1f;

            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = previousColor;
        }

        private void Update()
        {
            if (!_isActive)
                return;

            float deltaTime = Time.deltaTime;

            // Cinematic 정지 중에는 투사체 수명과 위치를 그대로 유지한다.
            if (deltaTime <= 0f)
                return;

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
                Release("MaximumDistance");
                return;
            }

            float moveDistance = Mathf.Min(
                requestedDistance,
                distanceToBoundary);
            bool reachesMaximumDistance =
                moveDistance >= distanceToBoundary - 0.0001f;

            Ray moveRay = new(
                GetCollisionCenter(),
                moveDirection);

            if (TryCastMovement(
                    moveRay,
                    moveDistance,
                    out RaycastHit hit))
            {
                HandleHit(
                    hit,
                    moveDirection);
                return;
            }

            transform.position +=
                moveDirection * moveDistance;

            if (reachesMaximumDistance)
            {
                Release("MaximumDistance");
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
            Ray p_moveRay,
            float p_moveDistance,
            out RaycastHit p_hit)
        {
            if (_activeCollisionRadius > 0f)
            {
                return Physics.SphereCast(
                    p_moveRay,
                    _activeCollisionRadius,
                    out p_hit,
                    p_moveDistance,
                    _activeHitMask,
                    TargetTriggerInteraction);
            }

            return Physics.Raycast(
                p_moveRay,
                out p_hit,
                p_moveDistance,
                _activeHitMask,
                TargetTriggerInteraction);
        }

        // 실제 비행과 조준 미리보기가 같은 등가속도 이동식을 사용한다.
        public static Vector3 CalculateDisplacement(
            Vector3 p_velocity,
            Vector3 p_gravity,
            float p_deltaTime)
        {
            return p_velocity * p_deltaTime +
                   0.5f * p_gravity *
                   p_deltaTime * p_deltaTime;
        }

        // 현재 이동 방향이 발사점 기준 MaxDistance 경계까지 갈 수 있는 거리를 계산한다.
        public static float CalculateDistanceToRangeBoundary(
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

        // 생성 전 예측에서도 Projectile Collider의 실제 중심을 사용할 수 있게 한다.
        public Vector3 CalculateCollisionCenter(
            Vector3 p_rootPosition,
            Quaternion p_rootRotation)
        {
            if (_collisionShape == null)
                return p_rootPosition;

            Vector3 rootLocalCenter =
                transform.InverseTransformPoint(
                    _collisionShape.transform.TransformPoint(
                        _collisionShape.center));

            Vector3 scaledCenter = Vector3.Scale(
                rootLocalCenter,
                transform.localScale);

            return p_rootPosition +
                   p_rootRotation * scaledCenter;
        }

        private Vector3 GetCollisionCenter()
        {
            return CalculateCollisionCenter(
                transform.position,
                transform.rotation);
        }

        private float CalculateCollisionRadius()
        {
            if (_collisionShape == null)
                return 0f;

            Vector3 scale = _collisionShape.transform.lossyScale;
            float maximumScale = Mathf.Max(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z));

            return Mathf.Max(0f, _collisionShape.radius) *
                   maximumScale;
        }

        private void HandleHit(
            RaycastHit p_hit,
            Vector3 p_direction)
        {
            transform.position = p_hit.point;

            int damagedTargetCount;

            if (_impactSettings.IsRadial)
            {
                damagedTargetCount = ApplyRadialDamage(
                    p_hit.point,
                    p_direction);
            }
            else
            {
                damagedTargetCount = ApplyDirectDamage(
                    p_hit,
                    p_direction)
                    ? 1
                    : 0;
            }

            string layerName = LayerMask.LayerToName(
                p_hit.collider.gameObject.layer);

            DamageSystem.TryGetDamageable(
                p_hit.collider,
                out IDamageable damageable);

            LogLifecycle(
                $"Hit | Collider={p_hit.collider.name}, " +
                $"Layer={layerName}({p_hit.collider.gameObject.layer}), " +
                $"Damageable={damageable?.GetType().Name ?? "None"}, " +
                $"DamageApplied={damagedTargetCount > 0}, " +
                $"Point={p_hit.point}, " +
                $"DistanceFromOrigin={Vector3.Distance(_launchOrigin, p_hit.point)}");

            OnImpacted?.Invoke(new ProjectileImpactResult(
                p_hit.point,
                p_hit.normal));

            // 지형이나 피해 불가능 대상에 명중해도 투사체는 종료한다.
            Release("Impact");
        }

        private bool ApplyDirectDamage(
            RaycastHit p_hit,
            Vector3 p_direction)
        {
            DamageInfo damageInfo = new(
                _attacker,
                _damage,
                p_hit.point,
                p_hit.normal,
                p_direction,
                p_impact: _attackImpact,
                p_deliveryType:
                    EDamageDeliveryType.Ranged);

            if (!DamageSystem.TryApply(
                    p_hit.collider,
                    damageInfo))
            {
                return false;
            }

            return true;
        }

        private int ApplyRadialDamage(
            Vector3 p_impactPoint,
            Vector3 p_fallbackDirection)
        {
            Physics.SyncTransforms();

            int hitCount = Physics.OverlapSphereNonAlloc(
                p_impactPoint,
                _impactSettings.DamageRadius,
                _impactBuffer,
                _activeHitMask,
                TargetTriggerInteraction);

            _damagedTargets.Clear();

            int damagedTargetCount = 0;

            for (int index = 0; index < hitCount; index++)
            {
                Collider targetCollider = _impactBuffer[index];
                // 하나의 대상이 여러 Collider를 가져도 폭발당 피해는 한 번만 적용한다.
                if (!DamageSystem.TryGetDamageable(
                        targetCollider,
                        out IDamageable damageable) ||
                    !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                Vector3 hitPoint =
                    ColliderPointUtility.GetClosestPoint(
                        targetCollider,
                        p_impactPoint);

                Vector3 direction = hitPoint - p_impactPoint;

                if (direction.sqrMagnitude <= 0.0001f)
                    direction = p_fallbackDirection;

                direction.Normalize();

                DamageInfo damageInfo = new(
                    _attacker,
                    _damage,
                    hitPoint,
                    -direction,
                    direction,
                    p_impact: _attackImpact,
                    p_deliveryType:
                        EDamageDeliveryType.Ranged);

                if (DamageSystem.TryApply(
                        targetCollider,
                        damageInfo))
                {
                    damagedTargetCount++;
                }
            }

            return damagedTargetCount;
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
