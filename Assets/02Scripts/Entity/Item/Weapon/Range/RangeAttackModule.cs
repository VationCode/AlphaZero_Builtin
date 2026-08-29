using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 원거리 무기의 대표 진입점으로 Flow와 구체 공격 실행체를 조립한다.
    [DisallowMultipleComponent]
    public sealed class RangeAttackModule : Weapon
    {
        // 발사 성공 후 Muzzle·Audio View가 구독하는 표현 이벤트다.
        public event Action<RangeAttackRequest> OnFired;

        // 판정 Module이 계산한 실제 경로만 발행하고 표현은 View에 맡긴다.
        public event Action<RangeAttackResult> OnTrajectoryResolved;

        [SerializeField]
        private RangeWeaponSettings _settings = new();

        [Header("Attack Dependencies")]
        [SerializeField]
        private Transform _muzzle;

        [Header("Hand IK")]
        [SerializeField]
        private Transform _leftHandIKTarget;

        private readonly RangeWeaponActionFlow _actionFlow = new();
        private readonly HitscanAttackModule _hitscanAttackModule = new();
        private readonly PenetrationAttackModule _penetrationAttackModule = new();
        private readonly ProjectileAttackModule _projectileAttackModule = new();

        private IRangeAttackSource _attackSource;

        public RangeWeaponSettings Settings => _settings;
        public sealed override EWeaponType WeaponType =>
            _settings != null
                ? (EWeaponType)_settings.WeaponType
                : EWeaponType.None;
        public ERangeAttackType AttackType =>
            _settings?.AttackType ?? ERangeAttackType.None;
        public ERangeAimView AimView =>
            _settings?.AimView ?? ERangeAimView.None;
        public RangeAttackTuning AttackTuning =>
            _settings?.AttackTuning;
        public float MaxDistance =>
            _settings?.MaxDistance ?? 0.01f;
        public ERangeTriggerMode CurrentTriggerMode =>
            _actionFlow.CurrentTriggerMode;
        public bool IsChargeEnabled =>
            _settings?.ChargeSettings?.Enabled == true;
        public bool HasSecondaryAction =>
            AimView != ERangeAimView.None || IsChargeEnabled;
        public Transform LeftHandIKTarget => _leftHandIKTarget;
        public bool DidFireDuringPrimaryAction =>
            _actionFlow.DidFireDuringPrimaryAction;
        public Vector3 LastFireDirection { get; private set; }
        public bool IsSecondaryActive =>
            _actionFlow.IsSecondaryActive;
        public bool CanSwitchTriggerMode =>
            _actionFlow.CanSwitchTriggerMode;
        public float ChargeRatio => _actionFlow.ChargeRatio;
        public float StartRadius =>
            _settings?.Penetration?.StartRadius ?? 0.01f;
        public float EndRadius =>
            _settings?.Penetration?.EndRadius ?? 0.01f;

        protected sealed override bool CanInitialize(WeaponDTO p_data)
        {
            return p_data?.WeaponCategory == EWeaponCategory.Range;
        }

        protected override void OnInitialized()
        {
            _settings ??= new RangeWeaponSettings();
            _settings.Validate();
            _muzzle ??= transform;
            _actionFlow.Bind(this, _settings);

            if (AttackType == ERangeAttackType.None)
            {
                Debug.LogError(
                    "원거리 무기 종류에 대응하는 공격 방식이 없습니다.",
                    this);
            }
        }

        // 무기를 사용하는 Entity로부터 공격자와 조준 계산을 전달받는다.
        public bool BindAttackSource(IRangeAttackSource p_attackSource)
        {
            if (!IsInitialized ||
                p_attackSource == null ||
                AttackType == ERangeAttackType.None)
            {
                return false;
            }

            _attackSource = p_attackSource;
            return true;
        }

        public bool TrySwitchTriggerMode()
        {
            return _actionFlow.TrySwitchTriggerMode();
        }

        protected override bool OnBeginAction(EWeaponActionType p_type)
        {
            return _actionFlow.TryBeginAction(p_type);
        }

        protected override void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            _actionFlow.TickAction(p_type);
        }

        public bool BeginSecondary()
        {
            return _actionFlow.BeginSecondary();
        }

        public void TickSecondary(float p_deltaTime)
        {
            _actionFlow.TickSecondary(p_deltaTime);
        }

        public void CancelSecondary()
        {
            _actionFlow.CancelSecondary();
        }

        public bool TryGetAimDirection(out Vector3 p_direction)
        {
            return TryGetAttackPose(out _, out p_direction);
        }

        // 미리보기와 실제 공격이 같은 시작점과 방향을 사용한다.
        public bool TryGetAttackPose(
            out Vector3 p_origin,
            out Vector3 p_direction)
        {
            p_origin = Vector3.zero;
            p_direction = Vector3.zero;

            if (_attackSource == null || _muzzle == null)
                return false;

            if (!_attackSource.TryGetAttackPose(
                    _muzzle.position,
                    MaxDistance,
                    out p_origin,
                    out Vector3 targetPoint))
            {
                return false;
            }

            Vector3 targetDirection = targetPoint - p_origin;

            if (targetDirection.sqrMagnitude <= 0.0001f)
                return false;

            p_direction = targetDirection.normalized;
            return true;
        }

        // Flow가 정한 시점에 설정된 공격 방식으로 한 번의 공격을 실행한다.
        internal bool TryFire(float p_bonusDamage)
        {
            if (_attackSource == null ||
                _muzzle == null ||
                _settings == null ||
                !TryGetAttackPose(
                    out Vector3 attackOrigin,
                    out Vector3 attackDirection))
            {
                return false;
            }

            RangeAttackRequest request = new(
                _attackSource.Attacker,
                _muzzle.position,
                attackOrigin,
                attackDirection,
                _attackSource.ResolveDamage(
                    _settings.BaseDamage + Mathf.Max(0f, p_bonusDamage)),
                _settings.MaxDistance,
                _settings.ImpactSettings.CreateInfo(),
                _settings.AttackTuning.SpreadAngle,
                _settings.AttackTuning.ProjectilesPerShot);

            if (!TryExecute(request))
                return false;

            LastFireDirection = request.Direction;
            OnFired?.Invoke(request);
            return true;
        }

        private bool TryExecute(in RangeAttackRequest p_request)
        {
            if (!p_request.IsValid)
                return false;

            return AttackType switch
            {
                ERangeAttackType.Hitscan =>
                    _hitscanAttackModule.Execute(
                        p_request,
                        _settings.Physics,
                        PublishTrajectory),
                ERangeAttackType.Penetration =>
                    _penetrationAttackModule.Execute(
                        p_request,
                        _settings.Physics,
                        _settings.Penetration,
                        PublishTrajectory),
                ERangeAttackType.Projectile =>
                    _projectileAttackModule.Execute(
                        p_request,
                        _settings.Projectile),
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
            if (AttackType != ERangeAttackType.Projectile)
            {
                p_result = default;
                return false;
            }

            return _projectileAttackModule.TryPredictTrajectory(
                _settings.Projectile,
                p_origin,
                p_direction,
                MaxDistance,
                p_simulationStep,
                p_points,
                out p_result);
        }

        public bool TryGetProjectileRadialDamageRadius(
            out float p_radius)
        {
            if (AttackType != ERangeAttackType.Projectile)
            {
                p_radius = 0f;
                return false;
            }

            return _projectileAttackModule.TryGetRadialDamageRadius(
                _settings.Projectile,
                out p_radius);
        }

        private void PublishTrajectory(RangeAttackResult p_result)
        {
            if (p_result.HasVisiblePath)
                OnTrajectoryResolved?.Invoke(p_result);
        }

        // 모든 세부 공격이 동일한 원형 분산 계산을 사용한다.
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

        private void OnValidate()
        {
            _settings ??= new RangeWeaponSettings();
            _settings.Validate();
        }
    }
}
