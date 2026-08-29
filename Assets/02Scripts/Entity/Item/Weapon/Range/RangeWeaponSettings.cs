using System;
using Alpha.Combat;
using Alpha.Projectile;
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

    // 무기 종류로부터 결정되는 실제 공격 전달 방식이다.
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

    // 관통 공격이 투영할 시작·종료 반경만 보관한다.
    [Serializable]
    public sealed class PenetrationAttackSettings
    {
        [Tooltip("관통 영역이 발사 지점에서 가질 반경입니다.")]
        [SerializeField, Min(0.01f)]
        private float _startRadius = 0.25f;

        [Tooltip("관통 영역이 최대 거리 지점에서 가질 반경입니다.")]
        [SerializeField, Min(0.01f)]
        private float _endRadius = 0.25f;

        public float StartRadius => Mathf.Max(0.01f, _startRadius);
        public float EndRadius => Mathf.Max(0.01f, _endRadius);

        public void Validate()
        {
            _startRadius = Mathf.Max(0.01f, _startRadius);
            _endRadius = Mathf.Max(0.01f, _endRadius);
        }
    }

    // 원거리 무기의 공통 정보와 공격 방식별 설정만 보관하는 Domain 데이터다.
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

        [SerializeField]
        private RangeAttackTuning _attackTuning = new();

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

        [Header("Physics Attack")]
        [SerializeField]
        private PhysicsRangeAttackSettings _physics = new();

        [Header("Penetration Volume")]
        [SerializeField]
        private PenetrationAttackSettings _penetration = new();

        [Header("Projectile Launch")]
        [SerializeField]
        private ProjectileLaunchSettings _projectile = new(
            null,
            120f,
            (LayerMask)129);

        public ERangeWeaponType WeaponType => _weaponType;
        public ERangeAttackType AttackType => ResolveAttackType(_weaponType);
        public float BaseDamage => Mathf.Max(0f, _baseDamage);
        public float MaxDistance => Mathf.Max(0.01f, _maxDistance);
        public RangeAttackTuning AttackTuning => _attackTuning;
        public ERangeTriggerMode DefaultTriggerMode => _defaultTriggerMode;
        public ERangeAimView AimView => _aimView;
        public RangeChargeSettings ChargeSettings => _chargeSettings;
        public AttackImpactSettings ImpactSettings => _impactSettings;
        public PhysicsRangeAttackSettings Physics => _physics;
        public PenetrationAttackSettings Penetration => _penetration;
        public ProjectileLaunchSettings Projectile => _projectile;

        public static ERangeAttackType ResolveAttackType(
            ERangeWeaponType p_weaponType)
        {
            return p_weaponType switch
            {
                ERangeWeaponType.Rifle => ERangeAttackType.Hitscan,
                ERangeWeaponType.SniperRifle => ERangeAttackType.Hitscan,
                ERangeWeaponType.Shotgun => ERangeAttackType.Hitscan,
                ERangeWeaponType.PenetrationRifle =>
                    ERangeAttackType.Penetration,
                ERangeWeaponType.GrenadeLauncher =>
                    ERangeAttackType.Projectile,
                _ => ERangeAttackType.None
            };
        }

        public void Validate()
        {
            _baseDamage = Mathf.Max(0f, _baseDamage);
            _maxDistance = Mathf.Max(0.01f, _maxDistance);

            _attackTuning ??= new RangeAttackTuning();
            _attackTuning.Validate();

            _chargeSettings ??= new RangeChargeSettings();
            _chargeSettings.Validate();

            _impactSettings ??= new AttackImpactSettings();
            _impactSettings.Validate();

            _physics ??= new PhysicsRangeAttackSettings();

            _penetration ??= new PenetrationAttackSettings();
            _penetration.Validate();

            _projectile.Validate();
        }
    }
}
