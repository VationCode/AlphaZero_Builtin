using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Item.Weapon.Range;
using Alpha.Utility;
using UnityEngine;

namespace Alpha.Projectile
{
    // 발사 후 이동, 충돌, 피해 전달, 사거리·수명 종료를 관리한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public class Projectile : MonoBehaviour
    {
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

        private readonly Collider[] _impactBuffer =
            new Collider[ImpactBufferCapacity];

        private readonly HashSet<IDamageable> _damagedTargets = new();

        private Transform _attacker;
        private Vector3 _velocity;
        private Vector3 _gravity;

        private float _damage;
        private EHitReaction _hitReaction;
        private float _activeCollisionRadius;
        private float _remainingDistance;
        private float _remainingLifetime;

        private LayerMask _activeHitMask;

        private bool _isActive;

        public float GravityScale => _gravityScale;
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
            _velocity = p_request.Direction * p_launchSettings.Speed;
            _gravity = Physics.gravity * _gravityScale;
            _damage = p_request.Damage;
            _hitReaction = p_request.HitReaction;
            _activeCollisionRadius = CollisionRadius;
            _remainingDistance = p_request.MaxDistance;
            _remainingLifetime = p_launchSettings.Lifetime;
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
                $"MaxDistance={_remainingDistance}, Lifetime={_remainingLifetime}, " +
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

        private void Update()
        {
            if (!_isActive)
                return;

            float deltaTime = Time.deltaTime;
            _remainingLifetime -= deltaTime;

            if (_remainingLifetime <= 0f)
            {
                Release("Lifetime");
                return;
            }

            Vector3 displacement =
                _velocity * deltaTime +
                0.5f * _gravity *
                deltaTime * deltaTime;

            float requestedDistance = displacement.magnitude;

            float moveDistance = Mathf.Min(
                requestedDistance,
                _remainingDistance);

            if (moveDistance <= 0f ||
                requestedDistance <= 0.0001f)
            {
                Release("NoMovement");
                return;
            }

            Vector3 moveDirection =
                displacement / requestedDistance;

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

            _remainingDistance -= moveDistance;

            if (_remainingDistance <= 0f)
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

        private Vector3 GetCollisionCenter()
        {
            return _collisionShape != null
                ? _collisionShape.transform.TransformPoint(
                    _collisionShape.center)
                : transform.position;
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

            IDamageable damageable =
                p_hit.collider.GetComponentInParent<IDamageable>();

            LogLifecycle(
                $"Hit | Collider={p_hit.collider.name}, " +
                $"Layer={layerName}({p_hit.collider.gameObject.layer}), " +
                $"Damageable={damageable?.GetType().Name ?? "None"}, " +
                $"DamageApplied={damagedTargetCount > 0}, " +
                $"Point={p_hit.point}, RemainingDistance={_remainingDistance}");

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
                p_hitReaction: _hitReaction,
                p_deliveryType:
                    EDamageDeliveryType.Ranged);

            return DamageSystem.TryApply(
                p_hit.collider,
                damageInfo);
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
                IDamageable damageable = targetCollider != null
                    ? targetCollider.GetComponentInParent<IDamageable>()
                    : null;

                // 하나의 대상이 여러 Collider를 가져도 폭발당 피해는 한 번만 적용한다.
                if (damageable == null ||
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
                    p_hitReaction: _hitReaction,
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
                $"RemainingDistance={_remainingDistance}, " +
                $"RemainingLifetime={_remainingLifetime}");

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

            Debug.Log(
                $"[{nameof(Projectile)}:{name}] {p_message}",
                this);
        }
    }
}
