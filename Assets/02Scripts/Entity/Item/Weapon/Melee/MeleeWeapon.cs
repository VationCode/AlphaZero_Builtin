using Alpha.Combat;
using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    // MeleeWeapon이 Entity 소유 공격 로직에 위임하기 위한 계약이다.
    public interface IMeleeWeaponActionController
    {
        bool EndsOnInputRelease(EWeaponActionType p_type);

        bool TryBeginAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type);

        void TickAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime);

        void EndAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type);

        void CancelAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type);
    }

    // 근접 무기의 고유 공격력과 모션 교체 정보를 제공한다.
    public abstract class MeleeWeapon : Weapon
    {
        [Header("Attack")]
        [SerializeField, Min(0f)]
        private float _baseDamage = 20f;

        [Tooltip("이 무기가 실행할 Skill 연계 자산입니다.")]
        [SerializeField]
        private MeleeComboDefinition _comboDefinition;

        [Header("Animation")]
        [SerializeField]
        private AnimatorOverrideController _animatorOverrideController;

        public float BaseDamage => _baseDamage;
        public MeleeComboDefinition ComboDefinition => _comboDefinition;
        public AnimatorOverrideController AnimatorOverrideController =>
            _animatorOverrideController;

        // 무기에 부착된 View가 실제로 시작된 공통 Skill 자산을 구독한다.
        public event Action<CombatSkillDefinition> OnSkillStarted;

        private IMeleeWeaponActionController _actionController;

        protected sealed override bool CanInitialize(WeaponDTO p_data)
        {
            return p_data is MeleeWeaponDTO;
        }

        // Melee 공격의 실행 주체가 되는 Entity Module을 연결한다.
        public bool BindActionController(
            IMeleeWeaponActionController p_actionController)
        {
            if (p_actionController == null ||
                _comboDefinition == null ||
                !_comboDefinition.IsValid)
            {
                return false;
            }

            _actionController = p_actionController;
            return true;
        }

        public override bool EndsOnInputRelease(EWeaponActionType p_type)
        {
            return _actionController?.EndsOnInputRelease(p_type) ?? true;
        }

        protected override bool OnBeginAction(EWeaponActionType p_type)
        {
            return _actionController != null &&
                   _actionController.TryBeginAction(this, p_type);
        }

        protected override void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            _actionController?.TickAction(
                this,
                p_type,
                p_isInputHeld,
                p_isInputPressed,
                p_deltaTime);
        }

        protected override void OnEndAction(EWeaponActionType p_type)
        {
            _actionController?.EndAction(this, p_type);
        }

        protected override void OnCancelAction(EWeaponActionType p_type)
        {
            _actionController?.CancelAction(this, p_type);
        }

        // Player 공격 Module이 결정한 Skill 자산을 무기 View에 전달한다.
        internal void NotifySkillStarted(CombatSkillDefinition p_skill)
        {
            if (p_skill != null)
                OnSkillStarted?.Invoke(p_skill);
        }

        private void OnValidate()
        {
            _baseDamage = Mathf.Max(0f, _baseDamage);
        }
    }
}
