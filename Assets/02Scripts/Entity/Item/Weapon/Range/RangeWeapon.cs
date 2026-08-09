using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // Range Prefab이 우클릭으로 실행할 보조 행동 종류다.
    public enum ERangeSecondaryType
    {
        Aiming,
        Charging
    }

    // Range Prefab이 Secondary 중 요청할 Camera View 표현이다.
    public enum ERangeSecondaryView
    {
        KeepCurrent,
        Aim,
        Scope
    }

    // 원거리 무기의 공통 연속 발사와 조준 입력 생명주기를 담당한다.
    public abstract class RangeWeapon : Weapon
    {
        // 발사 성공 후 Muzzle과 Tracer View가 구독하는 표현 이벤트다.
        public event Action<RangeAttackRequest, RangeAttackResult> OnFired;

        [Header("Attack")]
        [SerializeField]
        private Transform _muzzle;

        [SerializeField]
        private RangeAttackModule _attackModule;

        [Header("Hand IK")]
        [SerializeField]
        private Transform _leftHandIKTarget;

        [Header("Attack Tuning")]
        [SerializeField, Min(0f)]
        private float _baseDamage = 15f;

        [Tooltip("발사 사이의 시간(초)입니다. 초당 3발은 약 0.333초입니다.")]
        [SerializeField, Min(0.01f)]
        private float _fireInterval = 0.2f;

        [Tooltip("사격 종료 후 마지막 발사 방향을 유지할 시간(초)입니다.")]
        [SerializeField, Min(0f)]
        private float _postAttackFacingDuration = 0.25f;

        [SerializeField, Min(0.01f)]
        private float _maxDistance = 100f;

        [Header("Action")]
        [SerializeField]
        private EWeaponInputMode _primaryInputMode = EWeaponInputMode.Auto;

        [Header("Secondary")]
        [SerializeField]
        private ERangeSecondaryType _secondaryType = ERangeSecondaryType.Aiming;

        [SerializeField]
        private ERangeSecondaryView _secondaryView = ERangeSecondaryView.KeepCurrent;

        [SerializeField, Min(0.01f)]
        private float _maxChargeDuration = 1f;

        private IRangeAttackSource _attackSource;

        private float _nextFireTime;
        private float _chargeElapsedTime;

        public ERangeSecondaryType SecondaryType => _secondaryType;
        public ERangeSecondaryView SecondaryView => _secondaryView;
        public Transform LeftHandIKTarget => _leftHandIKTarget;
        public float PostAttackFacingDuration => _postAttackFacingDuration;
        public bool DidFireDuringPrimaryAction { get; private set; }
        public Vector3 LastFireDirection { get; private set; }
        public bool IsSecondaryActive { get; private set; }
        private bool IsFireReady => Time.time >= _nextFireTime;
        public float ChargeRatio => _secondaryType == ERangeSecondaryType.Charging
            ? Mathf.Clamp01(_chargeElapsedTime / Mathf.Max(0.01f, _maxChargeDuration))
            : 0f;

        protected sealed override bool CanInitialize(WeaponDTO p_data)
        {
            return p_data is RangeWeaponDTO;
        }

        protected override void OnInitialized()
        {
            _muzzle ??= transform;
            _attackModule ??= GetComponent<RangeAttackModule>();

            if (_attackModule == null)
            {
                Debug.LogError($"{nameof(RangeAttackModule)}이 설정되지 않았습니다.", this);
            }
        }

        // 실제 발사와 상체 조준 표현이 같은 총구 기준 방향을 사용한다.
        public bool TryGetAimDirection(out Vector3 p_direction)
        {
            return TryResolveAttack(
                out _,
                out _,
                out p_direction);
        }

        // 무기를 사용하는 Entity로부터 공격 출처를 전달받는다.
        public bool BindAttackSource(IRangeAttackSource p_attackSource)
        {
            if (!IsInitialized ||
                p_attackSource == null ||
                _attackModule == null)
            {
                return false;
            }

            _attackSource = p_attackSource;
            return true;
        }

        // 공통 WeaponAction에서는 좌클릭 발사만 시작한다.
        protected override bool OnBeginAction(EWeaponActionType p_type)
        {
            if (p_type != EWeaponActionType.Primary)
                return false;

            DidFireDuringPrimaryAction = false;

            // Auto는 제한 시간 중 눌러도 행동을 유지하고, 시간이 되면 Tick에서 발사한다.
            if (!IsFireReady)
                return _primaryInputMode == EWeaponInputMode.Auto;

            if (!TryFireByRate())
                return false;

            DidFireDuringPrimaryAction = true;
            return true;
        }

        // 좌클릭을 유지하는 동안 Inspector의 Fire Interval 간격으로 발사한다.
        protected override void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (p_type != EWeaponActionType.Primary)
                return;

            // Semi는 시작할 때 한 발만 발사하고 행동을 종료한다.
            if (_primaryInputMode == EWeaponInputMode.Semi)
            {
                EndAction();
                return;
            }

            if (!IsFireReady)
                return;

            if (!TryFireByRate())
            {
                EndAction();
                return;
            }

            DidFireDuringPrimaryAction = true;
        }

        // 입력 생명주기와 무관하게 마지막 발사 시각을 기준으로 간격을 제한한다.
        private bool TryFireByRate()
        {
            if (!IsFireReady || !TryFire())
                return false;

            _nextFireTime =
                Time.time +
                Mathf.Max(0.01f, _fireInterval);

            return true;
        }

        // 현재 조준 정보로 공격 요청을 생성하고 구체 공격 Module에 전달한다.
        private bool TryFire()
        {
            if (_attackModule == null ||
                !TryResolveAttack(
                    out Vector3 attackOrigin,
                    out Vector3 targetPoint,
                    out Vector3 attackDirection))
            {
                return false;
            }

            RangeAttackRequest request = new(
                _attackSource.Attacker,
                _muzzle.position,
                attackOrigin,
                targetPoint,
                attackDirection,
                _baseDamage,
                _maxDistance);

            if (!_attackModule.TryExecute(
                    request,
                    out RangeAttackResult result))
            {
                return false;
            }

            LastFireDirection =
                request.Direction;

            OnFired?.Invoke(
                request,
                result);

            return true;
        }

        // Camera 목표점과 공격 방식별 최종 발사 방향을 한 번에 계산한다.
        private bool TryResolveAttack(
            out Vector3 p_attackOrigin,
            out Vector3 p_targetPoint,
            out Vector3 p_direction)
        {
            p_attackOrigin = Vector3.zero;
            p_targetPoint = Vector3.zero;
            p_direction = Vector3.zero;

            if (_attackSource == null ||
                _muzzle == null ||
                _attackModule == null)
            {
                return false;
            }

            float defaultAimDistance =
                _attackModule.GetDefaultAimDistance(
                    _maxDistance);

            return _attackSource.TryGetAttackPose(
                       _muzzle.position,
                       _maxDistance,
                       defaultAimDistance,
                       out p_attackOrigin,
                       out p_targetPoint) &&
                   _attackModule.TryResolveLaunchDirection(
                       p_attackOrigin,
                       p_targetPoint,
                       out p_direction);
        }

        // Range Secondary는 Primary와 동시에 유지할 수 있도록 별도 생명주기로 시작한다.
        public bool BeginSecondary()
        {
            if (!IsInitialized || IsSecondaryActive)
                return false;

            IsSecondaryActive = true;
            _chargeElapsedTime = 0f;

            OnAimChanged(true);

            // Aiming은 공통 동작이며 Type은 추가 행동인 Charging만 구분한다.
            if (_secondaryType == ERangeSecondaryType.Charging)
                OnChargingChanged(true);

            return true;
        }

        // Charging 타입만 우클릭 유지 시간을 충전 비율로 누적한다.
        public void TickSecondary(float p_deltaTime)
        {
            if (!IsSecondaryActive ||
                _secondaryType != ERangeSecondaryType.Charging)
            {
                return;
            }

            _chargeElapsedTime = Mathf.Min(
                _chargeElapsedTime + Mathf.Max(0f, p_deltaTime),
                Mathf.Max(0.01f, _maxChargeDuration));

            OnChargingTick(ChargeRatio);
        }

        // 정상적인 우클릭 해제는 Charging 결과까지 실행한다.
        public void EndSecondary()
        {
            FinishSecondary(false);
        }

        // 무기 교체나 전투 차단에서는 충전 결과 없이 상태만 정리한다.
        public void CancelSecondary()
        {
            FinishSecondary(true);
        }

        private void FinishSecondary(bool p_isCanceled)
        {
            if (!IsSecondaryActive)
                return;

            float chargeRatio = ChargeRatio;

            if (_secondaryType == ERangeSecondaryType.Charging)
            {
                OnChargingChanged(false);

                if (!p_isCanceled)
                    OnChargeReleased(chargeRatio);
            }

            OnAimChanged(false);

            IsSecondaryActive = false;
            _chargeElapsedTime = 0f;
        }

        // Rifle과 Sniper가 무기 자체의 조준 표현을 확장할 수 있다.
        protected virtual void OnAimChanged(bool p_isAiming) { }

        protected virtual void OnChargingChanged(bool p_isCharging) { }
        protected virtual void OnChargingTick(float p_chargeRatio) { }

        // 기본 Charging은 해제 시 한 발을 발사하며 구체 무기는 비율 사용을 확장한다.
        protected virtual void OnChargeReleased(float p_chargeRatio)
        {
            TryFireByRate();
        }
    }
}
