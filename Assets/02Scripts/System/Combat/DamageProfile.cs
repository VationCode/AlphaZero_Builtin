using System;
using UnityEngine;

namespace Alpha.Combat
{
    // 공격자가 Inspector에서 설정하는 공용 피해 프로필이다.
    [Serializable]
    public class DamageProfile
    {
        [SerializeField, Min(0f)]
        private float _damage = 10f;

        [SerializeField]
        private AttackImpactSettings _impactSettings = new();

        public float Damage => _damage;
        public AttackImpactInfo Impact =>
            _impactSettings?.CreateInfo() ?? default;

        public bool IsValid => _damage > 0f;

        public void Validate()
        {
            _damage = Mathf.Max(0f, _damage);
            _impactSettings ??= new AttackImpactSettings();
            _impactSettings.Validate();
        }
    }
}
