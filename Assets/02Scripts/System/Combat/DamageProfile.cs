using System;
using UnityEngine;

namespace Alpha.Combat
{
    public enum EDamageType
    {
        Physical,
        Energy,
        Explosion
    }

    public enum EHitReaction
    {
        None,
        Light,
        Heavy,
        Knockdown,
        Launch
    }

    // 공격자가 Inspector에서 설정하는 공용 피해 프로필이다.
    [Serializable]
    public class DamageProfile
    {
        [SerializeField, Min(0f)]
        private float _damage = 10f;

        [SerializeField]
        private EDamageType _damageType;

        [SerializeField]
        private EHitReaction _hitReaction;

        [SerializeField, Min(0f)]
        private float _knockbackDistance;

        [SerializeField, Min(0f)]
        private float _knockbackDuration;

        public float Damage => _damage;
        public EDamageType DamageType => _damageType;
        public EHitReaction HitReaction => _hitReaction;
        public float KnockbackDistance => _knockbackDistance;
        public float KnockbackDuration => _knockbackDuration;

        public bool IsValid => _damage > 0f;

        public void Validate()
        {
            _damage = Mathf.Max(0f, _damage);
            _knockbackDistance = Mathf.Max(0f, _knockbackDistance);
            _knockbackDuration = Mathf.Max(0f, _knockbackDuration);
        }
    }
}
