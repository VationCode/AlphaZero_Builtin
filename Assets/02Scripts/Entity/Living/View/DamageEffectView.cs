using Alpha.Combat;
using UnityEngine;

namespace Alpha.Living.Effect
{
    // 성공한 피해를 피격 지점의 실제 Particle Effect로 표현한다.
    [DisallowMultipleComponent]
    public sealed class DamageEffectView : MonoBehaviour
    {
        [Tooltip("피해가 적용된 HitPoint에 생성할 Effect Prefab입니다.")]
        [SerializeField]
        private GameObject _effectPrefab;

        [Tooltip("반복 Particle을 포함한 Effect Instance의 유지 시간입니다.")]
        [SerializeField, Min(0.01f)]
        private float _lifetime = 1f;

        private DamageReceiverModule _damageReceiver;
        private bool _isSubscribed;

        // Entity Core가 실제 피해 수신 진입점을 전달한다.
        public void Bind(DamageReceiverModule p_damageReceiver)
        {
            if (ReferenceEquals(_damageReceiver, p_damageReceiver))
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            _damageReceiver = p_damageReceiver;
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _damageReceiver = null;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            _lifetime = Mathf.Max(0.01f, _lifetime);
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                _damageReceiver == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            _damageReceiver.OnDamaged += HandleDamaged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _damageReceiver == null)
                return;

            _damageReceiver.OnDamaged -= HandleDamaged;
            _isSubscribed = false;
        }

        private void HandleDamaged(DamageInfo p_damageInfo)
        {
            if (_effectPrefab == null)
                return;

            Vector3 hitNormal = ResolveHitNormal(p_damageInfo);
            Quaternion rotation = Quaternion.FromToRotation(
                Vector3.forward,
                hitNormal);

            // Knockback 이동을 따라가지 않도록 피격 월드 위치에 독립 생성한다.
            GameObject effect = Instantiate(
                _effectPrefab,
                p_damageInfo.HitPoint,
                rotation);

            ParticleSystem[] particles =
                effect.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            foreach (ParticleSystem particle in particles)
                particle.Play(true);

            Destroy(effect, _lifetime);
        }

        private static Vector3 ResolveHitNormal(
            in DamageInfo p_damageInfo)
        {
            if (p_damageInfo.HitNormal.sqrMagnitude > 0.0001f)
                return p_damageInfo.HitNormal.normalized;

            if (p_damageInfo.Direction.sqrMagnitude > 0.0001f)
                return -p_damageInfo.Direction.normalized;

            return Vector3.up;
        }
    }
}
