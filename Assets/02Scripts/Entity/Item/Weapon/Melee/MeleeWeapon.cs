using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    // MeleeWeapon Prefab에서 선택할 수 있는 근접 무기 타입이다.
    public enum EMeleeWeaponType
    {
        None = (int)EWeaponType.None,
        Sword = (int)EWeaponType.Sword,
        Polearm = (int)EWeaponType.Polearm
    }

    // Melee 자식 객체를 조립하고 외부 명령과 결과를 중계하는 대표 진입점이다.
    [DisallowMultipleComponent]
    public sealed class MeleeWeapon : Weapon
    {
        [Header("Identity")]
        [SerializeField]
        private EMeleeWeaponType _weaponType = EMeleeWeaponType.None;

        [Header("Attack")]
        [SerializeField, Min(0f)]
        private float _baseDamage = 20f;

        [Tooltip("한 번의 Skill 판정에서 임시로 저장할 최대 Collider 수입니다.")]
        [SerializeField, Min(1)]
        private int _hitBufferCapacity = 16;

        [Tooltip("이 무기가 실행할 Skill 연계 자산입니다.")]
        [SerializeField]
        private MeleeComboDefinition _comboDefinition;

        [Header("Animation")]
        [SerializeField]
        private AnimatorOverrideController _animatorOverrideController;

        private readonly MeleeWeaponContext _context = new();
        private readonly MeleeWeaponActionFlow _actionFlow = new();
        private readonly MeleeWeaponAttackModule _attackModule = new();
        private bool _isConfigured;

        public event Action<MeleeSkillDefinition> OnSkillStarted;
        public event Action<MeleeSkillDefinition> OnSkillEffectRequested;
        public event Action<MeleeSkillDefinition> OnSkillHitConfirmed;

        public float BaseDamage => _baseDamage;
        public sealed override EWeaponType WeaponType =>
            (EWeaponType)_weaponType;
        public MeleeComboDefinition ComboDefinition => _comboDefinition;
        public AnimatorOverrideController AnimatorOverrideController =>
            _animatorOverrideController;
        public bool HasUseContext => _context.HasUser;
        public Transform AttackSource => _context.AttackSource;
        public int CurrentSkillIndex => _actionFlow.CurrentSkillIndex;
        public MeleeSkillDefinition CurrentSkill =>
            _actionFlow.CurrentSkill;
        public bool IsGuarding =>
            HasUseContext && _actionFlow.IsGuarding;

        protected sealed override bool CanInitialize(WeaponDTO p_data)
        {
            return p_data?.WeaponCategory == EWeaponCategory.Melee;
        }

        protected override void OnInitialized()
        {
            ValidateSettings();
            _context.ClearUser();

            bool didBindAttack = _attackModule.Bind(
                _context,
                _baseDamage,
                _hitBufferCapacity,
                PublishSkillHitConfirmed);

            _isConfigured = didBindAttack &&
                _actionFlow.Bind(
                    _comboDefinition,
                    _attackModule,
                    PublishSkillStarted,
                    PublishSkillEffectRequested);

            if (!_isConfigured)
            {
                Debug.LogError(
                    "근접 무기 공격 객체를 초기화하지 못했습니다.",
                    this);
            }
        }

        // 장착 Entity의 구체 구현 대신 공격 출처와 보정 데이터만 연결한다.
        public bool BindUseContext(in MeleeWeaponUseContext p_context)
        {
            if (!IsInitialized ||
                !_isConfigured ||
                !p_context.IsValid)
            {
                return false;
            }

            UnbindUseContext();

            if (!_context.BindUser(p_context))
                return false;

            _actionFlow.Reset();
            return true;
        }

        public void UnbindUseContext()
        {
            CancelAction();
            _context.ClearUser();
            _actionFlow.Reset();
        }

        public MeleeSkillDefinition GetSkillDefinition(int p_skillIndex)
        {
            return _comboDefinition?.GetSkill(p_skillIndex);
        }

        public override bool EndsOnInputRelease(EWeaponActionType p_type)
        {
            return _actionFlow.EndsOnInputRelease(p_type);
        }

        protected override bool OnBeginAction(EWeaponActionType p_type)
        {
            return _actionFlow.TryBeginAction(
                p_type,
                HasUseContext);
        }

        protected override void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            EMeleeWeaponActionResult result =
                _actionFlow.TickAction(
                    p_type,
                    p_isInputHeld,
                    p_isInputPressed,
                    p_deltaTime);

            if (result == EMeleeWeaponActionResult.Completed)
                EndAction();
        }

        protected override void OnEndAction(EWeaponActionType p_type)
        {
            _actionFlow.EndAction(p_type);
        }

        protected override void OnCancelAction(EWeaponActionType p_type)
        {
            _actionFlow.CancelAction(p_type);
        }

        private void PublishSkillStarted(MeleeSkillDefinition p_skill)
        {
            OnSkillStarted?.Invoke(p_skill);
        }

        private void PublishSkillEffectRequested(
            MeleeSkillDefinition p_skill)
        {
            OnSkillEffectRequested?.Invoke(p_skill);
        }

        private void PublishSkillHitConfirmed(
            MeleeSkillDefinition p_skill)
        {
            OnSkillHitConfirmed?.Invoke(p_skill);
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        private void ValidateSettings()
        {
            _baseDamage = Mathf.Max(0f, _baseDamage);
            _hitBufferCapacity = Mathf.Max(1, _hitBufferCapacity);
        }

        private void OnDestroy()
        {
            UnbindUseContext();
            _attackModule.Unbind();
        }
    }
}
