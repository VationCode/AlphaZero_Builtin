using System;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Item.Weapon.Range
{
    // 공격 요청 생성과 공격 방식 선택을 소유하고 실행 결과만 부모에 전달한다.
    internal sealed class RangeWeaponAttackModule
    {
        private readonly HitscanAttackModule _hitscan = new();
        private readonly PenetrationAttackModule _penetration = new();
        private readonly ProjectileAttackModule _projectile = new();

        private RangeWeaponSettings _settings;
        private RangeWeaponAttackSettings _attackSettings;
        private RangeWeaponContext _context;
        private Transform _muzzle;
        private Action<RangeAttackRequest> _publishFired;
        private Action<RangeAttackResult> _publishTrajectory;
        private Action<RangeHitResult> _publishHit;
        private Action<ProjectileEntity> _publishProjectile;

        public bool Bind(
            RangeWeaponSettings p_settings,
            RangeWeaponAttackSettings p_attackSettings,
            RangeWeaponContext p_context,
            Transform p_muzzle,
            Action<RangeAttackRequest> p_publishFired,
            Action<RangeAttackResult> p_publishTrajectory,
            Action<RangeHitResult> p_publishHit,
            Action<ProjectileEntity> p_publishProjectile)
        {
            if (p_settings == null ||
                p_attackSettings == null ||
                !p_attackSettings.IsValid ||
                p_context == null ||
                p_muzzle == null ||
                p_publishFired == null ||
                p_publishTrajectory == null ||
                p_publishHit == null ||
                p_publishProjectile == null)
            {
                return false;
            }

            _settings = p_settings;
            _attackSettings = p_attackSettings;
            _context = p_context;
            _muzzle = p_muzzle;
            _publishFired = p_publishFired;
            _publishTrajectory = p_publishTrajectory;
            _publishHit = p_publishHit;
            _publishProjectile = p_publishProjectile;
            return true;
        }

        public void Unbind()
        {
            _settings = null;
            _attackSettings = null;
            _context = null;
            _muzzle = null;
            _publishFired = null;
            _publishTrajectory = null;
            _publishHit = null;
            _publishProjectile = null;
        }

        public bool TryFire(
            float p_bonusDamage,
            bool p_isAimViewActive)
        {
            if (_settings == null ||
                _context == null ||
                !_context.HasUser ||
                _muzzle == null ||
                !_context.TryGetAttackPose(
                    out Vector3 attackOrigin,
                    out Vector3 attackDirection,
                    out _))
            {
                return false;
            }

            float damage =
                Mathf.Max(
                    0f,
                    _settings.BaseDamage + Mathf.Max(0f, p_bonusDamage)) +
                _context.AdditionalDamage;
            float spreadAngle =
                _settings.ShotSettings.SpreadAngle;
            float recoil =
                _settings.FireResponseSettings.Recoil;

            // Aim View의 Secondary가 유지되는 동안만 조준 보정 배율을 적용한다.
            if (p_isAimViewActive)
            {
                spreadAngle *=
                    _settings.SecondarySettings.AimSpreadMultiplier;
                recoil *=
                    _settings.SecondarySettings.AimRecoilMultiplier;
            }

            RangeAttackRequest request = new(
                _context.Attacker,
                _muzzle.position,
                attackOrigin,
                attackDirection,
                damage,
                _settings.MaxDistance,
                _settings.ImpactSettings.CreateInfo(),
                spreadAngle,
                _settings.ShotSettings.TrajectoryCount,
                recoil);

            if (!Execute(request))
                return false;

            _context.SetLastFireDirection(request.Direction);
            _publishFired?.Invoke(request);
            return true;
        }

        private bool Execute(in RangeAttackRequest p_request)
        {
            if (_settings == null ||
                _attackSettings == null ||
                !p_request.IsValid)
                return false;

            return _attackSettings.AttackType switch
            {
                ERangeAttackType.Hitscan =>
                    _hitscan.Execute(
                        p_request,
                        _attackSettings.HitMask,
                        _publishTrajectory,
                        _publishHit),
                ERangeAttackType.Penetration =>
                    _penetration.Execute(
                        p_request,
                        _attackSettings.Penetration,
                        _attackSettings.HitMask,
                        _publishTrajectory,
                        _publishHit),
                ERangeAttackType.Projectile =>
                    _projectile.Execute(
                        p_request,
                        _attackSettings.Projectile,
                        _publishProjectile),
                _ => false
            };
        }

        // 모든 Range 세부 공격이 동일한 원형 분산 계산을 사용한다.
        internal static Vector3 ResolveSpreadDirection(
            in RangeAttackRequest p_request)
        {
            if (p_request.SpreadAngle <= 0f)
                return p_request.Direction;

            float spreadRadius = Mathf.Tan(
                p_request.SpreadAngle * Mathf.Deg2Rad);
            Vector2 spreadOffset =
                UnityEngine.Random.insideUnitCircle * spreadRadius;
            Quaternion aimRotation =
                Quaternion.LookRotation(p_request.Direction);

            return (aimRotation * new Vector3(
                    spreadOffset.x,
                    spreadOffset.y,
                    1f))
                .normalized;
        }
    }
}
