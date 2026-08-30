using Alpha.Projectile;
using System;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Item.Weapon.Range
{
    // Projectile 공격에만 필요한 Prefab과 발사 속도를 소유한다.
    [Serializable]
    public sealed class ProjectileAttackSettings : RangeAttackSettings
    {
        [Tooltip("발사할 Projectile Entity Prefab입니다.")]
        [SerializeField]
        private ProjectileEntity _prefab;

        [Tooltip("Projectile Entity에 전달할 초당 이동 속도입니다.")]
        [SerializeField, Min(0.01f)]
        private float _speed = 120f;

        public override ERangeAttackType AttackType =>
            ERangeAttackType.Projectile;
        public override bool IsValid => _prefab != null && _speed > 0f;
        public ProjectileEntity Prefab => _prefab;
        public float Speed => Mathf.Max(0.01f, _speed);

        public ProjectileLaunchSettings CreateLaunchSettings(
            LayerMask p_hitMask)
        {
            return new ProjectileLaunchSettings(
                _prefab,
                Speed,
                p_hitMask);
        }

        public override void Validate()
        {
            _speed = Mathf.Max(0.01f, _speed);
        }
    }
}
