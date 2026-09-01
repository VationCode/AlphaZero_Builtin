using System;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Item.Weapon.Range
{
    // Projectile 공격이 생성할 Prefab 참조만 소유한다.
    [Serializable]
    public sealed class ProjectileAttackSettings : RangeAttackSettings
    {
        [Tooltip("속도, 중력, 충돌 Layer와 피해 Collider를 직접 가진 Projectile Prefab입니다.")]
        [SerializeField]
        private ProjectileEntity _projectilePrefab;

        public override ERangeAttackType AttackType =>
            ERangeAttackType.Projectile;
        public override bool IsValid =>
            _projectilePrefab != null &&
            _projectilePrefab.IsConfigurationValid;
        public ProjectileEntity ProjectilePrefab => _projectilePrefab;

        public override void Validate()
        {
        }
    }
}
