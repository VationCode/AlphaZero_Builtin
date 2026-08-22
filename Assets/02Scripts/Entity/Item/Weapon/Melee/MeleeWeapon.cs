using Alpha.Combat;
using Alpha.Detection;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Item.Weapon.Melee
{
    // 하나의 콤보 모션이 사용할 타격 시점, 피해 배율, 공격 범위를 보관한다.
    [Serializable]
    public sealed class MeleeAttackSettings
    {
        [SerializeField, Range(0f, 1f)]
        private float _hitNormalizedTime = 0.35f;

        [SerializeField, Min(0f)]
        private float _damageMultiplier = 1f;

        [SerializeField]
        private EHitReaction _hitReaction = EHitReaction.Light;

        [SerializeField, Min(0f)]
        private float _knockbackDistance = 1.5f;

        [SerializeField, Min(0f)]
        private float _knockbackDuration = 0.2f;

        [SerializeField]
        private DetectionAreaSettings _area = new();

        public float HitNormalizedTime => _hitNormalizedTime;
        public float DamageMultiplier => _damageMultiplier;
        public EHitReaction HitReaction => _hitReaction;
        public float KnockbackDistance => _knockbackDistance;
        public float KnockbackDuration => _knockbackDuration;
        public DetectionAreaSettings Area => _area;

        public bool IsValid =>
            _area != null &&
            _area.IsValid &&
            _damageMultiplier > 0f;

        public void Validate()
        {
            _hitNormalizedTime = Mathf.Clamp01(_hitNormalizedTime);
            _damageMultiplier = Mathf.Max(0f, _damageMultiplier);
            _knockbackDistance = Mathf.Max(0f, _knockbackDistance);
            _knockbackDuration = Mathf.Max(0f, _knockbackDuration);
            _area?.Validate();
        }
    }

    // 근접 무기의 공통 공격과 방어 입력 생명주기를 담당한다.
    public abstract class MeleeWeapon : Weapon
    {
        [Header("Anim")]
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

        [Header("Attack")]
        [SerializeField, Min(0f)]
        private float _baseDamage = 20f;

        [Tooltip("Combo Clips와 같은 인덱스의 모션별 공격 설정")]
        [SerializeField]
        private MeleeAttackSettings[] _comboAttackSettings;

        [SerializeField, Min(1)]
        private int _hitBufferCapacity = 16;

        public IReadOnlyList<AnimationClip> ComboClips => _comboClips;
        public AnimationClip SecondaryClip => _secondaryClip;

        // 실제 콤보 공격이 시작된 시점을 View에 전달한다.
        public event Action<int> OnComboStarted;

        // 실제 Damage 적용에 성공한 공격만 View에 알린다.
        public event Action<int> OnHitConfirmed;

        public int CurrentComboIndex { get; private set; } = -1;
        public int ComboCount => _comboClips?.Length ?? 0;
        public bool IsGuarding { get; private set; }
        public Transform AttackSource => _attacker;

        private AnimationClip _activeAttackClip;
        private float _attackElapsedTime;
        private bool _didHitCurrentAttack;

        private Transform _attacker;
        private Collider[] _overlapBuffer;
        private DetectionAreaHit[] _hitBuffer;
        private readonly HashSet<IDamageable> _damagedTargets = new();

        private bool _isNextComboQueued;
        private int _rememberedComboIndex = -1;
        private float _comboExpireTime;

        protected virtual void OnAttack() { }
        protected virtual void OnAttackTick(float p_deltaTime) { }
        protected virtual void OnGuardChanged(bool p_isGuarding) { }

        // Player 루트를 공격 기준과 자기 자신 제외 기준으로 사용한다.
        public bool BindAttackSource(Transform p_attacker)
        {
            if (p_attacker == null)
                return false;

            _attacker = p_attacker;
            EnsureHitBuffers();
            return true;
        }

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
            _didHitCurrentAttack = false;
            _isNextComboQueued = false;
            ClearComboMemory();

            OnComboStarted?.Invoke(CurrentComboIndex);
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
            TryExecuteAttackHit();
            OnAttackTick(p_deltaTime);

            float clipLength = _activeAttackClip.length;
            int nextComboIndex = CurrentComboIndex + 1;
            bool isLastCombo = GetComboClip(nextComboIndex) == null;

            // 마지막 콤보 다음 입력은 첫 번째 콤보로 순환시킨다.
            if (isLastCombo)
                nextComboIndex = 0;

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
            if ((_isNextComboQueued || wantsNextCombo) &&
                TryStartCombo(nextComboIndex))
            {
                return;
            }

            // 마지막 콤보는 기억할 다음 순서가 없고, 중간 콤보만 Grace 시간 동안 기억한다.
            if (isLastCombo)
                ClearComboMemory();
            else
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
                _didHitCurrentAttack = false;
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

        // 현재 모션의 지정된 시점에 도달하면 공격당 한 번만 범위를 조회한다.
        private void TryExecuteAttackHit()
        {
            if (_didHitCurrentAttack ||
                _activeAttackClip == null ||
                _attacker == null)
            {
                return;
            }

            MeleeAttackSettings attackSettings =
                GetAttackSettings(CurrentComboIndex);

            if (attackSettings == null || !attackSettings.IsValid)
                return;

            float hitTime =
                _activeAttackClip.length *
                attackSettings.HitNormalizedTime;

            if (_attackElapsedTime < hitTime)
                return;

            _didHitCurrentAttack = true;
            ApplyAttackDamage(attackSettings);
        }

        // 공용 공간 탐지 결과 중 Damage 대상을 선별해 DamageSystem에 전달한다.
        private void ApplyAttackDamage(MeleeAttackSettings p_settings)
        {
            float damage = _baseDamage * p_settings.DamageMultiplier;

            if (damage <= 0f)
                return;

            EnsureHitBuffers();
            Physics.SyncTransforms();

            DetectionAreaRequest request = new(
                _attacker.position,
                _attacker.forward,
                _attacker.up,
                _attacker,
                p_settings.Area);

            int hitCount = DetectionAreaSystem.Query(
                request,
                _overlapBuffer,
                _hitBuffer);

            bool hasConfirmedHit = false;
            _damagedTargets.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];
                // 하나의 대상이 여러 Collider를 가져도 공격당 피해는 한 번만 적용한다.
                if (!DamageSystem.TryGetDamageable(
                        hit.Collider,
                        out IDamageable damageable) ||
                    !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                DamageInfo damageInfo = new(
                    _attacker,
                    damage,
                    hit.HitPoint,
                    -hit.Direction,
                    hit.Direction,
                    p_hitReaction:
                        p_settings.HitReaction,
                    p_deliveryType:
                        EDamageDeliveryType.Melee);

                if (!DamageSystem.TryApply(hit.Collider, damageInfo))
                    continue;

                hasConfirmedHit = true;

                // 각 접촉점이 아니라 공격 범위의 전방으로 함께 밀어낸다.
                Vector3 knockbackDirection = Vector3.ProjectOnPlane(
                    request.Forward,
                    Vector3.up);

                if (knockbackDirection.sqrMagnitude <= 0.0001f)
                    knockbackDirection = _attacker.forward;

                KnockbackInfo knockbackInfo = new(
                    _attacker,
                    knockbackDirection,
                    p_settings.KnockbackDistance,
                    p_settings.KnockbackDuration);

                KnockbackSystem.TryApply(
                    hit.Collider,
                    knockbackInfo);
            }

            // 다수의 적을 맞혀도 한 번의 공격에는 Effect 이벤트를 한 번만 보낸다.
            if (hasConfirmedHit)
                OnHitConfirmed?.Invoke(CurrentComboIndex);
        }

        // View가 콤보별 공격 범위를 미리 볼 수 있도록 읽기 전용 설정을 제공한다.
        public MeleeAttackSettings GetAttackSettings(int p_comboIndex)
        {
            if (_comboAttackSettings == null ||
                p_comboIndex < 0 ||
                p_comboIndex >= _comboAttackSettings.Length)
            {
                return null;
            }

            return _comboAttackSettings[p_comboIndex];
        }

        private void EnsureHitBuffers()
        {
            int capacity = Mathf.Max(1, _hitBufferCapacity);

            if (_overlapBuffer == null ||
                _overlapBuffer.Length != capacity)
            {
                _overlapBuffer = new Collider[capacity];
            }

            if (_hitBuffer == null ||
                _hitBuffer.Length != capacity)
            {
                _hitBuffer = new DetectionAreaHit[capacity];
            }
        }

        private void OnValidate()
        {
            _baseDamage = Mathf.Max(0f, _baseDamage);
            _hitBufferCapacity = Mathf.Max(1, _hitBufferCapacity);

            if (_comboAttackSettings == null)
                return;

            foreach (MeleeAttackSettings settings in _comboAttackSettings)
                settings?.Validate();
        }
    }
}
