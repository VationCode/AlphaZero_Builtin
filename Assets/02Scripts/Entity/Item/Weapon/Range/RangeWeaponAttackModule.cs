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

        public bool TryFire(float p_bonusDamage)
        {
            if (_settings == null ||
                _context == null ||
                !_context.HasUser ||
                _muzzle == null ||
                !_context.TryGetAttackPose(
                    out Vector3 attackOrigin,
                    out Vector3 attackDirection))
            {
                return false;
            }

            float damage =
                Mathf.Max(
                    0f,
                    _settings.BaseDamage + Mathf.Max(0f, p_bonusDamage)) +
                _context.AdditionalDamage;

            RangeAttackRequest request = new(
                _context.Attacker,
                _muzzle.position,
                attackOrigin,
                attackDirection,
                damage,
                _settings.MaxDistance,
                _settings.ImpactSettings.CreateInfo(),
                _settings.ShotSettings.SpreadAngle,
                _settings.ShotSettings.TrajectoryCount);

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
                        _attackSettings.HitMask,
                        _publishProjectile),
                _ => false
            };
        }

        public bool TryPredictProjectileTrajectory(
            Vector3 p_origin,
            Vector3 p_direction,
            float p_simulationStep,
            Vector3[] p_points,
            out ProjectileTrajectoryResult p_result)
        {
            if (_attackSettings?.AttackType != ERangeAttackType.Projectile)
            {
                p_result = default;
                return false;
            }

            return _projectile.TryPredictTrajectory(
                _attackSettings.Projectile,
                _attackSettings.HitMask,
                p_origin,
                p_direction,
                _settings.MaxDistance,
                p_simulationStep,
                p_points,
                out p_result);
        }

        public bool TryGetProjectileRadialDamageRadius(out float p_radius)
        {
            if (_attackSettings?.AttackType != ERangeAttackType.Projectile)
            {
                p_radius = 0f;
                return false;
            }

            return _projectile.TryGetRadialDamageRadius(
                _attackSettings.Projectile,
                out p_radius);
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
