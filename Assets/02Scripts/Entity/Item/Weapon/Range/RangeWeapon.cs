using System;
using Alpha.Projectile;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Item.Weapon.Range
{
    // Range 무기 자식 객체를 조립하고 외부 명령과 결과를 중계하는 대표 진입점이다.
    [DisallowMultipleComponent]
    public sealed class RangeWeapon : Weapon
    {
        public event Action<RangeAttackRequest> OnFired;
        public event Action<RangeAttackResult> OnTrajectoryResolved;
        public event Action<RangeHitResult> OnHitResolved;
        public event Action<ProjectileEntity> OnProjectileLaunched;
        public event Action<EWeaponActionType> OnActionStopped;

        [SerializeField]
        private RangeWeaponSettings _settings = new();

        [SerializeField]
        private RangeWeaponAttackSettings _attackSettings = new();

        [Header("Attack Dependencies")]
        [SerializeField]
        private Transform _muzzle;

        [Header("Hand IK")]
        [SerializeField]
        private Transform _leftHandIKTarget;

        [Header("Scene Preview")]
        [SerializeField]
        private bool _showRange = true;

        [SerializeField]
        private Color _rangeColor = new(0f, 0.8f, 1f, 1f);

        private readonly RangeWeaponContext _context = new();
        private readonly RangeWeaponActionFlow _actionFlow = new();
        private readonly RangeWeaponAttackModule _attackModule = new();

        public RangeWeaponSettings Settings => _settings;
        public RangeWeaponAttackSettings AttackSettings => _attackSettings;
        public Transform Muzzle => _muzzle != null ? _muzzle : transform;
        public Transform LeftHandIKTarget => _leftHandIKTarget;

        public sealed override EWeaponType WeaponType =>
            _settings != null
                ? (EWeaponType)_settings.WeaponType
                : EWeaponType.None;

        public ERangeAttackType AttackType =>
            _attackSettings?.AttackType ?? ERangeAttackType.None;
        public RangeShotSettings ShotSettings =>
            _settings?.ShotSettings;
        public RangeFireResponseSettings FireResponseSettings =>
            _settings?.FireResponseSettings;
        public float MaxDistance =>
            _settings?.MaxDistance ?? 0.01f;
        public RangeSecondarySettings SecondarySettings =>
            _settings?.SecondarySettings;
        public ERangeAimView AimView =>
            SecondarySettings?.CameraView ?? ERangeAimView.None;
        public bool IsChargeEnabled =>
            SecondarySettings?.Charge?.Enabled == true;
        public bool HasSecondaryAction =>
            SecondarySettings?.Enabled == true;
        public float StartRadius =>
            _attackSettings?.Penetration?.StartRadius ?? 0.01f;
        public float EndRadius =>
            _attackSettings?.Penetration?.EndRadius ?? 0.01f;

        public ERangeTriggerMode CurrentTriggerMode =>
            HasUseContext
                ? _actionFlow.CurrentTriggerMode
                : _settings?.DefaultTriggerMode ?? ERangeTriggerMode.Auto;
        public bool DidFireDuringPrimaryAction =>
            HasUseContext && _actionFlow.DidFireDuringPrimaryAction;
        public Vector3 LastFireDirection => _context.LastFireDirection;
        public bool IsSecondaryActive =>
            HasUseContext && _actionFlow.IsSecondaryActive;
        public bool CanSwitchTriggerMode =>
            HasUseContext && !HasActiveAction;
        public float ChargeRatio =>
            HasUseContext ? _actionFlow.ChargeRatio : 0f;
        public bool HasUseContext => _context.HasUser;

        protected sealed override bool CanInitialize(WeaponDTO p_data)
        {
            return p_data?.WeaponCategory == EWeaponCategory.Range;
        }

        protected override void OnInitialized()
        {
            _settings ??= new RangeWeaponSettings();
            _settings.Validate();
            _attackSettings ??= new RangeWeaponAttackSettings();
            _attackSettings.Validate();
            _muzzle ??= transform;
            _context.ClearUser();

            if (!_attackModule.Bind(
                    _settings,
                    _attackSettings,
                    _context,
                    Muzzle,
                    PublishFired,
                    PublishTrajectory,
                    PublishHit,
                    PublishProjectile))
            {
                Debug.LogError(
                    "원거리 무기 공격 객체를 초기화하지 못했습니다.",
                    this);
            }

            _actionFlow.Bind(_settings, _attackModule);

            if (!_attackSettings.IsValid)
            {
                Debug.LogError(
                    "원거리 무기의 공격 방식 설정이 올바르지 않습니다.",
                    this);
            }
        }

        // 장착 Entity의 구체 구현 대신 공격 출처와 보정 데이터만 연결한다.
        public bool BindUseContext(in RangeWeaponUseContext p_context)
        {
            if (!IsInitialized ||
                AttackType == ERangeAttackType.None ||
                !p_context.IsValid)
            {
                return false;
            }

            UnbindUseContext();

            if (!_context.BindUser(p_context))
                return false;

            _actionFlow.Reset();
            return true;
        }

        public void UnbindUseContext()
        {
            CancelSecondary();
            CancelAction();
            _context.ClearUser();
            _actionFlow.Reset();
        }

        // Player가 계산한 조준 결과를 내부 참조 없이 값으로 갱신한다.
        public bool SetAttackPose(in RangeWeaponAttackPose p_pose)
        {
            return _context.SetAttackPose(p_pose);
        }

        public void ClearAttackPose()
        {
            _context.ClearAttackPose();
        }

        public bool TryGetAttackPose(
            out Vector3 p_origin,
            out Vector3 p_direction)
        {
            return _context.TryGetAttackPose(
                out p_origin,
                out p_direction,
                out _);
        }

        // Projectile의 실제 이동식과 Collider로 최종 폭발점만 예측한다.
        public bool TryPredictProjectileImpact(
            Vector3 p_origin,
            Vector3 p_direction,
            float p_simulationStep,
            out ProjectileImpactResult p_result)
        {
            p_result = default;

            ProjectileAttackSettings projectileSettings =
                _attackSettings?.Projectile;

            if (AttackType != ERangeAttackType.Projectile ||
                projectileSettings == null ||
                !projectileSettings.IsValid)
            {
                return false;
            }

            return projectileSettings.ProjectilePrefab.TryPredictImpact(
                p_origin,
                p_direction,
                MaxDistance,
                p_simulationStep,
                out p_result);
        }

        // 실제 Radial 피해 설정을 조준 범위 View의 단일 반경 원본으로 제공한다.
        public bool TryGetProjectileRadialDamageRadius(
            out float p_damageRadius)
        {
            p_damageRadius = 0f;

            ProjectileEntity projectilePrefab =
                _attackSettings?.Projectile?.ProjectilePrefab;

            if (AttackType != ERangeAttackType.Projectile ||
                projectilePrefab == null ||
                !projectilePrefab.HasDamageArea)
            {
                return false;
            }

            p_damageRadius = projectilePrefab.DamageAreaPreviewRadius;
            return p_damageRadius > 0f;
        }

        public override bool EndsOnInputRelease(EWeaponActionType p_type)
        {
            return true;
        }

        protected override bool OnBeginAction(EWeaponActionType p_type)
        {
            return _actionFlow.TryBeginAction(
                p_type,
                HasUseContext);
        }

        protected override void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            ERangeWeaponActionResult result =
                _actionFlow.TickAction(p_type);

            if (result == ERangeWeaponActionResult.Completed)
                EndAction();
        }

        protected override void OnEndAction(EWeaponActionType p_type)
        {
            _actionFlow.EndAction(p_type);
            OnActionStopped?.Invoke(p_type);
        }

        protected override void OnCancelAction(EWeaponActionType p_type)
        {
            _actionFlow.CancelAction(p_type);
            OnActionStopped?.Invoke(p_type);
        }

        public bool TrySwitchTriggerMode()
        {
            return _actionFlow.TrySwitchTriggerMode(
                HasUseContext,
                HasActiveAction);
        }

        // Secondary는 Camera View와 독립적으로 유지되는 조준·차징 상태다.
        public bool BeginSecondary()
        {
            return _actionFlow.BeginSecondary(
                HasUseContext,
                HasSecondaryAction,
                HasActiveAction);
        }

        public void TickSecondary(float p_deltaTime)
        {
            _actionFlow.TickSecondary(p_deltaTime);
        }

        public void CancelSecondary()
        {
            _actionFlow.CancelSecondary();
        }

        private void PublishFired(RangeAttackRequest p_request)
        {
            OnFired?.Invoke(p_request);
        }

        private void PublishTrajectory(RangeAttackResult p_result)
        {
            if (p_result.HasVisiblePath)
                OnTrajectoryResolved?.Invoke(p_result);
        }

        private void PublishHit(RangeHitResult p_result)
        {
            OnHitResolved?.Invoke(p_result);
        }

        private void PublishProjectile(ProjectileEntity p_projectile)
        {
            if (p_projectile != null)
                OnProjectileLaunched?.Invoke(p_projectile);
        }

        private void OnValidate()
        {
            _settings ??= new RangeWeaponSettings();
            _settings.Validate();
            _attackSettings ??= new RangeWeaponAttackSettings();
            _attackSettings.Validate();
        }

        // 설정을 소유한 RangeWeapon에서 개발용 최대 사거리를 직접 표시한다.
        private void OnDrawGizmosSelected()
        {
            if (!_showRange || MaxDistance <= 0f)
                return;

            Transform origin = _muzzle != null ? _muzzle : transform;
            Vector3 endPoint =
                origin.position + origin.forward * MaxDistance;
            Color previousColor = Gizmos.color;

            Gizmos.color = _rangeColor;
            Gizmos.DrawLine(origin.position, endPoint);
            Gizmos.DrawWireSphere(endPoint, 0.1f);
            Gizmos.color = previousColor;
        }

        private void OnDestroy()
        {
            UnbindUseContext();
            _attackModule.Unbind();
        }
    }
}
