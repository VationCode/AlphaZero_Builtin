using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using System;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // 근접 무기 타입 하나에 적용할 Player Combat 설정이다.
    [Serializable]
    public sealed class MeleeCombatSettings
    {
        [SerializeField]
        private EMeleeWeaponType _weaponType = EMeleeWeaponType.None;

        [SerializeField, Min(0f)]
        private float _baseDamage = 20f;

        [Tooltip("한 번의 Skill 판정에서 임시로 저장할 최대 Collider 수입니다.")]
        [SerializeField, Min(1)]
        private int _hitBufferCapacity = 16;

        [SerializeField]
        private MeleeComboDefinition _comboDefinition;

        [SerializeField]
        private AnimatorOverrideController _animatorOverrideController;

        public EMeleeWeaponType WeaponType => _weaponType;
        public float BaseDamage => Mathf.Max(0f, _baseDamage);
        public int HitBufferCapacity => Mathf.Max(1, _hitBufferCapacity);
        public MeleeComboDefinition ComboDefinition => _comboDefinition;
        public AnimatorOverrideController AnimatorOverrideController =>
            _animatorOverrideController;

        public bool IsValid =>
            _weaponType != EMeleeWeaponType.None &&
            BaseDamage > 0f &&
            _comboDefinition != null &&
            _comboDefinition.IsValid;

        public void Validate()
        {
            _baseDamage = Mathf.Max(0f, _baseDamage);
            _hitBufferCapacity = Mathf.Max(1, _hitBufferCapacity);
        }
    }

    // 근접 공격·콤보·스킬·방어 실행을 소유하는 대표 Combat 모듈이다.
    [DisallowMultipleComponent]
    public sealed class MeleeCombatModule : MonoBehaviour
    {
        [Header("Weapon Type Settings")]
        [SerializeField]
        private MeleeCombatSettings[] _settings;

        private readonly MeleeCombatContext _context = new();
        private readonly MeleeCombatFlow _flow = new();
        private readonly MeleeAttackModule _attackModule = new();

        private Transform _attacker;
        private MeleeWeapon _activeWeapon;
        private MeleeCombatSettings _activeSettings;
        private EWeaponActionType _activeActionType = EWeaponActionType.None;
        private Action<MeleeSkillDefinition> _skillStarted;
        private Action<MeleeSkillDefinition> _effectRequested;
        private Action<MeleeSkillDefinition> _hitConfirmed;

        public MeleeWeapon CurrentWeapon => _activeWeapon;
        public EWeaponActionType ActiveActionType => _activeActionType;
        public bool HasActiveAction =>
            _activeActionType != EWeaponActionType.None;
        public int CurrentSkillIndex => _flow.CurrentSkillIndex;
        public MeleeSkillDefinition CurrentSkill => _flow.CurrentSkill;
        public string CurrentSkillId => CurrentSkill?.SkillId;
        public string CurrentAnimationKey => CurrentSkill?.AnimationKey;
        public AnimatorOverrideController AnimatorOverrideController =>
            _activeSettings?.AnimatorOverrideController;
        public bool IsGuarding => _context.IsValid && _flow.IsGuarding;
        public Transform AttackSource => _context.AttackSource;

        public bool Bind(
            Transform p_attacker,
            Action<MeleeSkillDefinition> p_skillStarted,
            Action<MeleeSkillDefinition> p_effectRequested,
            Action<MeleeSkillDefinition> p_hitConfirmed)
        {
            if (p_attacker == null ||
                p_skillStarted == null ||
                p_effectRequested == null ||
                p_hitConfirmed == null)
            {
                return false;
            }

            UnbindCurrentWeapon();
            _attacker = p_attacker;
            _skillStarted = p_skillStarted;
            _effectRequested = p_effectRequested;
            _hitConfirmed = p_hitConfirmed;
            return true;
        }

        // 장착 타입에 맞는 Combat 설정을 선택하고 근접 실행 객체를 구성한다.
        public bool TryBindWeapon(
            MeleeWeapon p_weapon,
            float p_additionalDamage)
        {
            if (p_weapon == null ||
                !p_weapon.IsInitialized ||
                _attacker == null ||
                !TryGetSettings(p_weapon.MeleeType, out MeleeCombatSettings settings))
            {
                return false;
            }

            UnbindCurrentWeapon();
            _activeWeapon = p_weapon;
            _activeSettings = settings;

            if (!_context.Bind(
                    p_weapon,
                    _attacker,
                    _attacker,
                    p_additionalDamage) ||
                !_attackModule.Bind(
                    _context,
                    settings.BaseDamage,
                    settings.HitBufferCapacity,
                    _hitConfirmed) ||
                !_flow.Bind(
                    settings.ComboDefinition,
                    _attackModule,
                    _skillStarted,
                    _effectRequested))
            {
                UnbindCurrentWeapon();
                return false;
            }

            return true;
        }

        public bool TryBeginAction(EWeaponActionType p_type)
        {
            if (!_context.IsValid ||
                p_type == EWeaponActionType.None ||
                HasActiveAction ||
                !_flow.TryBeginAction(p_type))
            {
                return false;
            }

            _activeActionType = p_type;
            return true;
        }

        public void TickAction(
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (!HasActiveAction)
                return;

            EWeaponActionType actionType = _activeActionType;

            if (_flow.EndsOnInputRelease(actionType) && !p_isInputHeld)
            {
                EndAction();
                return;
            }

            if (_flow.TickAction(
                    actionType,
                    p_isInputHeld,
                    p_isInputPressed,
                    p_deltaTime) == EMeleeCombatResult.Completed)
            {
                EndAction();
            }
        }

        public void CancelAction()
        {
            if (!HasActiveAction)
                return;

            _flow.CancelAction(_activeActionType);
            _activeActionType = EWeaponActionType.None;
        }

        public MeleeSkillDefinition GetSkillDefinition(int p_skillIndex)
        {
            return _flow.ComboDefinition?.GetSkill(p_skillIndex);
        }

        public void UnbindCurrentWeapon()
        {
            CancelAction();
            _flow.Reset();
            _attackModule.Unbind();
            _context.Clear();
            _activeWeapon = null;
            _activeSettings = null;
        }

        private void EndAction()
        {
            if (!HasActiveAction)
                return;

            _flow.EndAction(_activeActionType);
            _activeActionType = EWeaponActionType.None;
        }

        private bool TryGetSettings(
            EMeleeWeaponType p_weaponType,
            out MeleeCombatSettings p_settings)
        {
            p_settings = null;

            if (_settings == null)
                return false;

            foreach (MeleeCombatSettings settings in _settings)
            {
                if (settings != null &&
                    settings.IsValid &&
                    settings.WeaponType == p_weaponType)
                {
                    p_settings = settings;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            if (_settings == null)
                return;

            foreach (MeleeCombatSettings settings in _settings)
                settings?.Validate();
        }

        private void OnDestroy()
        {
            UnbindCurrentWeapon();
        }
    }
}
