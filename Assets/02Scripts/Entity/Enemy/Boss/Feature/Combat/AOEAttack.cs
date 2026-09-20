using System.Collections.Generic;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Boss
{
    // 정지형 범위 피해 또는 이동 경로의 범위 피해를 적용한다. 각 대상은 공격 하나당 한 번 맞는다.
    [DisallowMultipleComponent]
    public sealed class AOEAttack : MonoBehaviour
    {
        [SerializeField, Tooltip("자신 또는 자식의 피해용 Trigger입니다. Sphere·Box·CapsuleCollider를 지원하며 크기와 중심은 Collider에서 설정합니다.")]
        private Collider _damageCollider;
        [SerializeField] private LayerMask _damageMask = 64;

        private readonly HashSet<IDamageable> _damagedTargets = new();
        private Transform _attacker;
        private float _damage;
        private AttackImpactInfo _impact;
        private float _remainingLifetime;
        private bool _isActive;
        private bool _damagePending;
        private bool _isMoving;
        private Vector3 _startPosition;
        private Vector3 _direction;
        private float _moveSpeed;
        private float _maximumDistance;
        private float _travelled;

        public Collider DamageCollider => _damageCollider;
        public bool IsConfigurationValid => enabled && _damageMask.value != 0 &&
            TryGetDamageShape(out _);

        private bool Initialize(Transform p_attacker, float p_damage, in AttackImpactInfo p_impact)
        {
            if (!IsConfigurationValid || p_attacker == null || !IsFinite(p_damage) || p_damage <= 0f)
                return false;
            _attacker = p_attacker;
            _damage = p_damage;
            _impact = p_impact;
            _remainingLifetime = 0f;
            _damagedTargets.Clear();
            _isActive = true;
            // 한 회차의 모든 생성과 초기화가 끝난 뒤 피해 이벤트를 발생시킨다.
            _damagePending = true;
            _isMoving = false;
            return true;
        }

        public bool InitializeArea(Transform p_attacker, float p_damage, in AttackImpactInfo p_impact,
            float p_lifetime)
        {
            if (!IsFinite(p_lifetime) || p_lifetime <= 0f || !Initialize(p_attacker, p_damage, p_impact))
                return false;
            _remainingLifetime = p_lifetime;
            return true;
        }

        public bool InitializeMoving(Transform p_attacker, float p_damage, in AttackImpactInfo p_impact,
            Vector3 p_direction, float p_speed, float p_maximumDistance)
        {
            if (!IsFinite(p_direction.x) || !IsFinite(p_direction.y) || !IsFinite(p_direction.z) ||
                !IsFinite(p_direction.sqrMagnitude) || p_direction.sqrMagnitude < 0.0001f ||
                !IsFinite(p_speed) || p_speed <= 0f || !IsFinite(p_maximumDistance) ||
                p_maximumDistance <= 0f || !Initialize(p_attacker, p_damage, p_impact))
                return false;
            _isMoving = true;
            _startPosition = transform.position;
            _direction = p_direction.normalized;
            _moveSpeed = p_speed;
            _maximumDistance = p_maximumDistance;
            _travelled = 0f;
            return true;
        }

        private void Update() => Tick(Time.deltaTime);

        private void Tick(float p_deltaTime)
        {
            if (!_isActive || !IsFinite(p_deltaTime) || p_deltaTime <= 0f)
                return;
            if (_attacker == null)
            {
                Finish();
                return;
            }
            if (_isMoving)
            {
                Vector3 previous = transform.position;
                _travelled = Mathf.Min(_maximumDistance, _travelled + _moveSpeed * p_deltaTime);
                transform.position = _startPosition + _direction * _travelled;
                // 프레임 사이의 경로 전체를 검사하여 빠른 공격도 타겟을 통과하지 않게 한다.
                ApplyDamage(previous, transform.position);
                if (_travelled >= _maximumDistance)
                    Finish();
                return;
            }
            if (_damagePending)
            {
                _damagePending = false;
                ApplyDamage(transform.position, transform.position);
            }
            _remainingLifetime -= p_deltaTime;
            if (_remainingLifetime <= 0f)
                Finish();
        }

        private void ApplyDamage(Vector3 p_previous, Vector3 p_current)
        {
            if (!TryGetDamageShape(out DamageShape shape))
            {
                Finish();
                return;
            }
            Vector3 movement = p_current - p_previous;
            float distance = movement.magnitude;
            // Cast는 시작할 때 이미 겹친 대상을 놓칠 수 있으므로 시작·끝의 겹침도 함께 검사한다.
            if (distance > 0.000001f)
            {
                foreach (Collider hit in shape.Overlap(-movement, _damageMask))
                    ApplyHit(hit, shape.Center - movement);
                foreach (RaycastHit hit in shape.Cast(-movement, movement / distance, distance, _damageMask))
                    ApplyHit(hit.collider, shape.Center);
            }
            foreach (Collider hit in shape.Overlap(Vector3.zero, _damageMask))
                ApplyHit(hit, shape.Center);
        }

        private void ApplyHit(Collider p_collider, Vector3 p_origin)
        {
            if (!_isActive || _attacker == null || p_collider == null ||
                p_collider.transform.IsChildOf(transform) ||
                !DamageSystem.TryGetDamageable(p_collider, out IDamageable target) || !_damagedTargets.Add(target))
                return;
            Vector3 direction = _isMoving ? _direction : p_collider.bounds.center - p_origin;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.up;
            DamageInfo damage = new(_attacker, _damage, p_collider.ClosestPoint(p_origin), -direction.normalized,
                direction, _impact, EDamageDeliveryType.Ranged);
            if (!DamageSystem.TryApply(p_collider, damage))
                _damagedTargets.Remove(target);
        }

        private bool TryGetDamageShape(out DamageShape p_shape)
        {
            p_shape = default;
            if (_damageCollider == null || !_damageCollider.enabled || !_damageCollider.isTrigger ||
                !_damageCollider.transform.IsChildOf(transform))
                return false;
            // Prefab 루트는 비활성 상태로도 초기화한다. 비활성 자식 판정 영역은 사용하지 않는다.
            for (Transform node = _damageCollider.transform; node != transform; node = node.parent)
                if (!node.gameObject.activeSelf)
                    return false;
            return DamageShape.TryCreate(_damageCollider, out p_shape);
        }

        // Collider의 실제 기본 도형을 월드 좌표로 변환한다. 외접 Bounds를 피해 모양으로 사용하지 않는다.
        private struct DamageShape
        {
            public Vector3 Center;
            private Vector3 _halfExtents;
            private Quaternion _rotation;
            private Vector3 _capsuleOffset;
            private float _radius;
            private int _kind; // 0: Sphere, 1: Box, 2: Capsule

            public static bool TryCreate(Collider p_collider, out DamageShape p_shape)
            {
                p_shape = default;
                Transform frame = p_collider.transform;
                Vector3 scale = frame.lossyScale;
                scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                if (!IsFinite(scale) || scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                    return false;
                switch (p_collider)
                {
                    case SphereCollider sphere:
                        p_shape.Center = frame.TransformPoint(sphere.center);
                        p_shape._radius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                        return IsFinite(p_shape.Center) && IsFinite(p_shape._radius) && p_shape._radius > 0f;
                    case BoxCollider box:
                        p_shape._kind = 1;
                        p_shape.Center = frame.TransformPoint(box.center);
                        p_shape._rotation = frame.rotation;
                        p_shape._halfExtents = Vector3.Scale(box.size, scale) * 0.5f;
                        return IsFinite(p_shape.Center) && IsFinite(p_shape._halfExtents) &&
                            p_shape._halfExtents.x > 0f && p_shape._halfExtents.y > 0f && p_shape._halfExtents.z > 0f;
                    case CapsuleCollider capsule:
                        p_shape._kind = 2;
                        p_shape.Center = frame.TransformPoint(capsule.center);
                        int axis = capsule.direction;
                        if (axis < 0 || axis > 2)
                            return false;
                        float radiusScale = Mathf.Max(scale[(axis + 1) % 3], scale[(axis + 2) % 3]);
                        p_shape._radius = capsule.radius * radiusScale;
                        float height = capsule.height * scale[axis];
                        if (!IsFinite(height) || height <= 0f)
                            return false;
                        Vector3 localAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                        p_shape._capsuleOffset = frame.rotation * localAxis *
                            Mathf.Max(0f, height * 0.5f - p_shape._radius);
                        return IsFinite(p_shape.Center) && IsFinite(p_shape._capsuleOffset) &&
                            IsFinite(p_shape._radius) && p_shape._radius > 0f;
                    default:
                        return false;
                }
            }

            public Collider[] Overlap(Vector3 p_offset, int p_mask)
            {
                Vector3 center = Center + p_offset;
                return _kind switch
                {
                    1 => Physics.OverlapBox(center, _halfExtents, _rotation, p_mask, QueryTriggerInteraction.Collide),
                    2 => Physics.OverlapCapsule(center - _capsuleOffset, center + _capsuleOffset,
                        _radius, p_mask, QueryTriggerInteraction.Collide),
                    _ => Physics.OverlapSphere(center, _radius, p_mask, QueryTriggerInteraction.Collide)
                };
            }

            public RaycastHit[] Cast(Vector3 p_offset, Vector3 p_direction, float p_distance, int p_mask)
            {
                Vector3 center = Center + p_offset;
                return _kind switch
                {
                    1 => Physics.BoxCastAll(center, _halfExtents, p_direction, _rotation,
                        p_distance, p_mask, QueryTriggerInteraction.Collide),
                    2 => Physics.CapsuleCastAll(center - _capsuleOffset, center + _capsuleOffset,
                        _radius, p_direction, p_distance, p_mask, QueryTriggerInteraction.Collide),
                    _ => Physics.SphereCastAll(center, _radius, p_direction,
                        p_distance, p_mask, QueryTriggerInteraction.Collide)
                };
            }
        }

        private void Finish()
        {
            _isActive = false;
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            _isActive = false;
            _damagePending = false;
        }

        private void Reset() => _damageCollider = GetComponent<Collider>();

        private void OnValidate()
        {
            if (_damageCollider == null)
                _damageCollider = GetComponent<Collider>();
        }

        private static bool IsFinite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
        private static bool IsFinite(Vector3 p_value) => IsFinite(p_value.x) && IsFinite(p_value.y) && IsFinite(p_value.z);
    }
}
