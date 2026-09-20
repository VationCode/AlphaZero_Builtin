using System;
using System.Collections.Generic;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.GroundWave
{
    // 발사 후 지면을 따라 폭발 지점을 전개하고 파동당 같은 대상에게 한 번만 피해를 준다.
    [DisallowMultipleComponent]
    public sealed class GroundWave : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _speed = 15f;
        [SerializeField, Min(0.1f)] private float _eruptionSpacing = 4f;
        [SerializeField, Min(0.01f)] private float _damageRadius = 2f;
        [SerializeField] private LayerMask _damageMask = 64;
        [SerializeField] private LayerMask _groundMask = 1;
        [SerializeField, Min(0f)] private float _probeHeight = 5f;
        [SerializeField, Min(0.01f)] private float _probeDepth = 20f;

        private readonly HashSet<IDamageable> _damagedTargets = new();
        private Transform _attacker;
        private Vector3 _origin;
        private Vector3 _direction;
        private Vector3 _previousGroundPoint;
        private float _damage;
        private AttackImpactInfo _impact;
        private float _maximumDistance;
        private float _traveledDistance;
        private float _nextEruptionDistance;
        private bool _isActive;

        public event Action<Vector3, Vector3> OnErupted;
        public bool IsConfigurationValid => _speed > 0f && _eruptionSpacing >= 0.1f &&
            _damageRadius > 0f && _groundMask.value != 0 && _damageMask.value != 0 && _probeDepth > 0f &&
            IsFinite(_speed) && IsFinite(_eruptionSpacing) && IsFinite(_damageRadius);

        public bool Initialize(Transform p_attacker, Vector3 p_origin, Vector3 p_direction,
            float p_damage, float p_maximumDistance, in AttackImpactInfo p_impact)
        {
            Vector3 direction = Vector3.ProjectOnPlane(p_direction, Vector3.up);
            if (!IsConfigurationValid || p_attacker == null || direction.sqrMagnitude <= 0.0001f ||
                p_damage <= 0f || !IsFinite(p_damage) || p_maximumDistance <= 0f || !IsFinite(p_maximumDistance) ||
                !TryGetGround(p_origin, p_origin.y, out RaycastHit ground))
                return false;

            _attacker = p_attacker;
            _origin = ground.point;
            _previousGroundPoint = ground.point;
            _direction = direction.normalized;
            _damage = p_damage;
            _maximumDistance = p_maximumDistance;
            _impact = p_impact;
            _traveledDistance = 0f;
            _nextEruptionDistance = 0f;
            _damagedTargets.Clear();
            _isActive = true;
            transform.SetPositionAndRotation(ground.point, Quaternion.LookRotation(_direction));
            // 첫 폭발도 Update에서 발생시켜 View의 구독과 생성 초기화를 마친다.
            return true;
        }

        private void Update()
        {
            if (!_isActive || Time.deltaTime <= 0f)
                return;
            if (_attacker == null)
            {
                Finish();
                return;
            }

            _traveledDistance = Mathf.Min(_maximumDistance, _traveledDistance + _speed * Time.deltaTime);
            while (_isActive && _nextEruptionDistance <= _traveledDistance)
            {
                Vector3 position = _origin + _direction * _nextEruptionDistance;
                if (!TryGetGround(position, _previousGroundPoint.y, out RaycastHit ground))
                {
                    Finish();
                    return;
                }

                // 지형을 관통해 벽 뒤에 폭발을 생성하지 않는다.
                Vector3 lift = Vector3.up * 0.1f;
                if (_nextEruptionDistance > 0f && Physics.Linecast(_previousGroundPoint + lift,
                        ground.point + lift, _groundMask, QueryTriggerInteraction.Ignore))
                {
                    Finish();
                    return;
                }

                transform.position = ground.point;
                _previousGroundPoint = ground.point;
                ApplyEruptionDamage(ground.point, ground.normal);
                OnErupted?.Invoke(ground.point, ground.normal);

                if (_nextEruptionDistance >= _maximumDistance)
                {
                    Finish();
                    return;
                }
                // 사거리가 간격의 배수가 아니어도 마지막 지점은 한 번 처리한다.
                _nextEruptionDistance = Mathf.Min(_maximumDistance, _nextEruptionDistance + _eruptionSpacing);
            }
        }

        private bool TryGetGround(Vector3 p_position, float p_height, out RaycastHit p_hit)
        {
            Vector3 probe = new(p_position.x, p_height + _probeHeight, p_position.z);
            return Physics.Raycast(probe, Vector3.down, out p_hit, _probeHeight + _probeDepth,
                _groundMask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyEruptionDamage(Vector3 p_point, Vector3 p_normal)
        {
            // 보스 패턴의 소수 폭발에 완전한 검색을 사용하여 Collider 버퍼 초과로 대상을 놓치지 않는다.
            Collider[] overlaps = Physics.OverlapSphere(p_point, _damageRadius, _damageMask,
                QueryTriggerInteraction.Collide);
            foreach (Collider collider in overlaps)
            {
                if (!DamageSystem.TryGetDamageable(collider, out IDamageable target) ||
                    _damagedTargets.Contains(target))
                    continue;
                DamageInfo damage = new(_attacker, _damage, p_point, p_normal, _direction,
                    _impact, EDamageDeliveryType.Ranged);
                if (DamageSystem.TryApply(collider, damage))
                    _damagedTargets.Add(target);
            }
        }

        private void Finish()
        {
            _isActive = false;
            Destroy(gameObject);
        }

        private void OnDisable() => _isActive = false;

        private void OnValidate()
        {
            _speed = Mathf.Max(0.01f, _speed);
            _eruptionSpacing = Mathf.Max(0.1f, _eruptionSpacing);
            _damageRadius = Mathf.Max(0.01f, _damageRadius);
            _probeHeight = Mathf.Max(0f, _probeHeight);
            _probeDepth = Mathf.Max(0.01f, _probeDepth);
        }

        private static bool IsFinite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
    }
}
