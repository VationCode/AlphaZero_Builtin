using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using Alpha.Item.Weapon.Range;
using Alpha.Player.Equipment;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // ECombatStateType 관련 선택 값을 정의한다.
    public enum ECombatStateType
    {
        Idle,
        WeaponSwap,
        WeaponAction
    }

    // Player의 무기 교체와 조준 흐름을 판단한다.
    public class CombatFlow : MonoBehaviour
    {
        private PlayerCore _core;
        private readonly Dictionary<ECombatStateType, CombatStateBase> _stateDict = new();

        private ECameraViewType _secondaryReturnView;
        private bool _hasSecondaryReturnView;
        private float _rangeFacingHoldRemaining;
        private Vector3 _rangeFacingHoldDirection;

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

            TickFlow();
            UpdateRangeSecondary();
            UpdateRangeFacingHold(Time.deltaTime);
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
        }

        // 이전 State 종료 → Context 갱신 → 새 State 진입 순서로 전환한다.
        internal bool TryChangeState(ECombatStateType p_nextState)
        {
            if (!_stateDict.TryGetValue(p_nextState, out CombatStateBase nextState))
                return false;

            if (ReferenceEquals(CurrentState, nextState))
                return false;

            // 무기 교체가 현재 Range Secondary보다 먼저 기존 표현을 정리한다.
            if (p_nextState == ECombatStateType.WeaponSwap)
                CancelRangeSecondary();

            // 같은 State로의 중복 전환은 막고 기존 State를 먼저 종료한다.
            CurrentState?.ExitState();
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

            if (p_slotIndex < (int)EWeaponType.Melee ||
                p_slotIndex > (int)EWeaponType.Special)
                return false;

            EWeaponType weaponType = (EWeaponType)p_slotIndex;

            if (!_core.EquipmentContext.TryGetWeaponSlot(
                    weaponType,
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
                    currentWeapon.Data.WeaponType,
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

        // 실제 사격이 끝난 뒤 마지막 발사 방향을 지정된 시간만큼 유지한다.
        internal void BeginRangeFacingHold(float p_duration)
        {
            CombatContext context = _core?.CombatContext;

            if (context == null ||
                !context.HasAimDirection ||
                p_duration <= 0f)
            {
                EndRangeFacingHold();
                return;
            }

            _rangeFacingHoldRemaining = p_duration;
            _rangeFacingHoldDirection =
                context.AimDirection.normalized;
            context.SetRangeFacingHeld(true);
        }

        // 발사되지 않은 입력 행동이 끝나면 기존 사격 후 유지 방향을 복원한다.
        internal bool TryRestoreRangeFacingHold()
        {
            CombatContext context = _core?.CombatContext;

            if (context == null ||
                !context.IsRangeFacingHeld ||
                _rangeFacingHoldDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            context.SetAimDirection(
                _rangeFacingHoldDirection);
            _core.AnimationView?.SetRangeAimDirection(
                _rangeFacingHoldDirection);

            return true;
        }

        private void UpdateRangeFacingHold(float p_deltaTime)
        {
            CombatContext context = _core?.CombatContext;

            if (context == null || !context.IsRangeFacingHeld)
                return;

            if (context.IsRangeAttacking ||
                _core.BlockCombat ||
                _core.CombatModule.CurrentRangeWeapon == null ||
                CurrentState?.Type == ECombatStateType.WeaponSwap ||
                !CanUseRangeAttack())
            {
                EndRangeFacingHold();
                return;
            }

            _rangeFacingHoldRemaining -= Mathf.Max(0f, p_deltaTime);

            if (_rangeFacingHoldRemaining <= 0f)
                EndRangeFacingHold();
        }

        private void EndRangeFacingHold()
        {
            _rangeFacingHoldRemaining = 0f;
            _rangeFacingHoldDirection = Vector3.zero;

            CombatContext context = _core?.CombatContext;

            if (context == null)
                return;

            context.SetRangeFacingHeld(false);

            if (context.IsAiming ||
                context.IsRangePrimaryActive ||
                context.IsRangeAttacking)
                return;

            context.ClearAimDirection();
            _core.AnimationView?.ClearRangeAimDirection();
        }

        // Idle에서 들어온 무기 행동 요청을 검증하고 State가 소비할 값으로 저장한다.
        public bool RequestWeaponAction(EWeaponActionType p_actionType)
        {
            if (_core == null ||
                _core.BlockCombat ||
                CurrentState?.Type != ECombatStateType.Idle ||
                !_core.CombatModule.HasWeapon ||
                p_actionType == EWeaponActionType.None)
            {
                return false;
            }

            if (p_actionType == EWeaponActionType.Primary &&
                _core.CombatModule.CurrentRangeWeapon != null &&
                !CanUseRangeAttack())
            {
                return false;
            }

            // Range Secondary는 Primary와 동시에 유지되므로 별도 흐름이 처리한다.
            if (p_actionType == EWeaponActionType.Secondary &&
                _core.CombatModule.CurrentRangeWeapon != null)
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
            RangeWeapon currentRangeWeapon = module.CurrentRangeWeapon;

            bool canUseSecondary =
                _core.Input != null &&
                !_core.BlockCombat &&
                CurrentState?.Type != ECombatStateType.WeaponSwap &&
                currentRangeWeapon != null &&
                (currentRangeWeapon.SecondaryType != ERangeSecondaryType.Charging ||
                 CanUseRangeAttack());

            if (module.HasActiveRangeSecondary)
            {
                if (!canUseSecondary ||
                    currentRangeWeapon != module.ActiveRangeSecondaryWeapon)
                {
                    CancelRangeSecondary();
                    return;
                }

                if (!_core.Input.IsSecondaryAction)
                {
                    EndRangeSecondary();
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

            BeginRangeSecondaryPresentation(currentRangeWeapon);
        }

        private void BeginRangeSecondaryPresentation(RangeWeapon p_rangeWeapon)
        {
            SetRangeAiming(true);

            if (_core.CameraCore == null ||
                !TryResolveSecondaryView(p_rangeWeapon.SecondaryView, out ECameraViewType targetView))
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

        // 정상 해제는 Charging 결과를 실행한 뒤 Player 표현을 복구한다.
        private void EndRangeSecondary()
        {
            _core.CombatModule.EndRangeSecondary();
            EndRangeSecondaryPresentation();
        }

        // 전투 차단과 무기 교체에서는 Charging 결과 없이 정리한다.
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
        private static bool TryResolveSecondaryView(
            ERangeSecondaryView p_secondaryView,
            out ECameraViewType p_cameraView)
        {
            switch (p_secondaryView)
            {
                case ERangeSecondaryView.Aim:
                    p_cameraView = ECameraViewType.Aim;
                    return true;

                case ERangeSecondaryView.Scope:
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

            if (!p_isAiming &&
                !context.IsRangePrimaryActive &&
                !context.IsRangeAttacking &&
                !context.IsRangeFacingHeld)
            {
                context.ClearAimDirection();
                _core.AnimationView?.ClearRangeAimDirection();
            }

            _core.AnimationView?.SetRangeAiming(p_isAiming);
        }
        #endregion ============================== /Range Secondary
    }
}
