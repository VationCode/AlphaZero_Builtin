using Alpha.Combat;
using Alpha.Detection;
using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player의 콤보 하나가 사용할 시간, 피해 보정, 피격 종류와 공격 범위를 보관한다.
    [Serializable]
    public sealed class PlayerMeleeAttackSettings
    {
        [Tooltip("공격 하나가 유지되는 전체 시간(초)입니다.")]
        [SerializeField, Min(0.01f)]
        private float _duration = 0.6f;

        [Tooltip("공격 시작 후 실제 타격 판정을 실행할 시간(초)입니다.")]
        [SerializeField, Min(0f)]
        private float _hitTime = 0.2f;

        [Tooltip("다음 콤보 입력을 예약할 수 있는 시작 시간(초)입니다.")]
        [SerializeField, Min(0f)]
        private float _comboInputTime = 0.48f;

        [Tooltip("MeleeWeapon의 기본 공격력에 곱할 콤보·스킬 배율입니다.")]
        [SerializeField, Min(0f)]
        private float _damageMultiplier = 1f;

        [SerializeField]
        private AttackImpactSettings _impactSettings = new();

        [SerializeField]
        private DetectionAreaSettings _area = new();

        public float Duration => _duration;
        public float HitTime => _hitTime;
        public float ComboInputTime => _comboInputTime;
        public float DamageMultiplier => _damageMultiplier;
        public AttackImpactInfo Impact =>
            _impactSettings?.CreateInfo() ?? default;
        public DetectionAreaSettings Area => _area;

        public bool IsValid =>
            _duration > 0f &&
            _hitTime >= 0f &&
            _hitTime <= _duration &&
            _comboInputTime >= 0f &&
            _comboInputTime <= _duration &&
            _damageMultiplier > 0f &&
            _impactSettings != null &&
            _area != null &&
            _area.IsValid;

        public void Validate()
        {
            _duration = Mathf.Max(0.01f, _duration);
            _hitTime = Mathf.Clamp(_hitTime, 0f, _duration);
            _comboInputTime = Mathf.Clamp(
                _comboInputTime,
                0f,
                _duration);
            _damageMultiplier = Mathf.Max(0f, _damageMultiplier);
            _impactSettings ??= new AttackImpactSettings();
            _impactSettings.Validate();
            _area ??= new DetectionAreaSettings();
            _area.Validate();
        }
    }

    // Animator 상태 Name과 해당 상태에서 실행할 Player 공격 설정을 묶는다.
    [Serializable]
    public sealed class PlayerMeleeComboSettings
    {
        [Tooltip("Player Animator의 Weapon FullBody Layer에 등록한 상태 이름입니다.")]
        [SerializeField]
        private string _name;

        [SerializeField]
        private PlayerMeleeAttackSettings _attackSettings = new();

        public string Name => _name?.Trim();
        public PlayerMeleeAttackSettings AttackSettings => _attackSettings;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Name) &&
            _attackSettings != null &&
            _attackSettings.IsValid;

        public void Validate()
        {
            _name = _name?.Trim();
            _attackSettings ??= new PlayerMeleeAttackSettings();
            _attackSettings.Validate();
        }
    }

    // Player의 Melee 콤보 진행과 공간 판정 및 최종 피해 조합을 수행한다.
    [Serializable]
    public sealed class PlayerMeleeAttackModule : IMeleeWeaponActionController
    {
        [Header("Combo")]
        [SerializeField]
        private EWeaponInputMode _primaryInputMode = EWeaponInputMode.Auto;

        [Tooltip("콤보가 끊긴 뒤 다음 순서를 기억하는 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _comboGraceDuration = 0.5f;

        [Tooltip("배열 순서는 진행 순서이며 Name은 Animator 상태와 View 매칭에 사용합니다.")]
        [SerializeField]
        private PlayerMeleeComboSettings[] _comboSettings;

        [Header("Detection")]
        [SerializeField, Min(1)]
        private int _hitBufferCapacity = 16;

        public int CurrentComboIndex { get; private set; } = -1;
        public string CurrentComboName =>
            GetComboSettings(CurrentComboIndex)?.Name;
        public bool IsGuarding { get; private set; }
        public Transform AttackSource => _attacker;

        private Transform _attacker;
        private Func<float, float> _damageResolver;
        private Action<string> _hitConfirmed;
        private MeleeWeapon _activeWeapon;
        private PlayerMeleeComboSettings _activeComboSettings;
        private float _attackElapsedTime;
        private bool _didHitCurrentAttack;

        private Collider[] _overlapBuffer;
        private DetectionAreaHit[] _hitBuffer;
        private readonly HashSet<IDamageable> _damagedTargets = new();

        private bool _isNextComboQueued;
        private int _rememberedComboIndex = -1;
        private float _comboExpireTime;

        public bool Bind(
            Transform p_attacker,
            Func<float, float> p_damageResolver,
            Action<string> p_hitConfirmed)
        {
            if (p_attacker == null ||
                p_damageResolver == null ||
                p_hitConfirmed == null)
            {
                return false;
            }

            _attacker = p_attacker;
            _damageResolver = p_damageResolver;
            _hitConfirmed = p_hitConfirmed;
            EnsureHitBuffers();
            return true;
        }

        public bool EndsOnInputRelease(EWeaponActionType p_type)
        {
            return p_type == EWeaponActionType.Secondary;
        }

        public bool TryBeginAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type)
        {
            if (p_weapon == null || _attacker == null)
                return false;

            switch (p_type)
            {
                case EWeaponActionType.Primary:
                    return TryStartCombo(
                        p_weapon,
                        GetPrimaryStartComboIndex());

                case EWeaponActionType.Secondary:
                    ClearComboMemory();
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
                _activeComboSettings == null)
            {
                return;
            }

            _attackElapsedTime += Mathf.Max(0f, p_deltaTime);
            TryExecuteAttackHit();

            PlayerMeleeAttackSettings attackSettings =
                _activeComboSettings.AttackSettings;
            int nextComboIndex = CurrentComboIndex + 1;
            bool isLastCombo = GetComboSettings(nextComboIndex) == null;

            if (isLastCombo)
                nextComboIndex = 0;

            bool wantsNextCombo = IsActionInput(
                _primaryInputMode,
                p_isInputHeld,
                p_isInputPressed);

            if (_attackElapsedTime < attackSettings.Duration)
            {
                if (!_isNextComboQueued &&
                    _attackElapsedTime >= attackSettings.ComboInputTime &&
                    wantsNextCombo)
                {
                    _isNextComboQueued = true;
                }

                return;
            }

            if ((_isNextComboQueued || wantsNextCombo) &&
                TryStartCombo(p_weapon, nextComboIndex))
            {
                return;
            }

            if (isLastCombo)
                ClearComboMemory();
            else
                RememberNextCombo(nextComboIndex);

            p_weapon.EndAction();
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
            ClearComboMemory();
        }

        public PlayerMeleeComboSettings GetComboSettings(int p_comboIndex)
        {
            if (_comboSettings == null ||
                p_comboIndex < 0 ||
                p_comboIndex >= _comboSettings.Length)
            {
                return null;
            }

            return _comboSettings[p_comboIndex];
        }

        public PlayerMeleeAttackSettings GetAttackSettings(int p_comboIndex)
        {
            return GetComboSettings(p_comboIndex)?.AttackSettings;
        }

        private bool TryStartCombo(
            MeleeWeapon p_weapon,
            int p_comboIndex)
        {
            PlayerMeleeComboSettings comboSettings =
                GetComboSettings(p_comboIndex);

            if (comboSettings == null ||
                !comboSettings.IsValid ||
                p_weapon.BaseDamage <= 0f)
            {
                return false;
            }

            _activeWeapon = p_weapon;
            _activeComboSettings = comboSettings;
            CurrentComboIndex = p_comboIndex;
            _attackElapsedTime = 0f;
            _didHitCurrentAttack = false;
            _isNextComboQueued = false;
            ClearComboMemory();

            p_weapon.NotifyComboStarted(comboSettings.Name);
            return true;
        }

        private void TryExecuteAttackHit()
        {
            if (_didHitCurrentAttack ||
                _activeWeapon == null ||
                _activeComboSettings == null ||
                _attacker == null)
            {
                return;
            }

            PlayerMeleeAttackSettings attackSettings =
                _activeComboSettings.AttackSettings;

            if (attackSettings == null ||
                !attackSettings.IsValid ||
                _attackElapsedTime < attackSettings.HitTime)
            {
                return;
            }

            _didHitCurrentAttack = true;
            ApplyAttackDamage(attackSettings);
        }

        private void ApplyAttackDamage(
            PlayerMeleeAttackSettings p_settings)
        {
            float weaponDamage =
                _activeWeapon.BaseDamage * p_settings.DamageMultiplier;
            float damage = _damageResolver != null
                ? _damageResolver.Invoke(weaponDamage)
                : weaponDamage;

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

                if (!DamageSystem.TryGetDamageable(
                        hit.Collider,
                        out IDamageable damageable) ||
                    !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                Vector3 hitDirection = Vector3.ProjectOnPlane(
                    request.Forward,
                    Vector3.up);

                if (hitDirection.sqrMagnitude <= 0.0001f)
                    hitDirection = _attacker.forward;

                DamageInfo damageInfo = new(
                    _attacker,
                    damage,
                    hit.HitPoint,
                    -hit.Direction,
                    hitDirection,
                    p_impact: p_settings.Impact,
                    p_deliveryType: EDamageDeliveryType.Melee);

                if (DamageSystem.TryApply(hit.Collider, damageInfo))
                    hasConfirmedHit = true;
            }

            if (hasConfirmedHit)
                _hitConfirmed?.Invoke(CurrentComboName);
        }

        private int GetPrimaryStartComboIndex()
        {
            if (_rememberedComboIndex >= 0 &&
                Time.time <= _comboExpireTime &&
                GetComboSettings(_rememberedComboIndex) != null)
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

        private void ResetActiveAction(
            MeleeWeapon p_weapon,
            EWeaponActionType p_type)
        {
            if (!ReferenceEquals(p_weapon, _activeWeapon))
                return;

            if (p_type == EWeaponActionType.Primary)
            {
                CurrentComboIndex = -1;
                _activeComboSettings = null;
                _attackElapsedTime = 0f;
                _didHitCurrentAttack = false;
                _isNextComboQueued = false;
            }

            if (p_type == EWeaponActionType.Secondary)
                IsGuarding = false;

            _activeWeapon = null;
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

        private static bool IsActionInput(
            EWeaponInputMode p_inputMode,
            bool p_isInputHeld,
            bool p_isInputPressed)
        {
            return p_inputMode == EWeaponInputMode.Auto
                ? p_isInputHeld
                : p_isInputPressed;
        }

        public void Validate()
        {
            _comboGraceDuration = Mathf.Max(0f, _comboGraceDuration);
            _hitBufferCapacity = Mathf.Max(1, _hitBufferCapacity);

            if (_comboSettings == null)
                return;

            HashSet<string> comboNames = new(StringComparer.Ordinal);

            foreach (PlayerMeleeComboSettings comboSettings in _comboSettings)
            {
                comboSettings?.Validate();

                if (comboSettings == null ||
                    string.IsNullOrWhiteSpace(comboSettings.Name))
                {
                    continue;
                }

                if (!comboNames.Add(comboSettings.Name))
                {
                    Debug.LogWarning(
                        $"Player Melee Combo Name이 중복되었습니다: {comboSettings.Name}");
                }
            }
        }
    }
}
