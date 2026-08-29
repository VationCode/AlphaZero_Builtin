using Alpha.Item.Weapon;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 원거리 입력을 해석해 발사 시점과 Secondary 상태를 결정한다.
    public sealed class RangeWeaponActionFlow
    {
        private RangeAttackModule _attackModule;
        private RangeWeaponSettings _settings;

        private float _nextFireTime;
        private float _chargeElapsedTime;
        private bool _isChargedPrimaryAction;

        public ERangeTriggerMode CurrentTriggerMode { get; private set; }
        public bool DidFireDuringPrimaryAction { get; private set; }
        public bool IsSecondaryActive { get; private set; }

        public bool CanSwitchTriggerMode =>
            _attackModule != null &&
            _attackModule.IsInitialized &&
            !_attackModule.HasActiveAction;

        public float ChargeRatio => IsChargeEnabled
            ? Mathf.Clamp01(
                _chargeElapsedTime / _settings.ChargeSettings.MaxDuration)
            : 0f;

        private bool IsChargeEnabled =>
            _settings?.ChargeSettings?.Enabled == true;
        private bool IsFireReady => Time.time >= _nextFireTime;

        public void Bind(
            RangeAttackModule p_attackModule,
            RangeWeaponSettings p_settings)
        {
            _attackModule = p_attackModule;
            _settings = p_settings;
            Reset();
        }

        public void Reset()
        {
            _nextFireTime = 0f;
            _chargeElapsedTime = 0f;
            _isChargedPrimaryAction = false;
            DidFireDuringPrimaryAction = false;
            IsSecondaryActive = false;
            CurrentTriggerMode = _settings?.DefaultTriggerMode ??
                                 ERangeTriggerMode.Auto;
        }

        public bool TrySwitchTriggerMode()
        {
            if (!CanSwitchTriggerMode)
                return false;

            CurrentTriggerMode =
                CurrentTriggerMode == ERangeTriggerMode.Semi
                    ? ERangeTriggerMode.Auto
                    : ERangeTriggerMode.Semi;

            return true;
        }

        // Primary 시작 시 일반 사격과 우클릭 차징 사격을 구분한다.
        public bool TryBeginAction(EWeaponActionType p_type)
        {
            if (p_type != EWeaponActionType.Primary)
                return false;

            DidFireDuringPrimaryAction = false;
            _isChargedPrimaryAction =
                IsChargeEnabled && IsSecondaryActive;

            // Primary 시작 시 저장한 우클릭 상태로 일반 사격과 차징 사격을 분리한다.
            if (_isChargedPrimaryAction)
            {
                // Auto는 쿨다운 중 입력을 유지하고 준비되는 즉시 발사한다.
                if (!IsFireReady)
                    return CurrentTriggerMode == ERangeTriggerMode.Auto;

                return TryFireChargedPrimary();
            }

            if (!IsFireReady)
                return CurrentTriggerMode == ERangeTriggerMode.Auto;

            if (!TryFireByRate())
                return false;

            DidFireDuringPrimaryAction = true;
            return true;
        }

        // Auto 입력 유지 중 Fire Interval마다 대표 공격 Module을 실행한다.
        public void TickAction(EWeaponActionType p_type)
        {
            if (p_type != EWeaponActionType.Primary)
                return;

            // 시작할 때 차징으로 확정된 Primary만 Secondary 상태와 Charge 피해를 사용한다.
            if (_isChargedPrimaryAction)
            {
                if (!IsSecondaryActive ||
                    CurrentTriggerMode == ERangeTriggerMode.Semi)
                {
                    _attackModule.EndAction();
                    return;
                }

                if (!IsFireReady)
                    return;

                if (!TryFireChargedPrimary())
                    _attackModule.EndAction();

                return;
            }

            if (CurrentTriggerMode == ERangeTriggerMode.Semi)
            {
                _attackModule.EndAction();
                return;
            }

            if (!IsFireReady)
                return;

            if (!TryFireByRate())
            {
                _attackModule.EndAction();
                return;
            }

            DidFireDuringPrimaryAction = true;
        }

        public bool BeginSecondary()
        {
            if (_attackModule == null ||
                !_attackModule.IsInitialized ||
                IsSecondaryActive ||
                (IsChargeEnabled && _attackModule.HasActiveAction) ||
                !_attackModule.HasSecondaryAction)
            {
                return false;
            }

            IsSecondaryActive = true;
            _chargeElapsedTime = 0f;
            return true;
        }

        public void TickSecondary(float p_deltaTime)
        {
            if (!IsSecondaryActive || !IsChargeEnabled)
                return;

            _chargeElapsedTime = Mathf.Min(
                _chargeElapsedTime + Mathf.Max(0f, p_deltaTime),
                _settings.ChargeSettings.MaxDuration);
        }

        public void CancelSecondary()
        {
            IsSecondaryActive = false;
            _chargeElapsedTime = 0f;
        }

        // 현재 차징 비율을 피해에 반영하고 성공한 경우 다음 차징을 시작한다.
        private bool TryFireChargedPrimary()
        {
            if (!IsSecondaryActive || !IsFireReady)
                return false;

            float bonusDamage =
                _settings.ChargeSettings.CalculateBonusDamage(
                    ChargeRatio);

            if (!TryFireByRate(bonusDamage))
                return false;

            _chargeElapsedTime = 0f;
            DidFireDuringPrimaryAction = true;
            return true;
        }

        private bool TryFireByRate(float p_bonusDamage = 0f)
        {
            if (!IsFireReady ||
                _attackModule == null ||
                !_attackModule.TryFire(p_bonusDamage))
            {
                return false;
            }

            _nextFireTime =
                Time.time + _settings.AttackTuning.FireInterval;
            return true;
        }
    }
}
