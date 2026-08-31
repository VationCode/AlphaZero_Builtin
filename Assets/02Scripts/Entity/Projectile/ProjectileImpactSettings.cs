using System;
using UnityEngine;

namespace Alpha.Projectile
{
    public enum EProjectileImpactType
    {
        Direct,
        Radial
    }

    // ProjectileDefinition이 소유하는 명중 이후 피해 방식과 범위를 보관한다.
    [Serializable]
    public struct ProjectileImpactSettings
    {
        private const float MinimumRadialRadius = 0.01f;

        [SerializeField]
        private EProjectileImpactType _impactType;

        [Tooltip("Radial 충돌 시 명중 지점을 중심으로 피해를 적용할 반경입니다.")]
        [SerializeField, Min(0f)]
        private float _damageRadius;

        public EProjectileImpactType ImpactType => _impactType;
        public float DamageRadius => _damageRadius;
        public bool IsRadial =>
            _impactType == EProjectileImpactType.Radial;

        public bool IsValid =>
            _impactType switch
            {
                EProjectileImpactType.Direct => true,
                EProjectileImpactType.Radial => _damageRadius > 0f,
                _ => false
            };

        public ProjectileImpactSettings(
            EProjectileImpactType p_impactType,
            float p_damageRadius)
        {
            _impactType = p_impactType;
            _damageRadius = ResolveRadius(
                p_impactType,
                p_damageRadius);
        }

        public void Validate()
        {
            _damageRadius = ResolveRadius(
                _impactType,
                _damageRadius);
        }

        private static float ResolveRadius(
            EProjectileImpactType p_impactType,
            float p_damageRadius)
        {
            return p_impactType == EProjectileImpactType.Radial
                ? Mathf.Max(MinimumRadialRadius, p_damageRadius)
                : Mathf.Max(0f, p_damageRadius);
        }
    }
}
