using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using Alpha.Item.Weapon.Range;
using Alpha.Player.Equipment;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Player.Combat
{
    // ECombatStateType 관련 선택 값을 정의한다.
    public enum ECombatStateType
    {
        Idle,
        WeaponSwap,
        WeaponAction
    }

    // ActionFlow가 Combat을 허용하는 동안 공격 요청과 무기 행동의 상태 전환을 조정한다.
    // 실제 공격·무기 교체 처리는 대표 CombatModule과 현재 Weapon에 맡긴다.
    public class CombatFlow : MonoBehaviour
    {
        [Header("Combat Stance")]
        [Tooltip("마지막 무기 행동 후 공통 전투 태세 유지 시간입니다. 0이면 무제한으로 유지합니다.")]
        [FormerlySerializedAs("_rangeCombatDuration")]
        [SerializeField, Min(0f)]
        private float _combatDuration = 3f;

        private PlayerCore _core;
        private readonly Dictionary<ECombatStateType, CombatStateBase> _stateDict = new();

        private ECameraViewType _secondaryReturnView;
        private bool _hasSecondaryReturnView;
        private float _combatElapsedTime;

        public CombatStateBase CurrentState { get; private set; }

        // Player 전투 참조를 연결하고 Idle 상태로 State Flow를 시작한다.
        public void Bind(PlayerCore p_core)
        {
            if (p_core == null || p_core.CombatModule == null)
            {
                Debug.LogError($"{nameof(CombatFlow)}의 참조가 설정되지 않았습니다.", this);
                return;
            }

            _core = p_core;

            // 모든 State를 새로 구성한 뒤 기본 Idle 상태에 진입한다.
            InitializeStates();
            EnterFlow(ECombatStateType.Idle);
        }

        // 이전 State를 종료하고 Combat State 인스턴스를 다시 등록한다.
        private void InitializeStates()
        {
            ExitFlow();
            _stateDict.Clear();

            RegisterState(new CombatIdleState(_core, this));
            RegisterState(new WeaponSwapState(_core, this));
            RegisterState(new WeaponActionState(_core, this));
        }

        // 매 프레임 입력과 현재 상태를 갱신한다.
        private void Update()
        {
            if (_core == null)
                return;

            UpdateRangeTriggerMode();
            UpdateRangeSecondary();
            TickFlow();
            UpdateCombatStance();
        }

        // 발사 모드 변경 입력을 해석하고 현재 Range 공격 Module에 위임한다.
        private void UpdateRangeTriggerMode()
        {
            AlphaInputSystem input = _core.Input;
            RangeAttackModule rangeAttackModule =
                _core.CombatModule.CurrentRangeAttackModule;

            if (input == null ||
                !input.IsTriggerModeSwitchInput ||
                !_core.CanUseCombat ||
                CurrentState?.Type == ECombatStateType.WeaponSwap ||
                rangeAttackModule == null)
            {
                return;
            }

            _core.CombatModule.TrySwitchRangeTriggerMode();
        }

        // State 타입 중복 없이 Flow Dictionary에 등록한다.
        internal bool RegisterState(CombatStateBase p_state)
        {
            if (p_state == null || _stateDict.ContainsKey(p_state.Type))
                return false;

            _stateDict.Add(p_state.Type, p_state);
            return true;
        }

        // 지정된 시작 State로 Combat Flow를 진입시킨다.
        internal bool EnterFlow(ECombatStateType p_entryState)
        {
            return TryChangeState(p_entryState);
        }

        // 현재 Combat State의 프레임 갱신을 실행한다.
        internal void TickFlow()
        {
            CurrentState?.TickState();
        }

        // 현재 State를 종료하고 활성 상태를 비운다.
        internal void ExitFlow()
        {
            CancelRangeSecondary();
            CurrentState?.ExitState();
            CurrentState = null;
            EndCombatStance();
        }

        // 이전 State 종료 → Context 갱신 → 새 State 진입 순서로 전환한다.
        internal bool TryChangeState(ECombatStateType p_nextState)
        {
            if (!_stateDict.TryGetValue(p_nextState, out CombatStateBase nextState))
                return false;

            if (ReferenceEquals(CurrentState, nextState))
                return false;

            bool isWeaponSwap =
                p_nextState == ECombatStateType.WeaponSwap;

            // 무기 교체가 현재 Range Secondary보다 먼저 기존 표현을 정리한다.
            if (isWeaponSwap)
                CancelRangeSecondary();

            // 같은 State로의 중복 전환은 막고 기존 State를 먼저 종료한다.
            CurrentState?.ExitState();

            // WeaponAction 종료가 공통 전투 태세를 갱신한 뒤 교체 상태에서 정리한다.
            if (isWeaponSwap)
                EndCombatStance();

            CurrentState = nextState;

            _core.CombatContext.SetCurrentState(nextState.Type);
            CurrentState.EnterState();

            return true;
        }

        #region ============================== Swap
        // 숫자키로의 변경, 장비창으로의 변경

        // 숫자 키를 장비 슬롯의 WeaponDTO로 변환해 공통 교체 요청으로 전달한다.
        internal bool RequestKeyWeaponSwap(int p_slotIndex)
        {
            if (_core == null)
                return false;

            if (p_slotIndex < (int)EWeaponCategory.Melee ||
                p_slotIndex > (int)EWeaponCategory.Special)
                return false;

            EWeaponCategory weaponCategory =
                (EWeaponCategory)p_slotIndex;

            if (!_core.EquipmentContext.TryGetWeaponSlot(
                    weaponCategory,
                    out WeaponEquipmentSlot weaponSlot) ||
                weaponSlot.IsEmpty)
                return false;

            return RequestEquipWeaponSwap(weaponSlot.Weapon);
        }

        // 인벤토리 장착과 숫자 키 선택이 공유하는 무기 교체 진입점이다.
        // true는 요청 접수이며 즉시 상태 전환됐다는 의미는 아니다.
        private bool RequestEquipWeaponSwap(WeaponDTO p_weapon)
        {
            if (_core == null || _core.CombatModule == null)
                return false;

            CombatContext context = _core.CombatContext;
            WeaponDTO currentWeapon = _core.CombatModule.CurrentWeapon?.Data;

            if (context.HasPendingWeaponChange)
            {
                // 같은 대기 요청은 다시 저장하지 않는다.
                if (ReferenceEquals(context.PendingWeapon, p_weapon))
                    return false;

                // 현재 무기를 다시 선택하면 기존 대기 요청만 취소한다.
                if (ReferenceEquals(currentWeapon, p_weapon))
                {
                    context.ClearPendingWeaponChange();
                    return true;
                }
            }
            else if (ReferenceEquals(currentWeapon, p_weapon)) // 대기 요청이 없다면 현재 무기와 같은 요청은 무시한다.
            {
                return false;
            }

            // 상태 전환은 하지 않고 요청만 저장한다.
            context.SetPendingWeaponChange(p_weapon);

            return true;
        }

        // 장비 슬롯 변경을 Combat 무기 교체 요청으로 해석한다.
        internal void HandleEquipmentWeaponChanged(WeaponDTO p_weapon)
        {
            // 장착 또는 교환이면 해당 무기로 교체한다.
            if (p_weapon != null)
            {
                RequestEquipWeaponSwap(p_weapon);
                return;
            }

            Weapon currentWeapon = _core.CombatModule.CurrentWeapon;

            if (currentWeapon?.Data == null)
                return;

            // 현재 사용 중인 무기의 장비 슬롯을 확인한다.
            if (!_core.EquipmentContext.TryGetWeaponSlot(
                    currentWeapon.Data.WeaponCategory,
                    out WeaponEquipmentSlot currentSlot))
            {
                return;
            }

            // 현재 무기의 장비 슬롯이 비었을 때만 실제 무기를 해제한다.
            if (currentSlot.IsEmpty)
                RequestEquipWeaponSwap(null);
        }

        #endregion ============================== /Swap

        #region ============================== Weapon Action

        // Locomotion이 Idle 또는 Move일 때만 실제 Range 공격을 허용한다.
        internal bool CanUseRangeAttack()
        {
            if (_core == null ||
                _core.LocomotionContext == null ||
                _core.Input?.IsJump == true ||
                _core.Input?.IsDash == true)
            {
                return false;
            }

            ELocoStateType? locomotionState =
                _core.LocomotionContext.CurrentState;

            return locomotionState == ELocoStateType.Idle ||
                   locomotionState == ELocoStateType.Move;
        }

        // 실제 좌·우 무기 행동이 시작되면 현재 무기 유형의 공통 전투 태세를 활성화한다.
        internal void BeginCombatStance()
        {
            CombatContext context = _core?.CombatContext;
            CombatModule module = _core?.CombatModule;

            if (context == null || module?.CurrentWeapon == null)
            {
                return;
            }

            bool isRange = module.CurrentRangeAttackModule != null;
            Vector3 rangeDirection = Vector3.zero;

            if (isRange)
            {
                if (!module.TryGetRangeAimDirection(out rangeDirection))
                {
                    rangeDirection = context.HasAimDirection
                        ? context.AimDirection
                        : context.RangeCombatDirection;
                }

                if (rangeDirection.sqrMagnitude > 0.0001f)
                {
                    context.SetAimDirection(rangeDirection);
                    _core.RigView?.SetAimDirection(rangeDirection);
                }
            }

            context.EnterCombatStance(
                isRange
                    ? ECombatStanceType.Range
                    : ECombatStanceType.Melee,
                rangeDirection);
            _combatElapsedTime = 0f;
            RefreshRangeAimRigPresentation();
        }

        // 전투 태세 중에는 현재 Camera 조준 방향을 우선하고, 계산 실패 시 마지막 사격 방향을 복원한다.
        internal bool TryRestoreRangeCombatDirection()
        {
            CombatContext context = _core?.CombatContext;

            if (context == null || !context.IsRangeCombatActive)
            {
                return false;
            }

            Vector3 aimDirection;

            if (!_core.CombatModule.TryGetRangeAimDirection(out aimDirection))
            {
                aimDirection = context.RangeCombatDirection;
            }

            if (aimDirection.sqrMagnitude <= 0.0001f)
                return false;

            context.SetAimDirection(aimDirection);
            _core.RigView?.SetAimDirection(aimDirection);

            return true;
        }

        // 전투 불가 조건 또는 Inspector에서 설정한 유지 시간이 지나면 공통 전투 태세를 종료한다.
        private void UpdateCombatStance()
        {
            CombatContext context = _core?.CombatContext;
            CombatModule module = _core?.CombatModule;

            if (context == null ||
                module == null ||
                !context.IsCombatStanceActive)
            {
                return;
            }

            if (!_core.CanUseCombat ||
                !module.HasWeapon ||
                CurrentState?.Type == ECombatStateType.WeaponSwap ||
                (context.IsRangeCombatActive &&
                 (module.CurrentRangeAttackModule == null ||
                  !CanUseRangeAttack())))
            {
                EndCombatStance();
                return;
            }

            // 좌·우 행동 또는 Range Secondary가 유지되는 동안에는 유지 시간을 소모하지 않는다.
            if (_combatDuration <= 0f ||
                CurrentState?.Type == ECombatStateType.WeaponAction ||
                module.HasActiveAction ||
                module.HasActiveRangeSecondary ||
                context.IsAiming ||
                context.IsRangePrimaryActive ||
                context.IsRangeAttacking)
            {
                return;
            }

            _combatElapsedTime += Time.deltaTime;

            if (_combatElapsedTime >= _combatDuration)
                EndCombatStance();
        }

        private void EndCombatStance()
        {
            CombatContext context = _core?.CombatContext;

            if (context == null)
                return;

            bool wasRangeCombat = context.IsRangeCombatActive;

            context.ExitCombatStance();
            _combatElapsedTime = 0f;

            if (!wasRangeCombat)
                return;

            if (context.IsAiming ||
                context.IsRangePrimaryActive ||
                context.IsRangeAttacking)
                return;

            context.ClearAimDirection();
            _core.RigView?.ClearAimDirection();
            RefreshRangeAimRigPresentation();
        }

        // Idle에서 들어온 무기 행동 요청을 검증하고 State가 소비할 값으로 저장한다.
        public bool RequestWeaponAction(EWeaponActionType p_actionType)
        {
            if (_core == null ||
                !_core.CanUseCombat ||
                CurrentState?.Type != ECombatStateType.Idle ||
                !_core.CombatModule.HasWeapon ||
                p_actionType == EWeaponActionType.None)
            {
                return false;
            }

            RangeAttackModule currentRangeAttackModule =
                _core.CombatModule.CurrentRangeAttackModule;

            if (p_actionType == EWeaponActionType.Primary &&
                currentRangeAttackModule != null &&
                !CanUseRangeAttack())
            {
                return false;
            }

            // Range Secondary는 Primary와 동시에 유지되므로 별도 흐름이 처리한다.
            if (p_actionType == EWeaponActionType.Secondary &&
                currentRangeAttackModule != null)
            {
                return false;
            }

            // 근접 전신 행동은 지상 Move 상태에서만 시작한다.
            if (_core.CombatModule.CurrentWeapon is MeleeWeapon &&
                (_core.LocomotionContext.CurrentMode != ELocomotionMode.Ground ||
                 _core.LocomotionContext.CurrentState != ELocoStateType.Move))
            {
                return false;
            }

            _core.CombatContext.SetPendingWeaponAction(p_actionType);
            return true;
        }

        #endregion ============================== /Weapon Action

        #region ============================== Range Secondary
        // 우클릭의 시작·유지·해제를 배타적인 WeaponAction과 별도로 처리한다.
        private void UpdateRangeSecondary()
        {
            CombatModule module = _core.CombatModule;
            RangeAttackModule currentRangeAttackModule =
                module.CurrentRangeAttackModule;

            bool canUseSecondary =
                _core.Input != null &&
                _core.CanUseCombat &&
                CurrentState?.Type != ECombatStateType.WeaponSwap &&
                currentRangeAttackModule != null &&
                currentRangeAttackModule.HasSecondaryAction &&
                (!currentRangeAttackModule.IsChargeEnabled ||
                 CanUseRangeAttack());

            if (module.HasActiveRangeSecondary)
            {
                if (!canUseSecondary ||
                    currentRangeAttackModule !=
                    module.ActiveRangeSecondaryModule)
                {
                    CancelRangeSecondary();
                    return;
                }

                if (!_core.Input.IsSecondaryAction)
                {
                    // 우클릭 해제는 발사하지 않고 차징과 조준 표현만 취소한다.
                    CancelRangeSecondary();
                    return;
                }

                module.TickRangeSecondary(Time.deltaTime);
                return;
            }

            if (!canUseSecondary ||
                !_core.Input.IsSecondaryAction ||
                !module.BeginRangeSecondary())
            {
                return;
            }

            BeginCombatStance();
            BeginRangeSecondaryPresentation(currentRangeAttackModule);
        }

        private void BeginRangeSecondaryPresentation(
            RangeAttackModule p_rangeAttackModule)
        {
            SetRangeAiming(true);

            if (_core.CameraCore == null ||
                !TryResolveAimView(
                    p_rangeAttackModule.AimView,
                    out ECameraViewType targetView))
            {
                return;
            }

            // 전환 이 완료되면 targetView로 그렇지 않으면 이전뷰 가지고 있는 상태
            ECameraViewType effectiveView = _core.CameraCore.Context.EffectiveViewType;

            // 우클릭 해제 시 저장한 이전뷰로 전환
            if (effectiveView != targetView)
            {
                _secondaryReturnView = effectiveView;
                _hasSecondaryReturnView = true;
            }

            // 전환 중이어도 최신 목표를 전달한다.
            _core.CameraCore.RequestView(targetView);
        }

        // 우클릭 해제, 전투 차단, 무기 교체 시 차징 결과 없이 정리한다.
        private void CancelRangeSecondary()
        {
            if (_core == null || _core.CombatModule == null)
                return;

            _core.CombatModule.CancelRangeSecondary();
            EndRangeSecondaryPresentation();
        }

        private void EndRangeSecondaryPresentation()
        {
            SetRangeAiming(false);

            if (_hasSecondaryReturnView && _core.CameraCore != null)
            {
                _core.CameraCore.RequestView(_secondaryReturnView);
            }

            _hasSecondaryReturnView = false;
        }

        // CombatContext를 단일 조준 상태로 사용하고 View는 표현만 갱신한다.
        // Item의 View 설정을 실제 Camera Entity의 ViewType으로 변환한다.
        private static bool TryResolveAimView(
            ERangeAimView p_aimView,
            out ECameraViewType p_cameraView)
        {
            switch (p_aimView)
            {
                case ERangeAimView.Aim:
                    p_cameraView = ECameraViewType.Aim;
                    return true;

                case ERangeAimView.Scope:
                    p_cameraView = ECameraViewType.Scope;
                    return true;

                default:
                    p_cameraView = default;
                    return false;
            }
        }

        private void SetRangeAiming(bool p_isAiming)
        {
            CombatContext context = _core.CombatContext;

            if (context.IsAiming != p_isAiming)
                context.SetAiming(p_isAiming);

            if (!p_isAiming && context.IsRangeCombatActive)
            {
                TryRestoreRangeCombatDirection();
            }
            else if (!p_isAiming &&
                     !context.IsRangePrimaryActive &&
                     !context.IsRangeAttacking)
            {
                context.ClearAimDirection();
                _core.RigView?.ClearAimDirection();
            }

            RefreshRangeAimRigPresentation();
        }

        // Camera View와 무관하게 Range 조준 또는 공격 상태를 상체 Rig 표현으로 변환한다.
        internal void RefreshRangeAimRigPresentation()
        {
            if (_core?.CombatContext == null ||
                _core.CombatModule == null)
            {
                return;
            }

            CombatContext context = _core.CombatContext;

            bool shouldActivate =
                _core.CombatModule.CurrentRangeAttackModule != null &&
                (context.IsAiming ||
                 context.IsRangePrimaryActive ||
                 context.IsRangeAttacking ||
                 context.IsRangeCombatActive);

            _core.RigView?.SetAiming(shouldActivate);
        }

        #endregion ============================== /Range Secondary

        private void OnValidate()
        {
            _combatDuration = Mathf.Max(0f, _combatDuration);
        }
    }
}
