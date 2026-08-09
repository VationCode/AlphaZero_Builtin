using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // Range Prefab이 우클릭으로 실행할 보조 행동 종류다.
    public enum ERangeSecondaryType
    {
        Aiming,
        Zoom,
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
        [Header("Attack")]
        [SerializeField]
        private Transform _muzzle;

        [SerializeField]
        private RangeAttackModule _attackModule;

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

        private float _fireCooldown;
        private float _chargeElapsedTime;

        public ERangeSecondaryType SecondaryType => _secondaryType;
        public ERangeSecondaryView SecondaryView => _secondaryView;
        public bool IsSecondaryActive { get; private set; }
        public float ChargeRatio => _secondaryType == ERangeSecondaryType.Charging
            ? Mathf.Clamp01(_chargeElapsedTime / Mathf.Max(0.01f, _maxChargeDuration))
            : 0f;

        protected RangeWeaponDTO RangeData => Data as RangeWeaponDTO;

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
            if (p_type != EWeaponActionType.Primary || RangeData == null)
                return false;

            if (!TryFire())
                return false;

            _fireCooldown = Mathf.Max(0.01f, RangeData.Rate);
            return true;
        }

        // 좌클릭을 유지하는 동안 DTO의 Rate 간격으로 발사한다.
        protected override void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (p_type != EWeaponActionType.Primary || RangeData == null)
                return;

            // Semi는 시작할 때 한 발만 발사하고 행동을 종료한다.
            if (_primaryInputMode == EWeaponInputMode.Semi)
            {
                EndAction();
                return;
            }

            _fireCooldown -= p_deltaTime;

            if (_fireCooldown > 0f)
                return;

            if (!TryFire())
            {
                EndAction();
                return;
            }

            _fireCooldown = Mathf.Max(0.01f, RangeData.Rate);
        }

        protected override void OnEndAction(EWeaponActionType p_type)
        {
            ResetAction(p_type);
        }

        protected override void OnCancelAction(EWeaponActionType p_type)
        {
            ResetAction(p_type);
        }

        // 현재 조준 정보로 공격 요청을 생성하고 구체 공격 Module에 전달한다.
        private bool TryFire()
        {
            if (_attackSource == null ||
                _attackModule == null ||
                _muzzle == null ||
                RangeData == null)
            {
                return false;
            }

            if (!_attackSource.TryGetAttackDirection(
                    _muzzle.position,
                    RangeData.MaxDistance,
                    out Vector3 attackDirection))
            {
                return false;
            }

            RangeAttackRequest request = new(
                _attackSource.Attacker,
                _muzzle.position,
                attackDirection,
                RangeData.BaseDamage,
                RangeData.MaxDistance);

            return _attackModule.TryExecute(request);
        }

        // Range Secondary는 Primary와 동시에 유지할 수 있도록 별도 생명주기로 시작한다.
        public bool BeginSecondary()
        {
            if (!IsInitialized || IsSecondaryActive)
                return false;

            IsSecondaryActive = true;
            _chargeElapsedTime = 0f;

            OnAimChanged(true);

            switch (_secondaryType)
            {
                case ERangeSecondaryType.Aiming:
                    break;

                case ERangeSecondaryType.Zoom:
                    OnZoomChanged(true);
                    break;

                case ERangeSecondaryType.Charging:
                    OnChargingChanged(true);
                    break;
            }

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

            switch (_secondaryType)
            {
                case ERangeSecondaryType.Zoom:
                    OnZoomChanged(false);
                    break;

                case ERangeSecondaryType.Charging:
                    OnChargingChanged(false);

                    if (!p_isCanceled)
                        OnChargeReleased(chargeRatio);
                    break;
            }

            OnAimChanged(false);

            IsSecondaryActive = false;
            _chargeElapsedTime = 0f;
        }

        // Rifle과 Sniper가 무기 자체의 조준 표현을 확장할 수 있다.
        protected virtual void OnAimChanged(bool p_isAiming) { }

        protected virtual void OnZoomChanged(bool p_isZooming) { }
        protected virtual void OnChargingChanged(bool p_isCharging) { }
        protected virtual void OnChargingTick(float p_chargeRatio) { }

        // 기본 Charging은 해제 시 한 발을 발사하며 구체 무기는 비율 사용을 확장한다.
        protected virtual void OnChargeReleased(float p_chargeRatio)
        {
            TryFire();
        }

        private void ResetAction(EWeaponActionType p_type)
        {
            if (p_type == EWeaponActionType.Primary)
                _fireCooldown = 0f;
        }
    }
}
