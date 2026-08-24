using Alpha.Combat;
using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using System;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Melee 무기 Action을 Combo Flow와 단일 Skill Module로 조립하는 대표 진입점이다.
    [Serializable]
    public sealed class PlayerMeleeAttackModule : IMeleeWeaponActionController
    {
        [SerializeField]
        private PlayerMeleeSkillModule _skillModule = new();

        public int CurrentSkillIndex =>
            _comboFlow?.CurrentSkillIndex ?? -1;
        public MeleeSkillDefinition CurrentSkill =>
            _comboFlow?.CurrentSkill;
        public string CurrentSkillId => CurrentSkill?.SkillId;
        public string CurrentAnimationKey => CurrentSkill?.AnimationKey;
        public bool IsGuarding { get; private set; }
        public Transform AttackSource => _skillModule?.AttackSource;

        private PlayerMeleeComboFlow _comboFlow = new();
        private MeleeWeapon _activeWeapon;
        private Action<MeleeSkillDefinition> _effectRequested;
        private bool _didRequestSkillEffect;

        public bool Bind(
            Transform p_attacker,
            Func<float, float> p_damageResolver,
            Action<MeleeSkillDefinition> p_effectRequested,
            Action<MeleeSkillDefinition> p_hitConfirmed)
        {
            _skillModule ??= new PlayerMeleeSkillModule();
            _comboFlow ??= new PlayerMeleeComboFlow();
            _comboFlow.Cancel();
            _skillModule.End();
            _effectRequested = p_effectRequested;
            _didRequestSkillEffect = false;

            return _skillModule.Bind(
                p_attacker,
                p_damageResolver,
                p_hitConfirmed);
        }

        public bool EndsOnInputRelease(EWeaponActionType p_type)
        {
            return p_type == EWeaponActionType.Secondary;
        }

        public bool TryBeginAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type)
        {
            if (p_weapon == null || AttackSource == null)
                return false;

            switch (p_type)
            {
                case EWeaponActionType.Primary:
                    return TryBeginPrimary(p_weapon);

                case EWeaponActionType.Secondary:
                    _comboFlow.Cancel();
                    _activeWeapon = p_weapon;
                    IsGuarding = true;
                    return true;

                default:
                    return false;
            }
        }

        public void TickAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (p_type != EWeaponActionType.Primary ||
                p_weapon == null ||
                !ReferenceEquals(p_weapon, _activeWeapon) ||
                CurrentSkill == null)
            {
                return;
            }

            _skillModule.Tick(p_deltaTime);
            TryRequestSkillEffect();

            EMeleeComboFlowResult result = _comboFlow.Tick(
                p_isInputHeld,
                p_isInputPressed,
                _skillModule.ElapsedTime,
                Time.time,
                out MeleeSkillDefinition nextSkill);

            switch (result)
            {
                case EMeleeComboFlowResult.SkillChanged:
                    _skillModule.End();

                    if (!TryStartSkill(p_weapon, nextSkill))
                    {
                        _comboFlow.Cancel();
                        p_weapon.EndAction();
                    }
                    break;

                case EMeleeComboFlowResult.Completed:
                    p_weapon.EndAction();
                    break;
            }
        }

        public void EndAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type)
        {
            ResetActiveAction(p_weapon, p_type);
        }

        public void CancelAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type)
        {
            ResetActiveAction(p_weapon, p_type);
            _comboFlow.Cancel();
        }

        private bool TryBeginPrimary(MeleeWeapon p_weapon)
        {
            if (!_comboFlow.TryBegin(
                    p_weapon.ComboDefinition,
                    Time.time,
                    out MeleeSkillDefinition skill) ||
                !TryStartSkill(p_weapon, skill))
            {
                _comboFlow.Cancel();
                return false;
            }

            return true;
        }

        private bool TryStartSkill(
            MeleeWeapon p_weapon,
            MeleeSkillDefinition p_skill)
        {
            if (!_skillModule.TryBegin(p_weapon, p_skill))
                return false;

            _activeWeapon = p_weapon;
            _didRequestSkillEffect = false;
            p_weapon.NotifySkillStarted(p_skill);
            TryRequestSkillEffect();
            return true;
        }

        // Skill이 취소되지 않고 설정 시간에 도달한 경우 Effect를 한 번만 요청한다.
        private void TryRequestSkillEffect()
        {
            MeleeSkillDefinition skill = CurrentSkill;

            if (_didRequestSkillEffect ||
                skill == null ||
                _skillModule.ElapsedTime < skill.EffectStartTime)
            {
                return;
            }

            _didRequestSkillEffect = true;
            _effectRequested?.Invoke(skill);
        }

        private void ResetActiveAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type)
        {
            if (!ReferenceEquals(p_weapon, _activeWeapon))
                return;

            if (p_type == EWeaponActionType.Primary)
            {
                _skillModule.End();
                _comboFlow.EndActive();
                _didRequestSkillEffect = false;
            }

            if (p_type == EWeaponActionType.Secondary)
                IsGuarding = false;

            _activeWeapon = null;
        }

        public void Validate()
        {
            _skillModule ??= new PlayerMeleeSkillModule();
            _comboFlow ??= new PlayerMeleeComboFlow();
            _skillModule.Validate();
        }
    }
}
