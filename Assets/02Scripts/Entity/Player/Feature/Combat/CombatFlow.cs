using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
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
            //UpdateAimState();
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

            // 지상 Root Motion을 사용하는 근접 공격은 Ground Move에서만 시작한다.
            if (p_actionType == EWeaponActionType.Primary &&
                _core.CombatModule.CurrentWeapon is MeleeWeapon &&
                (_core.LocomotionContext.CurrentMode != ELocomotionMode.Ground ||
                 _core.LocomotionContext.CurrentState != ELocoStateType.Move))
            {
                return false;
            }

            _core.CombatContext.SetPendingWeaponAction(p_actionType);
            return true;
        }

        #endregion ============================== /Weapon Action
    }
}
