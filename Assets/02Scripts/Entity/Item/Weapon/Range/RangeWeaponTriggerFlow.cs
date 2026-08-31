using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // RangeWeapon의 발사 모드와 다음 발사 가능 시점을 관리한다.
    internal sealed class RangeWeaponTriggerFlow
    {
        private RangeWeaponSettings _settings;
        private float _nextFireTime;

        public ERangeTriggerMode CurrentMode { get; private set; }
        public bool IsFireReady => Time.time >= _nextFireTime;

        public void Bind(RangeWeaponSettings p_settings)
        {
            _settings = p_settings;
            Reset();
        }

        public void Reset()
        {
            _nextFireTime = 0f;
            CurrentMode = _settings?.DefaultTriggerMode ??
                          ERangeTriggerMode.Auto;
        }

        public bool TrySwitchMode(bool p_canSwitch)
        {
            if (!p_canSwitch)
                return false;

            CurrentMode =
                CurrentMode == ERangeTriggerMode.Semi
                    ? ERangeTriggerMode.Auto
                    : ERangeTriggerMode.Semi;

            return true;
        }

        public bool TryFire(
            float p_bonusDamage,
            bool p_isAimViewActive,
            Func<float, bool, bool> p_fire)
        {
            if (!IsFireReady ||
                p_fire == null ||
                !p_fire(p_bonusDamage, p_isAimViewActive))
            {
                return false;
            }

            _nextFireTime =
                Time.time + _settings.ShotSettings.FireInterval;
            return true;
        }
    }
}
