using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 공격 방식별 설정이 제공해야 하는 타입과 검증 계약이다.
    [Serializable]
    public abstract class RangeAttackSettings
    {
        public abstract ERangeAttackType AttackType { get; }
        public abstract bool IsValid { get; }
        public abstract void Validate();
    }

    // 선택한 공격 방식과 해당 방식의 설정 객체 하나만 소유한다.
    [Serializable]
    public sealed class RangeWeaponAttackSettings
    {
        [SerializeField]
        private ERangeAttackType _attackType = ERangeAttackType.Hitscan;

        [Tooltip("선택한 공격 방식이 충돌 대상으로 검사할 Layer입니다.")]
        [SerializeField]
        private LayerMask _hitMask = (LayerMask)129;

        [SerializeReference]
        private RangeAttackSettings _activeSettings;

        public ERangeAttackType AttackType => _attackType;
        public LayerMask HitMask => _hitMask;
        public RangeAttackSettings ActiveSettings => _activeSettings;
        public PenetrationAttackSettings Penetration =>
            _activeSettings as PenetrationAttackSettings;
        public ProjectileAttackSettings Projectile =>
            _activeSettings as ProjectileAttackSettings;

        public bool IsValid => _attackType switch
        {
            ERangeAttackType.Hitscan => _activeSettings == null,
            ERangeAttackType.Penetration or ERangeAttackType.Projectile =>
                _activeSettings != null &&
                _activeSettings.AttackType == _attackType &&
                _activeSettings.IsValid,
            _ => false
        };

        public void Validate()
        {
            if (_attackType is ERangeAttackType.None or
                ERangeAttackType.Hitscan)
            {
                _activeSettings = null;
                return;
            }

            if (_activeSettings == null ||
                _activeSettings.AttackType != _attackType)
            {
                _activeSettings = CreateDefault(_attackType);
            }

            _activeSettings?.Validate();
        }

        public static RangeAttackSettings CreateDefault(
            ERangeAttackType p_attackType)
        {
            return p_attackType switch
            {
                ERangeAttackType.Penetration =>
                    new PenetrationAttackSettings(),
                ERangeAttackType.Projectile =>
                    new ProjectileAttackSettings(),
                _ => null
            };
        }
    }
}
