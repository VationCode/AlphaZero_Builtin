using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Item.Weapon.Range
{
    // 공격 요청으로 실제 투사체 Entity를 생성하고 초기화한다.
    public class ProjectileAttackModule : RangeAttackModule
    {
        [Header("Projectile")]
        [SerializeField]
        private ProjectileEntity _projectilePrefab;

        [SerializeField, Min(0.01f)]
        private float _projectileSpeed = 20f;

        protected override bool OnExecute(
            in RangeAttackRequest p_request)
        {
            if (_projectilePrefab == null ||
                _projectileSpeed <= 0f)
            {
                return false;
            }

            ProjectileEntity projectile = Instantiate(
                _projectilePrefab,
                p_request.Origin,
                Quaternion.LookRotation(p_request.Direction));

            if (projectile.Initialize(
                    p_request,
                    _projectileSpeed))
            {
                return true;
            }

            Destroy(projectile.gameObject);
            return false;
        }
    }
}
