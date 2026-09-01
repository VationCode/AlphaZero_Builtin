using Alpha.Combat;
using UnityEngine;

namespace Alpha.Projectile
{
    // Projectile 명중 시 잠시 활성화되어 범위 피해를 전달하는 자식 Collider다.
    [DisallowMultipleComponent]
    public sealed class ProjectileDamageArea : MonoBehaviour, IDamageSource
    {
        [Tooltip("피해 범위로 사용할 Trigger Collider입니다.")]
        [SerializeField]
        private Collider _damageCollider;

        [Tooltip("명중 후 피해 Collider를 유지할 시간입니다.")]
        [SerializeField, Min(0.02f)]
        private float _activeDuration = 0.1f;

        private Transform _attacker;
        private float _damage;
        private AttackImpactInfo _impact;
        private int _attackId;
        private bool _isActive;

        public int SourceId => GetInstanceID();
        public int AttackId => _isActive ? _attackId : 0;
        public float ActiveDuration => Mathf.Max(0.02f, _activeDuration);
        public float PreviewRadius => CalculatePreviewRadius();
        public bool IsConfigurationValid =>
            _damageCollider != null &&
            _damageCollider.isTrigger &&
            _activeDuration > 0f;

        public bool Activate(
            Transform p_attacker,
            float p_damage,
            in AttackImpactInfo p_impact)
        {
            if (!IsConfigurationValid ||
                p_attacker == null ||
                p_damage <= 0f)
            {
                Deactivate();
                return false;
            }

            _attacker = p_attacker;
            _damage = p_damage;
            _impact = p_impact;
            _attackId = _attackId == int.MaxValue
                ? 1
                : _attackId + 1;
            _isActive = true;
            enabled = true;
            _damageCollider.enabled = true;
            Physics.SyncTransforms();
            return true;
        }

        public void Deactivate()
        {
            if (_damageCollider != null)
                _damageCollider.enabled = false;

            _attacker = null;
            _damage = 0f;
            _impact = default;
            _isActive = false;
        }

        public bool TryCreateDamageInfo(
            Transform p_target,
            out DamageInfo p_damageInfo)
        {
            p_damageInfo = default;

            if (!_isActive ||
                p_target == null ||
                _attacker == null ||
                _damage <= 0f)
            {
                return false;
            }

            Vector3 direction = p_target.position - transform.position;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = _attacker.forward;

            direction.Normalize();

            p_damageInfo = new DamageInfo(
                _attacker,
                _damage,
                p_target.position,
                -direction,
                direction,
                p_impact: _impact,
                p_deliveryType: EDamageDeliveryType.Ranged);

            return true;
        }

        private void Awake()
        {
            ResolveCollider();
            Deactivate();
        }

        private void OnValidate()
        {
            ResolveCollider();
            _activeDuration = Mathf.Max(0.02f, _activeDuration);

            if (_damageCollider != null)
                _damageCollider.isTrigger = true;
        }

        private void OnDisable()
        {
            Deactivate();
        }

        private void ResolveCollider()
        {
            _damageCollider ??= GetComponent<Collider>();
        }

        private float CalculatePreviewRadius()
        {
            if (_damageCollider == null)
                return 0f;

            Vector3 scale = Abs(_damageCollider.transform.lossyScale);

            return _damageCollider switch
            {
                SphereCollider sphere =>
                    sphere.center.magnitude * MaxComponent(scale) +
                    sphere.radius * MaxComponent(scale),

                BoxCollider box =>
                    Vector3.Scale(box.center, scale).magnitude +
                    Vector3.Scale(box.size * 0.5f, scale).magnitude,

                CapsuleCollider capsule =>
                    Vector3.Scale(capsule.center, scale).magnitude +
                    Mathf.Max(
                        capsule.radius * MaxComponent(scale),
                        capsule.height * 0.5f * MaxComponent(scale)),

                _ => 0f
            };
        }

        private static Vector3 Abs(Vector3 p_value)
        {
            return new Vector3(
                Mathf.Abs(p_value.x),
                Mathf.Abs(p_value.y),
                Mathf.Abs(p_value.z));
        }

        private static float MaxComponent(Vector3 p_value)
        {
            return Mathf.Max(p_value.x, p_value.y, p_value.z);
        }
    }
}
