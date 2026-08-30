using UnityEngine;
using Alpha.Combat;
using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using Alpha.Item.Weapon.Range;
using System;
using UnityEngine.Serialization;

namespace Alpha.Player.Combat
{
    [RequireComponent(typeof(WeaponSwapModule))]
    [RequireComponent(typeof(RangeAimModule))]
    public class CombatModule : MonoBehaviour
    {
        [Header("Attack Power")]
        [Tooltip("무기와 Skill이 계산한 공격력에 Player 능력치로 추가할 값입니다.")]
        [SerializeField, Min(0f)]
        private float _additionalAttackDamage;

        [Header("Melee Attack")]
        [FormerlySerializedAs("_meleeAttackModule")]
        [SerializeField]
        private PlayerMeleeWeaponUseModule _meleeWeaponUseModule = new();

        [Header("Range Attack")]
        [FormerlySerializedAs("_rangeAttackModule")]
        [SerializeField]
        private PlayerRangeWeaponUseModule _rangeWeaponUseModule = new();

        private PlayerCore _core;
        private WeaponSwapModule _weaponSwapModule;
        private RangeAimModule _rangeAimModule;
        private RangeWeapon _activeRangeSecondaryWeapon;

        public Transform Attacker => _core?.PlayerTr;

        // 실제 전투에 적용된 활성 무기의 변경을 외부 표현 계층에 알린다.
        public event Action<WeaponDTO> OnWeaponChanged;

        // 현재 원거리 무기의 발사 모드가 변경되면 UI 표현에 알린다.
        public event Action<ERangeTriggerMode> OnRangeTriggerModeChanged;

        // Player의 공격으로 피해가 실제 적용된 경우에만 명중 표현에 알린다.
        public event Action<DamageInfo> OnHitConfirmed;

        // 설정된 재생 시간에 도달한 Melee Effect를 Player 표현 계층에 요청한다.
        public event Action<MeleeSkillDefinition> OnMeleeSkillEffectRequested;

        // 하나의 Melee Skill이 한 명 이상의 대상을 맞힌 경우 Skill 자산과 함께 한 번만 알린다.
        public event Action<MeleeSkillDefinition> OnMeleeSkillHitConfirmed;

        // 현재 전투에 사용 가능한 무기를 대표 진입점으로 제공한다.
        public Weapon CurrentWeapon => _weaponSwapModule?.CurrentWeapon;
        public RangeWeapon CurrentRangeWeapon =>
            CurrentWeapon as RangeWeapon;
        public bool HasWeapon => CurrentWeapon != null;
        public int CurrentMeleeSkillIndex =>
            _meleeWeaponUseModule?.CurrentSkillIndex ?? -1;
        public MeleeSkillDefinition CurrentMeleeSkill =>
            _meleeWeaponUseModule?.CurrentSkill;
        public string CurrentMeleeSkillId =>
            _meleeWeaponUseModule?.CurrentSkillId;
        public string CurrentMeleeAnimationKey =>
            _meleeWeaponUseModule?.CurrentAnimationKey;
        public Transform MeleeAttackSource =>
            _meleeWeaponUseModule?.AttackSource;

        public EWeaponActionType ActiveActionType =>
            CurrentWeapon?.ActiveActionType ?? EWeaponActionType.None;

        public bool HasActiveAction =>
            CurrentWeapon != null && CurrentWeapon.HasActiveAction;

        public RangeWeapon ActiveRangeSecondaryWeapon =>
            _activeRangeSecondaryWeapon;

        public bool HasActiveRangeSecondary =>
            _activeRangeSecondaryWeapon != null &&
            _activeRangeSecondaryWeapon.IsSecondaryActive;

        private void Awake()
        {
            _meleeWeaponUseModule ??= new PlayerMeleeWeaponUseModule();
            _rangeWeaponUseModule ??= new PlayerRangeWeaponUseModule();
            _weaponSwapModule = GetComponent<WeaponSwapModule>();
            _rangeAimModule = GetComponent<RangeAimModule>();
        }

        private void OnValidate()
        {
            _additionalAttackDamage = Mathf.Max(0f, _additionalAttackDamage);
            _meleeWeaponUseModule ??= new PlayerMeleeWeaponUseModule();
            _rangeWeaponUseModule ??= new PlayerRangeWeaponUseModule();
        }

        private void OnEnable()
        {
            SubscribeDamageApplied();
        }

        private void OnDisable()
        {
            UnsubscribeDamageApplied();
        }

        // Player 전투 기능과 런타임 무기 생성 의존성을 연결한다.
        public bool Bind(PlayerCore p_core)
        {
            if (p_core == null ||
                _weaponSwapModule == null ||
                _rangeAimModule == null ||
                !_weaponSwapModule.Bind(p_core.ResourceLoader) ||
                !_rangeAimModule.Bind(p_core))
            {
                Debug.LogError($"{nameof(CombatModule)}의 참조가 설정되지 않았습니다.", this);

                return false;
            }

            _core = p_core;

            if (!_meleeWeaponUseModule.Bind(
                    Attacker,
                    HandleMeleeSkillEffectRequested,
                    HandleMeleeSkillHitConfirmed))
            {
                Debug.LogError(
                    $"{nameof(PlayerMeleeWeaponUseModule)}의 사용 기준을 설정하지 못했습니다.",
                    this);
                return false;
            }

            if (!_rangeWeaponUseModule.Bind(
                    Attacker,
                    _rangeAimModule))
            {
                Debug.LogError(
                    $"{nameof(PlayerRangeWeaponUseModule)}의 사용 기준을 설정하지 못했습니다.",
                    this);
                return false;
            }

            SubscribeDamageApplied();
            return true;
        }

        private void SubscribeDamageApplied()
        {
            if (_core == null || !isActiveAndEnabled)
                return;

            DamageSystem.OnDamageApplied -= HandleDamageApplied;
            DamageSystem.OnDamageApplied += HandleDamageApplied;
        }

        private void UnsubscribeDamageApplied()
        {
            DamageSystem.OnDamageApplied -= HandleDamageApplied;
        }

        // 공용 피해 성공 중 Player 또는 Player 자식이 공격자인 경우만 명중으로 확정한다.
        private void HandleDamageApplied(
            Collider p_target,
            DamageInfo p_damageInfo)
        {
            Transform player = _core?.PlayerTr;
            Transform attacker = p_damageInfo.Attacker;

            if (player == null ||
                attacker == null ||
                (attacker != player &&
                 !attacker.IsChildOf(player)))
            {
                return;
            }

            OnHitConfirmed?.Invoke(p_damageInfo);
        }

        private void HandleMeleeSkillHitConfirmed(
            MeleeSkillDefinition p_skill)
        {
            OnMeleeSkillHitConfirmed?.Invoke(p_skill);
        }

        private void HandleMeleeSkillEffectRequested(
            MeleeSkillDefinition p_skill)
        {
            OnMeleeSkillEffectRequested?.Invoke(p_skill);
        }

        #region ============================== Weapon Swap
        // 공통 무기 교체 요청을 실제 무기 생성 Module에 전달한다.
        public bool ApplyWeaponChange(WeaponDTO p_weapon)
        {
            // 기존 무기가 교체되기 전에 진행 중인 행동을 정리한다.
            CancelRangeSecondary();
            CancelWeaponAction();

            if (!_weaponSwapModule.Apply(p_weapon))
                return false;

            // 새 무기가 적용된 뒤 이전 Weapon 연결 상태를 정리한다.
            _meleeWeaponUseModule.UnbindCurrentWeapon();
            _rangeWeaponUseModule.UnbindCurrentWeapon();

            MeleeWeapon meleeWeapon = CurrentWeapon as MeleeWeapon;

            if (meleeWeapon != null &&
                !_meleeWeaponUseModule.TryBindWeapon(
                    meleeWeapon,
                    _additionalAttackDamage))
            {
                _weaponSwapModule.Apply(null);
                OnWeaponChanged?.Invoke(null);
                return false;
            }

            RangeWeapon rangeWeapon =
                CurrentRangeWeapon;

            if (rangeWeapon != null &&
                !_rangeWeaponUseModule.TryBindWeapon(
                    rangeWeapon,
                    _additionalAttackDamage))
            {
                // 잘못 구성된 Range 무기는 장착 상태로 남기지 않는다.
                _weaponSwapModule.Apply(null);
                OnWeaponChanged?.Invoke(null);
                return false;
            }

            OnWeaponChanged?.Invoke(CurrentWeapon?.Data);
            return true;
        }

        #endregion ============================== /Weapon Swap

        #region ============================== Range Trigger
        // Flow의 입력 요청을 현재 대표 Range 공격 Module에 전달한다.
        public bool TrySwitchRangeTriggerMode()
        {
            RangeWeapon rangeWeapon =
                CurrentRangeWeapon;

            if (rangeWeapon == null ||
                !rangeWeapon.TrySwitchTriggerMode())
            {
                return false;
            }

            OnRangeTriggerModeChanged?.Invoke(
                rangeWeapon.CurrentTriggerMode);
            return true;
        }
        #endregion ============================== /Range Trigger

        #region ============================== CombatAction
        // 현재 무기의 Action을 선택하고 행동을 시작한다.
        public bool TryBeginWeaponAction(EWeaponActionType p_type)
        {
            if (CurrentRangeWeapon != null &&
                !_rangeWeaponUseModule.RefreshAttackPose())
            {
                return false;
            }

            return CurrentWeapon != null &&
                   CurrentWeapon.TryBeginAction(p_type);
        }

        // 진행 중인 무기 행동을 갱신한다.
        public void TickWeaponAction(
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (CurrentRangeWeapon != null)
                _rangeWeaponUseModule.RefreshAttackPose();

            CurrentWeapon?.TickAction(
                p_isInputHeld,
                p_isInputPressed,
                p_deltaTime);
        }

        public void CancelWeaponAction()
        {
            CurrentWeapon?.CancelAction();
        }

        // 현재 MeleeWeapon이 참조하는 Skill 자산을 View에 읽기 전용으로 제공한다.
        public MeleeSkillDefinition GetMeleeSkillDefinition(
            int p_skillIndex)
        {
            return _meleeWeaponUseModule?.GetSkillDefinition(
                p_skillIndex);
        }

        // Player가 계산한 현재 조준 자세를 RangeWeapon과 공유한다.
        public bool TryGetRangeAttackPose(
            out Vector3 p_attackOrigin,
            out Vector3 p_attackDirection)
        {
            RangeWeapon rangeWeapon = CurrentRangeWeapon;

            if (rangeWeapon == null ||
                _rangeWeaponUseModule == null)
            {
                p_attackOrigin = Vector3.zero;
                p_attackDirection = Vector3.zero;
                return false;
            }

            return _rangeWeaponUseModule.TryGetAttackPose(
                rangeWeapon,
                out p_attackOrigin,
                out p_attackDirection);
        }

        // 현재 Range 공격 Module의 총구 기준 전체 조준 방향을 반환한다.
        public bool TryGetRangeAimDirection(
            out Vector3 p_direction)
        {
            p_direction = Vector3.zero;

            return TryGetRangeAttackPose(
                out _,
                out p_direction);
        }

        // 현재 Range 조준점을 Player의 지상 회전 방향으로 변환한다.
        public bool TryGetRangeFacingDirection(
            out Vector3 p_direction)
        {
            p_direction = Vector3.zero;

            if (_core == null ||
                !TryGetRangeAimDirection(
                    out Vector3 attackDirection))
            {
                return false;
            }

            p_direction = Vector3.ProjectOnPlane(
                attackDirection,
                Vector3.up);

            if (p_direction.sqrMagnitude <= 0.0001f)
                return false;

            p_direction.Normalize();
            return true;
        }
        #endregion ============================== /CombatAction

        #region ============================== Range Secondary
        public bool BeginRangeSecondary()
        {
            RangeWeapon rangeWeapon =
                CurrentRangeWeapon;

            if (rangeWeapon != null)
                _rangeWeaponUseModule.RefreshAttackPose();

            if (rangeWeapon == null ||
                _activeRangeSecondaryWeapon != null ||
                !rangeWeapon.BeginSecondary())
            {
                return false;
            }

            _activeRangeSecondaryWeapon = rangeWeapon;
            return true;
        }

        public void TickRangeSecondary(float p_deltaTime)
        {
            _rangeWeaponUseModule?.RefreshAttackPose();
            _activeRangeSecondaryWeapon?.TickSecondary(p_deltaTime);
        }

        public void CancelRangeSecondary()
        {
            _activeRangeSecondaryWeapon?.CancelSecondary();
            _activeRangeSecondaryWeapon = null;
        }
        #endregion ============================== /Range Secondary
    }
}
