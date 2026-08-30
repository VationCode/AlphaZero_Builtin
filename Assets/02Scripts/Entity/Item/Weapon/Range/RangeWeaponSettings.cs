using Alpha.Combat;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Item.Weapon.Range
{
    // Range Primary가 발사 요청을 생성하는 입력 방식을 구분한다.
    public enum ERangeTriggerMode
    {
        Semi = 0,
        Auto = 1
    }

    // Range Prefab이 Secondary 중 요청할 Camera View 표현이다.
    public enum ERangeAimView
    {
        None = 0,
        Aim = 1,
        Scope = 2
    }

    // Range Prefab에서 선택할 수 있는 원거리 무기 종류다.
    public enum ERangeWeaponType
    {
        None = (int)EWeaponType.None,
        Rifle = (int)EWeaponType.Rifle,
        SniperRifle = (int)EWeaponType.SniperRifle,
        PenetrationRifle = (int)EWeaponType.PenetrationRifle,
        Shotgun = (int)EWeaponType.Shotgun,
        GrenadeLauncher = (int)EWeaponType.GrenadeLauncher
    }

    // 원거리 공격의 구체적인 전달 방식을 구분한다.
    public enum ERangeAttackType
    {
        None,
        Hitscan,
        Penetration,
        Projectile
    }

    // 조준 View와 독립적으로 우클릭 차징 여부와 시간을 보관한다.
    [Serializable]
    public sealed class RangeChargeSettings
    {
        [SerializeField]
        private bool _enabled;

        [SerializeField, Min(0.01f)]
        private float _maxDuration = 1f;

        [Tooltip("최대 차징 시 기본 공격력에 더할 고정 피해입니다.")]
        [SerializeField, Min(0f)]
        private float _maxBonusDamage;

        public bool Enabled => _enabled;
        public float MaxDuration => Mathf.Max(0.01f, _maxDuration);

        public float CalculateBonusDamage(float p_chargeRatio)
        {
            if (!_enabled)
                return 0f;

            return Mathf.Lerp(
                0f,
                _maxBonusDamage,
                Mathf.Clamp01(p_chargeRatio));
        }

        public void Validate()
        {
            _maxDuration = Mathf.Max(0.01f, _maxDuration);
            _maxBonusDamage = Mathf.Max(0f, _maxBonusDamage);
        }
    }

    // 공격 전달 방식과 무관한 RangeWeapon의 기본 설정만 보관한다.
    [Serializable]
    public sealed class RangeWeaponSettings
    {
        [Header("Identity")]
        [SerializeField]
        private ERangeWeaponType _weaponType = ERangeWeaponType.None;

        [Header("Attack")]
        [SerializeField, Min(0f)]
        private float _baseDamage = 15f;

        [SerializeField, Min(0.01f)]
        private float _maxDistance = 100f;

        [FormerlySerializedAs("_attackTuning")]
        [SerializeField]
        private RangeShotSettings _shotSettings = new();

        [FormerlySerializedAs("_handlingSettings")]
        [SerializeField]
        private RangeFireResponseSettings _fireResponseSettings = new();

        [Header("Action")]
        [FormerlySerializedAs("_primaryInputMode")]
        [FormerlySerializedAs("_triggerMode")]
        [SerializeField]
        private ERangeTriggerMode _defaultTriggerMode =
            ERangeTriggerMode.Auto;

        [Header("Secondary")]
        [FormerlySerializedAs("_secondaryView")]
        [SerializeField]
        private ERangeAimView _aimView = ERangeAimView.None;

        [SerializeField]
        private RangeChargeSettings _chargeSettings = new();

        [Header("Impact")]
        [SerializeField]
        private AttackImpactSettings _impactSettings = new();

        public ERangeWeaponType WeaponType => _weaponType;
        public float BaseDamage => Mathf.Max(0f, _baseDamage);
        public float MaxDistance => Mathf.Max(0.01f, _maxDistance);
        public RangeShotSettings ShotSettings => _shotSettings;
        public RangeFireResponseSettings FireResponseSettings =>
            _fireResponseSettings;
        public ERangeTriggerMode DefaultTriggerMode => _defaultTriggerMode;
        public ERangeAimView AimView => _aimView;
        public RangeChargeSettings ChargeSettings => _chargeSettings;
        public AttackImpactSettings ImpactSettings => _impactSettings;

        public void Validate()
        {
            _baseDamage = Mathf.Max(0f, _baseDamage);
            _maxDistance = Mathf.Max(0.01f, _maxDistance);

            _shotSettings ??= new RangeShotSettings();
            _shotSettings.Validate();

            _fireResponseSettings ??= new RangeFireResponseSettings();
            _fireResponseSettings.Validate();

            _chargeSettings ??= new RangeChargeSettings();
            _chargeSettings.Validate();

            _impactSettings ??= new AttackImpactSettings();
            _impactSettings.Validate();
        }
    }
}
