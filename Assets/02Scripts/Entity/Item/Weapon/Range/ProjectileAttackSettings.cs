using Alpha.Projectile;
using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // Projectile 공격이 사용할 탄종과 무기별 발사 조건을 소유한다.
    [Serializable]
    public sealed class ProjectileAttackSettings : RangeAttackSettings
    {
        [Tooltip("발사할 탄종의 Prefab, 중력, 명중 방식을 가진 정의입니다.")]
        [SerializeField]
        private ProjectileDefinition _projectile;

        [Tooltip("Projectile Entity에 전달할 초당 이동 속도입니다.")]
        [SerializeField, Min(0.01f)]
        private float _speed = 120f;

        public override ERangeAttackType AttackType =>
            ERangeAttackType.Projectile;
        public override bool IsValid =>
            _projectile != null &&
            _projectile.IsValid &&
            _speed > 0f;
        public ProjectileDefinition Projectile => _projectile;
        public float Speed => Mathf.Max(0.01f, _speed);

        public ProjectileLaunchSettings CreateLaunchSettings(
            LayerMask p_hitMask)
        {
            return new ProjectileLaunchSettings(
                _projectile,
                Speed,
                p_hitMask);
        }

        public override void Validate()
        {
            _speed = Mathf.Max(0.01f, _speed);
        }
    }
}
