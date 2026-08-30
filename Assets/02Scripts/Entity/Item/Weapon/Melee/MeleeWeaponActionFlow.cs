using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    internal enum EMeleeWeaponActionResult
    {
        Running,
        Completed
    }

    // Combo와 Skill 자식 Flow를 조정하고 행동 결과만 부모에 반환한다.
    internal sealed class MeleeWeaponActionFlow
    {
        private readonly MeleeComboFlow _comboFlow = new();
        private readonly MeleeWeaponSkillFlow _skillFlow = new();

        private MeleeComboDefinition _comboDefinition;
        private MeleeWeaponAttackModule _attackModule;
        private Action<MeleeSkillDefinition> _skillStarted;
        private Action<MeleeSkillDefinition> _effectRequested;
        private bool _isBound;

        public int CurrentSkillIndex => _comboFlow.CurrentSkillIndex;
        public MeleeSkillDefinition CurrentSkill =>
            _comboFlow.CurrentSkill;
        public bool IsGuarding { get; private set; }

        public bool Bind(
            MeleeComboDefinition p_comboDefinition,
            MeleeWeaponAttackModule p_attackModule,
            Action<MeleeSkillDefinition> p_skillStarted,
            Action<MeleeSkillDefinition> p_effectRequested)
        {
            _comboDefinition = p_comboDefinition;
            _attackModule = p_attackModule;
            _skillStarted = p_skillStarted;
            _effectRequested = p_effectRequested;
            _isBound =
                _comboDefinition != null &&
                _comboDefinition.IsValid &&
                _attackModule?.IsConfigured == true;
            Reset();
            return _isBound;
        }

        public void Reset()
        {
            _comboFlow.Cancel();
            _skillFlow.End();
            IsGuarding = false;
        }

        public bool EndsOnInputRelease(EWeaponActionType p_type)
        {
            return p_type == EWeaponActionType.Secondary;
        }

        public bool TryBeginAction(
            EWeaponActionType p_type,
            bool p_hasUseContext)
        {
            if (!_isBound || !p_hasUseContext)
                return false;

            switch (p_type)
            {
                case EWeaponActionType.Primary:
                    return TryBeginPrimary();

                case EWeaponActionType.Secondary:
                    _comboFlow.Cancel();
                    _skillFlow.End();
                    IsGuarding = true;
                    return true;

                default:
                    return false;
            }
        }

        public EMeleeWeaponActionResult TickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (p_type != EWeaponActionType.Primary ||
                CurrentSkill == null)
            {
                return EMeleeWeaponActionResult.Running;
            }

            _skillFlow.Tick(
                p_deltaTime,
                _attackModule,
                _effectRequested);

            EMeleeComboFlowResult result = _comboFlow.Tick(
                p_isInputHeld,
                p_isInputPressed,
                _skillFlow.ElapsedTime,
                Time.time,
                out MeleeSkillDefinition nextSkill);

            switch (result)
            {
                case EMeleeComboFlowResult.SkillChanged:
                    _skillFlow.End();

                    if (TryStartSkill(nextSkill))
                        return EMeleeWeaponActionResult.Running;

                    _comboFlow.Cancel();
                    return EMeleeWeaponActionResult.Completed;

                case EMeleeComboFlowResult.Completed:
                    return EMeleeWeaponActionResult.Completed;

                default:
                    return EMeleeWeaponActionResult.Running;
            }
        }

        public void EndAction(EWeaponActionType p_type)
        {
            if (p_type == EWeaponActionType.Primary)
            {
                _skillFlow.End();
                _comboFlow.EndActive();
            }

            if (p_type == EWeaponActionType.Secondary)
                IsGuarding = false;
        }

        public void CancelAction(EWeaponActionType p_type)
        {
            EndAction(p_type);
            _comboFlow.Cancel();
        }

        private bool TryBeginPrimary()
        {
            if (!_comboFlow.TryBegin(
                    _comboDefinition,
                    Time.time,
                    out MeleeSkillDefinition skill) ||
                !TryStartSkill(skill))
            {
                _comboFlow.Cancel();
                return false;
            }

            return true;
        }

        private bool TryStartSkill(MeleeSkillDefinition p_skill)
        {
            if (!_skillFlow.TryBegin(p_skill))
                return false;

            _skillStarted?.Invoke(p_skill);
            _skillFlow.TryRequestEffect(_effectRequested);
            return true;
        }
    }
}
