using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Item.Weapon.Melee
{
    // 근접 무기의 공통 공격과 방어 입력 생명주기를 담당한다.
    public abstract class MeleeWeapon : Weapon
    {
        [Header("Animation")]
        [SerializeField]
        private AnimationClip[] _comboClips;
        [SerializeField] private AnimationClip _secondaryClip;

        [Header("Combo")]
        [SerializeField]
        private EWeaponInputMode _primaryInputMode = EWeaponInputMode.Auto;

        [FormerlySerializedAs("_comboTransitionTime")]
        [SerializeField, Range(0.1f, 0.95f)]
        private float _comboInputWindowStart = 0.8f;        // 다음 콤보 입력을 예약할 수 있는 구간

        [SerializeField, Min(0f)]
        private float _comboGraceDuration = 0.5f;           // Idle에서도 다음 콤보를 기억하는 시간

        public IReadOnlyList<AnimationClip> ComboClips => _comboClips;
        public AnimationClip SecondaryClip => _secondaryClip;

        public int CurrentComboIndex { get; private set; } = -1;
        public int ComboCount => _comboClips?.Length ?? 0;
        public bool IsGuarding { get; private set; }

        private AnimationClip _activeAttackClip;
        private float _attackElapsedTime;

        private bool _isNextComboQueued;
        private int _rememberedComboIndex = -1;
        private float _comboExpireTime;

        protected virtual void OnAttack() { }
        protected virtual void OnAttackTick(float p_deltaTime) { }
        protected virtual void OnGuardChanged(bool p_isGuarding) { }

        // Primary는 애니메이션 완료로 종료하고 Secondary는 입력 해제로 종료한다.
        public override bool EndsOnInputRelease(EWeaponActionType p_type)
        {
            return p_type == EWeaponActionType.Secondary;
        }

        protected sealed override bool CanInitialize(WeaponDTO p_data)
        {
            return p_data is MeleeWeaponDTO;
        }

        // 현재 콤보 순서에 해당하는 클립을 반환한다.
        public AnimationClip GetComboClip(int p_comboIndex)
        {
            if (_comboClips == null ||
                p_comboIndex < 0 ||
                p_comboIndex >= _comboClips.Length)
            {
                return null;
            }

            return _comboClips[p_comboIndex];
        }

        // 좌클릭은 공격을 시작하고 우클릭은 방어 상태에 진입한다.
        protected override bool OnBeginAction(EWeaponActionType p_type)
        {
            switch (p_type)
            {
                case EWeaponActionType.Primary:
                    return TryStartCombo(GetPrimaryStartComboIndex());

                case EWeaponActionType.Secondary:
                    ClearComboMemory();
                    SetGuarding(true);
                    return true;

                default:
                    return false;
            }
        }
        // 지정한 콤보 클립을 현재 공격으로 시작한다.
        private bool TryStartCombo(int p_comboIndex)
        {
            AnimationClip comboClip = GetComboClip(p_comboIndex);

            if (comboClip == null)
                return false;

            CurrentComboIndex = p_comboIndex;
            _activeAttackClip = comboClip;
            _attackElapsedTime = 0f;
            _isNextComboQueued = false;
            ClearComboMemory();

            OnAttack();
            return true;
        }

        // 근접 공격의 입력 예약과 클립 종료 시점을 관리한다.
        protected override void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (p_type != EWeaponActionType.Primary || _activeAttackClip == null)
            {
                return;
            }

            _attackElapsedTime += p_deltaTime;
            OnAttackTick(p_deltaTime);

            float clipLength = _activeAttackClip.length;
            int nextComboIndex = CurrentComboIndex + 1;
            bool hasNextCombo = GetComboClip(nextComboIndex) != null;

            // 마지막 콤보는 현재 클립 길이만큼 재생하고 끝낸다.
            if (!hasNextCombo)
            {
                if (_attackElapsedTime >= clipLength)
                {
                    ClearComboMemory();
                    EndAction();
                }

                return;
            }

            bool wantsNextCombo = IsActionInput(
                _primaryInputMode,
                p_isInputHeld,
                p_isInputPressed);

            if (_attackElapsedTime < clipLength)
            {
                float inputWindowStart = clipLength * _comboInputWindowStart;

                // 재생 중에는 다음 콤보를 시작하지 않고 입력만 예약한다.
                if (!_isNextComboQueued &&
                    _attackElapsedTime >= inputWindowStart &&
                    wantsNextCombo)
                {
                    _isNextComboQueued = true;
                }

                return;
            }

            // 종료 프레임의 입력까지 인정하되, 현재 클립 길이가 지난 뒤에만 전환한다.
            if (_isNextComboQueued || wantsNextCombo)
            {
                TryStartCombo(nextComboIndex);
                return;
            }

            // 예약이 없다면 Idle로 돌아가되 다음 콤보 순서는 Grace 시간 동안 기억한다.
            RememberNextCombo(nextComboIndex);
            EndAction();
        }

        protected override void OnEndAction(EWeaponActionType p_type)
        {
            ResetActiveAction(p_type);
        }

        protected override void OnCancelAction(EWeaponActionType p_type)
        {
            ResetActiveAction(p_type);
            ClearComboMemory();
        }

        private void ResetActiveAction(EWeaponActionType p_type)
        {
            if (p_type == EWeaponActionType.Primary)
            {
                CurrentComboIndex = -1;
                _activeAttackClip = null;
                _attackElapsedTime = 0f;
                _isNextComboQueued = false;
            }

            if (p_type == EWeaponActionType.Secondary)
                SetGuarding(false);
        }

        // Grace 시간이 남아 있다면 이전 공격의 다음 콤보부터 시작한다.
        private int GetPrimaryStartComboIndex()
        {
            if (_rememberedComboIndex >= 0 &&
                Time.time <= _comboExpireTime &&
                GetComboClip(_rememberedComboIndex) != null)
            {
                return _rememberedComboIndex;
            }

            ClearComboMemory();
            return 0;
        }

        private void RememberNextCombo(int p_comboIndex)
        {
            _rememberedComboIndex = p_comboIndex;
            _comboExpireTime = Time.time + _comboGraceDuration;
        }

        private void ClearComboMemory()
        {
            _rememberedComboIndex = -1;
            _comboExpireTime = 0f;
        }

        private void SetGuarding(bool p_isGuarding)
        {
            IsGuarding = p_isGuarding;
            OnGuardChanged(p_isGuarding);
        }
    }
}
