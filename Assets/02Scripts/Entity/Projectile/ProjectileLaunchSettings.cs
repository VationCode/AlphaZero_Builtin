using System;
using UnityEngine;

namespace Alpha.Projectile
{
    // 발사 주체가 선택한 탄종과 발사 조건을 Projectile에 전달한다.
    [Serializable]
    public struct ProjectileLaunchSettings
    {
        [Tooltip("발사 주체가 사용할 탄종 정의입니다.")]
        [SerializeField]
        private ProjectileDefinition _projectile;

        [Tooltip("발사 주체가 Projectile에 부여할 초당 이동 속도입니다.")]
        [SerializeField, Min(0.01f)]
        private float _speed;

        [Tooltip("Projectile이 충돌 대상으로 검사할 Layer입니다.")]
        [SerializeField]
        private LayerMask _hitMask;

        public ProjectileDefinition Projectile => _projectile;
        public Projectile Prefab => _projectile != null
            ? _projectile.Prefab
            : null;
        public float GravityScale => _projectile != null
            ? _projectile.GravityScale
            : 0f;
        public Vector3 Gravity => _projectile != null
            ? _projectile.Gravity
            : Vector3.zero;
        public ProjectileImpactSettings ImpactSettings =>
            _projectile != null
                ? _projectile.ImpactSettings
                : default;
        public float Speed => _speed;
        public LayerMask HitMask => _hitMask;

        public bool IsValid =>
            _projectile != null &&
            _projectile.IsValid &&
            _speed > 0f;

        public ProjectileLaunchSettings(
            ProjectileDefinition p_projectile,
            float p_speed,
            LayerMask p_hitMask)
        {
            _projectile = p_projectile;
            _speed = Mathf.Max(0.01f, p_speed);
            _hitMask = p_hitMask;
        }

        // 중첩 직렬화 값은 실제 소유자의 OnValidate에서 보정한다.
        public void Validate()
        {
            _speed = Mathf.Max(0.01f, _speed);
        }
    }
}
