using Alpha.Combat;
using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    // Combo 안에서 사용할 Skill과 다음 Skill 입력 가능 시점을 연결한다.
    [Serializable]
    public sealed class MeleeComboStep
    {
        [SerializeField]
        private MeleeSkillDefinition _skill;

        [Tooltip("현재 Skill 시작 후 다음 Skill 입력을 예약할 수 있는 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _nextSkillInputTime = 0.48f;

        public MeleeSkillDefinition Skill => _skill;
        public float NextSkillInputTime => _nextSkillInputTime;

        public bool IsValid =>
            _skill != null &&
            _skill.IsValid &&
            _nextSkillInputTime >= 0f &&
            _nextSkillInputTime <= _skill.AttackSettings.Duration;

        public void Validate()
        {
            float duration = _skill != null
                ? _skill.AttackSettings.Duration
                : float.MaxValue;

            _nextSkillInputTime = Mathf.Clamp(
                _nextSkillInputTime,
                0f,
                duration);
        }
    }

    // MeleeWeapon이 사용할 Skill 순서와 연계 규칙을 자산으로 보관한다.
    [CreateAssetMenu(
        fileName = "MeleeCombo",
        menuName = "Alpha/Combat/Combo/Melee")]
    public sealed class MeleeComboDefinition : ScriptableObject
    {
        [SerializeField]
        private string _comboId;

        [SerializeField]
        private EWeaponInputMode _inputMode = EWeaponInputMode.Auto;

        [Tooltip("Skill 연계가 끊긴 뒤 다음 순서를 기억하는 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _graceDuration = 0.5f;

        [Tooltip("마지막 Skill 입력이 이어지면 첫 Skill부터 다시 시작합니다.")]
        [SerializeField]
        private bool _loop = true;

        [SerializeField]
        private MeleeComboStep[] _steps;

        public string ComboId => _comboId?.Trim();
        public EWeaponInputMode InputMode => _inputMode;
        public float GraceDuration => _graceDuration;
        public bool Loop => _loop;
        public int Count => _steps?.Length ?? 0;

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ComboId) || Count == 0)
                    return false;

                foreach (MeleeComboStep step in _steps)
                {
                    if (step == null ||
                        !step.IsValid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public MeleeComboStep GetStep(int p_index)
        {
            if (_steps == null || p_index < 0 || p_index >= _steps.Length)
                return null;

            return _steps[p_index];
        }

        public MeleeSkillDefinition GetSkill(int p_index)
        {
            return GetStep(p_index)?.Skill;
        }

        private void OnValidate()
        {
            _comboId = _comboId?.Trim();
            _graceDuration = Mathf.Max(0f, _graceDuration);

            if (_steps == null)
                return;

            foreach (MeleeComboStep step in _steps)
                step?.Validate();
        }
    }
}
