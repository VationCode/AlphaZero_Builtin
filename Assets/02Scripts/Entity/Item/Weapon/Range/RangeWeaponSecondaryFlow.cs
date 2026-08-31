using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // RangeWeapon의 Secondary 활성 상태와 차징 시간을 관리한다.
    internal sealed class RangeWeaponSecondaryFlow
    {
        private RangeSecondarySettings _settings;
        private float _chargeElapsedTime;

        public bool IsActive { get; private set; }
        public bool IsChargeEnabled =>
            _settings?.Charge?.Enabled == true;
        public bool IsAimViewActive =>
            IsActive &&
            _settings?.CameraView == ERangeAimView.Aim;
        public float ChargeRatio => IsChargeEnabled
            ? Mathf.Clamp01(
                _chargeElapsedTime / _settings.Charge.MaxDuration)
            : 0f;

        public void Bind(RangeSecondarySettings p_settings)
        {
            _settings = p_settings;
            Reset();
        }

        public void Reset()
        {
            IsActive = false;
            _chargeElapsedTime = 0f;
        }

        public bool Begin(
            bool p_hasSecondaryAction,
            bool p_hasActiveWeaponAction)
        {
            if (!p_hasSecondaryAction ||
                IsActive ||
                (IsChargeEnabled && p_hasActiveWeaponAction))
            {
                return false;
            }

            IsActive = true;
            _chargeElapsedTime = 0f;
            return true;
        }

        public void Tick(float p_deltaTime)
        {
            if (!IsActive || !IsChargeEnabled)
                return;

            _chargeElapsedTime = Mathf.Min(
                _chargeElapsedTime + Mathf.Max(0f, p_deltaTime),
                _settings.Charge.MaxDuration);
        }

        public float CalculateBonusDamage()
        {
            return IsChargeEnabled
                ? _settings.Charge.CalculateBonusDamage(ChargeRatio)
                : 0f;
        }

        public void ResetChargeAfterFire()
        {
            _chargeElapsedTime = 0f;
        }

        public void Cancel()
        {
            Reset();
        }
    }
}
