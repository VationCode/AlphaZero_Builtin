using Alpha.Combat;
using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;

namespace Alpha.Player.Combat
{
    // Combo 갱신 결과를 대표 Module이 해석할 수 있게 구분한다.
    internal enum EMeleeComboFlowResult
    {
        Waiting,
        SkillChanged,
        Completed
    }

    // Melee Combo 자산을 해석하고 현재 Skill 선택과 연계 상태만 관리한다.
    internal sealed class PlayerMeleeComboFlow
    {
        public int CurrentSkillIndex { get; private set; } = -1;
        public MeleeSkillDefinition CurrentSkill { get; private set; }

        private MeleeComboDefinition _activeCombo;
        private bool _isNextSkillQueued;

        private MeleeComboDefinition _rememberedCombo;
        private int _rememberedSkillIndex = -1;
        private float _skillChainExpireTime;

        public bool TryBegin(
            MeleeComboDefinition p_combo,
            float p_currentTime,
            out MeleeSkillDefinition p_skill)
        {
            p_skill = null;

            if (p_combo == null || !p_combo.IsValid)
                return false;

            int skillIndex = GetStartSkillIndex(
                p_combo,
                p_currentTime);

            if (!TryActivate(p_combo, skillIndex))
                return false;

            p_skill = CurrentSkill;
            return true;
        }

        // 현재 Skill 시간과 입력을 바탕으로 대기·다음 Skill·종료만 결정한다.
        public EMeleeComboFlowResult Tick(
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_skillElapsedTime,
            float p_currentTime,
            out MeleeSkillDefinition p_nextSkill)
        {
            p_nextSkill = null;

            if (_activeCombo == null || CurrentSkill == null)
                return EMeleeComboFlowResult.Completed;

            MeleeComboStep currentStep =
                _activeCombo.GetStep(CurrentSkillIndex);

            if (currentStep == null)
                return EMeleeComboFlowResult.Completed;

            int nextSkillIndex = CurrentSkillIndex + 1;
            bool isLastSkill = nextSkillIndex >= _activeCombo.Count;

            if (isLastSkill)
                nextSkillIndex = _activeCombo.Loop ? 0 : -1;

            bool wantsNextSkill = IsActionInput(
                _activeCombo.InputMode,
                p_isInputHeld,
                p_isInputPressed);

            if (p_skillElapsedTime <
                CurrentSkill.AttackSettings.Duration)
            {
                if (nextSkillIndex >= 0 &&
                    !_isNextSkillQueued &&
                    p_skillElapsedTime >= currentStep.NextSkillInputTime &&
                    wantsNextSkill)
                {
                    _isNextSkillQueued = true;
                }

                return EMeleeComboFlowResult.Waiting;
            }

            if (nextSkillIndex >= 0 &&
                (_isNextSkillQueued || wantsNextSkill) &&
                TryActivate(_activeCombo, nextSkillIndex))
            {
                p_nextSkill = CurrentSkill;
                return EMeleeComboFlowResult.SkillChanged;
            }

            if (isLastSkill)
                ClearSkillMemory();
            else
                RememberNextSkill(
                    _activeCombo,
                    nextSkillIndex,
                    p_currentTime);

            return EMeleeComboFlowResult.Completed;
        }

        // 정상 종료에서는 다음 Skill 기억을 유지한다.
        public void EndActive()
        {
            _activeCombo = null;
            CurrentSkillIndex = -1;
            CurrentSkill = null;
            _isNextSkillQueued = false;
        }

        // 강제 취소에서는 진행 상태와 연계 기억을 모두 제거한다.
        public void Cancel()
        {
            EndActive();
            ClearSkillMemory();
        }

        private bool TryActivate(
            MeleeComboDefinition p_combo,
            int p_skillIndex)
        {
            MeleeSkillDefinition skill = p_combo?.GetSkill(p_skillIndex);

            if (skill == null || !skill.IsValid)
                return false;

            _activeCombo = p_combo;
            CurrentSkillIndex = p_skillIndex;
            CurrentSkill = skill;
            _isNextSkillQueued = false;
            ClearSkillMemory();
            return true;
        }

        private int GetStartSkillIndex(
            MeleeComboDefinition p_combo,
            float p_currentTime)
        {
            if (ReferenceEquals(_rememberedCombo, p_combo) &&
                _rememberedSkillIndex >= 0 &&
                p_currentTime <= _skillChainExpireTime &&
                p_combo.GetSkill(_rememberedSkillIndex) != null)
            {
                return _rememberedSkillIndex;
            }

            ClearSkillMemory();
            return 0;
        }

        private void RememberNextSkill(
            MeleeComboDefinition p_combo,
            int p_skillIndex,
            float p_currentTime)
        {
            _rememberedCombo = p_combo;
            _rememberedSkillIndex = p_skillIndex;
            _skillChainExpireTime =
                p_currentTime + p_combo.GraceDuration;
        }

        private void ClearSkillMemory()
        {
            _rememberedCombo = null;
            _rememberedSkillIndex = -1;
            _skillChainExpireTime = 0f;
        }

        private static bool IsActionInput(
            EWeaponInputMode p_inputMode,
            bool p_isInputHeld,
            bool p_isInputPressed)
        {
            return p_inputMode == EWeaponInputMode.Auto
                ? p_isInputHeld
                : p_isInputPressed;
        }
    }
}
