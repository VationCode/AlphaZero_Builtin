using UnityEngine;

namespace Alpha.Projectile.View
{
    // Projectile 충돌 결과를 발사 무기에서 전달받은 Particle 표현으로 재생한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Projectile))]
    public sealed class ProjectileImpactEffectView : MonoBehaviour
    {
        [SerializeField]
        private Projectile _projectile;

        private ParticleSystem _particlePrefab;
        private float _particleLifetime = 5f;
        private bool _isSubscribed;

        private void Awake()
        {
            ResolveProjectile();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // Weapon View의 표현 설정값만 복사받고 Weapon 내부는 참조하지 않는다.
        public void Configure(
            ParticleSystem p_particlePrefab,
            float p_particleLifetime)
        {
            _particlePrefab = p_particlePrefab;
            _particleLifetime = Mathf.Max(
                0.01f,
                p_particleLifetime);
        }

        private void ResolveProjectile()
        {
            _projectile ??= GetComponent<Projectile>();
        }

        private void Subscribe()
        {
            if (_isSubscribed || !isActiveAndEnabled)
                return;

            ResolveProjectile();

            if (_projectile == null)
                return;

            _projectile.OnImpacted += HandleImpacted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _projectile == null)
                return;

            _projectile.OnImpacted -= HandleImpacted;
            _isSubscribed = false;
        }

        private void HandleImpacted(ProjectileImpactResult p_result)
        {
            if (_particlePrefab == null)
                return;

            ParticleSystem effect = Instantiate(
                _particlePrefab,
                p_result.Point,
                Quaternion.LookRotation(p_result.Normal));

            effect.Play(true);
            Destroy(effect.gameObject, _particleLifetime);
        }

        private void OnValidate()
        {
            ResolveProjectile();
        }
    }
}
