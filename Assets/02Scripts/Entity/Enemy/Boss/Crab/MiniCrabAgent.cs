using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Enemy.Boss
{
    // 소환 후 이동, 접촉 감지, 수명과 단발 폭발을 미니 게 자신이 관리한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(AreaDetectionModule))]
    public sealed class MiniCrabAgent : MonoBehaviour
    {
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int WalkLeftState = Animator.StringToHash("Base Layer.Walk_L");
        private static readonly int WalkRightState = Animator.StringToHash("Base Layer.Walk_R");

        [SerializeField] private Rigidbody _body;
        [SerializeField] private Collider _contactTrigger;
        [SerializeField] private Collider _bodyCollider;
        [SerializeField] private AreaDetectionModule _explosionArea;
        [SerializeField] private Transform _model;
        [SerializeField] private Transform _explosionEffect;
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveStartDelay = 1f;
        [SerializeField, Min(0f)] private float _moveSpeed = 3f;
        [SerializeField, Min(0.01f)] private float _sideChangeInterval = 1f;
        [Header("Explosion")]
        [SerializeField, Min(0.01f)] private float _lifetime = 6f;
        [SerializeField, Min(0f)] private float _explosionEffectDuration = 2f;
        [SerializeField] private DamageProfile _damage = new();

        private readonly HashSet<IDamageable> _damagedTargets = new();
        private Transform _attacker;
        private Animator _animator;
        private int _movingState;
        private Vector3 _sideDirection;
        private float _spawnTime;
        private bool _initialized;
        private bool _exploded;

        public void Initialize(Transform attacker, Collider[] bossColliders)
        {
            _attacker = attacker;
            _body ??= GetComponent<Rigidbody>();
            _explosionArea ??= GetComponent<AreaDetectionModule>();
            if (_body == null || _explosionArea == null)
            {
                Debug.LogError("MiniCrabAgent: 이동 또는 폭발 범위 구성 요소가 없습니다.", this);
                enabled = false;
                return;
            }
            _explosionArea.Bind(transform);
            IgnoreBossCollisions(bossColliders);
            _animator = _model != null ? _model.GetComponent<Animator>() :
                GetComponentInChildren<Animator>();
            _sideDirection = transform.right * (Random.value < 0.5f ? -1f : 1f);
            _spawnTime = Time.time;
            _initialized = true;
            _exploded = false;
        }

        // 소환된 개체와 보스 사이의 물리·Trigger 충돌만 제외한다.
        private void IgnoreBossCollisions(Collider[] bossColliders)
        {
            if (bossColliders == null)
                return;

            Collider[] crabColliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider crabCollider in crabColliders)
            {
                if (crabCollider == null || !crabCollider.enabled ||
                    !crabCollider.gameObject.activeInHierarchy)
                    continue;

                foreach (Collider bossCollider in bossColliders)
                {
                    if (bossCollider == null || bossCollider == crabCollider ||
                        !bossCollider.enabled || !bossCollider.gameObject.activeInHierarchy)
                        continue;
                    Physics.IgnoreCollision(crabCollider, bossCollider);
                }
            }
        }

        private void Update()
        {
            if (_initialized && !_exploded && Time.time - _spawnTime >= _lifetime)
                Explode();
        }

        private void FixedUpdate()
        {
            if (!_initialized || _exploded || _body == null)
                return;
            float elapsed = Time.time - _spawnTime - _moveStartDelay;
            float side = elapsed < 0f ? 0f :
                (Mathf.FloorToInt(elapsed / _sideChangeInterval) % 2 == 0 ? 1f : -1f);
            Vector3 velocity = _body.linearVelocity;
            Vector3 horizontal = _sideDirection * (_moveSpeed * side);
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
            _body.linearVelocity = velocity;
            int state = side == 0f ? IdleState :
                Vector3.Dot(horizontal, transform.right) >= 0f
                    ? WalkRightState : WalkLeftState;
            if (_animator != null && _animator.isActiveAndEnabled &&
                state != _movingState && _animator.HasState(0, state))
            {
                _animator.CrossFadeInFixedTime(state, 0.1f);
                _movingState = state;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_initialized || _exploded || other == null ||
                (_contactTrigger != null && !_contactTrigger.enabled))
                return;
            if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
                Explode();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_initialized && !_exploded && collision.collider != null &&
                (collision.collider.CompareTag("Player") ||
                 collision.collider.transform.root.CompareTag("Player")))
                Explode();
        }

        private void Explode()
        {
            if (_exploded)
                return;
            _exploded = true;
            _damagedTargets.Clear();
            if (_explosionArea != null && _damage != null && _damage.IsValid)
            {
                Physics.SyncTransforms();
                int count = _explosionArea.CollectHits();
                for (int index = 0; index < count; index++)
                {
                    if (!_explosionArea.TryGetHit(index, out DetectionAreaHit hit) ||
                        !(hit.Collider.CompareTag("Player") ||
                          hit.Collider.transform.root.CompareTag("Player")) ||
                        !DamageSystem.TryGetDamageable(hit.Collider, out IDamageable target) ||
                        !_damagedTargets.Add(target))
                        continue;
                    Vector3 direction = Vector3.ProjectOnPlane(hit.Direction, Vector3.up);
                    if (direction.sqrMagnitude < 0.0001f)
                        direction = transform.forward;
                    DamageInfo damage = new(_attacker != null ? _attacker : transform,
                        _damage.Damage, hit.HitPoint, -hit.Direction, direction,
                        _damage.Impact, EDamageDeliveryType.Ranged);
                    DamageSystem.TryApply(hit.Collider, damage);
                }
            }

            if (_contactTrigger != null)
                _contactTrigger.enabled = false;
            if (_bodyCollider != null)
                _bodyCollider.enabled = false;
            if (_body != null)
            {
                _body.linearVelocity = Vector3.zero;
                _body.isKinematic = true;
            }
            if (_model != null)
                _model.gameObject.SetActive(false);
            if (_explosionEffect != null)
            {
                _explosionEffect.gameObject.SetActive(true);
                foreach (ParticleSystem particle in
                         _explosionEffect.GetComponentsInChildren<ParticleSystem>(true))
                    particle.Play(true);
            }
            Destroy(gameObject, _explosionEffectDuration);
        }

        private void OnValidate()
        {
            _body ??= GetComponent<Rigidbody>();
            _explosionArea ??= GetComponent<AreaDetectionModule>();
            _moveStartDelay = Mathf.Max(0f, _moveStartDelay);
            _moveSpeed = Mathf.Max(0f, _moveSpeed);
            _sideChangeInterval = Mathf.Max(0.01f, _sideChangeInterval);
            _lifetime = Mathf.Max(0.01f, _lifetime);
            _explosionEffectDuration = Mathf.Max(0f, _explosionEffectDuration);
            _damage ??= new DamageProfile();
            _damage.Validate();
        }
    }
}
