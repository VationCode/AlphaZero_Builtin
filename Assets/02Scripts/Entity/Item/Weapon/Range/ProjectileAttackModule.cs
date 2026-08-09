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
        private float _projectileSpeed = 120f;

        [SerializeField, Min(0f)]
        private float _gravityScale = 1f;

        [SerializeField, Min(0f)]
        private float _projectileRadius = 0.025f;

        protected override bool OnExecute(
            in RangeAttackRequest p_request,
            out RangeAttackResult p_result)
        {
            p_result = default;

            if (_projectilePrefab == null ||
                _projectileSpeed <= 0f)
            {
                return false;
            }

            ProjectileEntity projectile = Instantiate(
                _projectilePrefab,
                p_request.Origin,
                Quaternion.LookRotation(p_request.Direction));

            Vector3 initialVelocity =
                p_request.Direction *
                _projectileSpeed;

            if (projectile.Initialize(
                    p_request,
                    initialVelocity,
                    Physics.gravity * _gravityScale,
                    _projectileRadius))
            {
                p_result = RangeAttackResult.Deferred();
                return true;
            }

            Destroy(projectile.gameObject);
            return false;
        }
    }
}
