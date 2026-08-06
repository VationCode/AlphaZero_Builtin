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
        public bool IsBound { get; private set; }

        // Player 전투 참조를 연결하고 Idle 상태로 State Flow를 시작한다.
        public void Bind(PlayerCore p_core)
        {
            if (p_core == null ||
                p_core.CombatModule == null ||
                !p_core.CombatModule.IsBound)
            {
                Debug.LogError($"{nameof(CombatFlow)}의 참조가 설정되지 않았습니다.", this);
                return;
            }

            _core = p_core;

            // 모든 State를 새로 구성한 뒤 기본 Idle 상태에 진입한다.
            InitializeStates();
            IsBound = EnterFlow(ECombatStateType.Idle);
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
            if (!IsBound)
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

        // Idle 상태와 이동 제약을 확인한 뒤 무기 교체 요청을 준비한다.
        internal bool TryRequestWeaponSwap(int p_slotIndex)
        {
            if (!IsBound ||
                _core.BlockCombat ||
                CurrentState?.Type != ECombatStateType.Idle ||
                !CanWeaponSwap())
            {
                return false;
            }

            return _core.CombatModule.TryPrepareWeaponSwap(p_slotIndex);
        }

        // WeaponSwapState가 준비된 무기 교체를 확정할 때 호출한다.

        // 대시·점프·사망 중에는 무기 교체를 허용하지 않는다.
        private bool CanWeaponSwap()
        {
            if (_core.BlockCombat)
                return false;

            return _core.LocomotionContext.CurrentState switch
            {
                ELocoStateType.Dash => false,
                ELocoStateType.Jump => false,
                ELocoStateType.Die => false,
                _ => true
            };
        }
    }
}
