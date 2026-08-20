using Alpha.Projectile;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Item.Weapon.Range
{
    // RangeWeapon의 공격 요청을 Projectile 생성과 발사로 실행한다.
    public sealed class RangeWeaponProjectileModule : RangeAttackModule
    {
        [Header("Projectile Launch")]
        [SerializeField]
        private ProjectileLaunchSettings _launchSettings = new(
            null,
            120f,
            (LayerMask)129);

        protected override bool OnExecute(
            in RangeAttackRequest p_request,
            out RangeAttackResult p_result)
        {
            p_result = default;

            if (!_launchSettings.IsValid ||
                !_launchSettings.Prefab.IsConfigurationValid)
                return false;

            ProjectileEntity projectile = Instantiate(
                _launchSettings.Prefab,
                p_request.Origin,
                Quaternion.LookRotation(p_request.Direction));

            if (projectile.Initialize(
                    p_request,
                    _launchSettings))
            {
                p_result = RangeAttackResult.Deferred();
                return true;
            }

            Destroy(projectile.gameObject);
            return false;
        }

        private void OnValidate()
        {
            _launchSettings.Validate();
        }
    }
}
