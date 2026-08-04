using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Combat
{
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

            InitializeStates();
            IsBound = EnterFlow(ECombatStateType.Idle);
        }

        private void InitializeStates()
        {
            ExitFlow();
            _stateDict.Clear();

            RegisterState(new CombatIdleState(_core, this));
            RegisterState(new WeaponSwapState(_core, this));
            RegisterState(new WeaponActionState(_core, this));
        }

        private void Update()
        {
            if (!IsBound)
                return;

            TickFlow();
            //UpdateAimState();
        }

        internal bool RegisterState(CombatStateBase p_state)
        {
            if (p_state == null || _stateDict.ContainsKey(p_state.Type))
                return false;

            _stateDict.Add(p_state.Type, p_state);
            return true;
        }

        internal bool EnterFlow(ECombatStateType p_entryState)
        {
            return TryChangeState(p_entryState);
        }

        internal void TickFlow()
        {
            CurrentState?.TickState();
        }

        internal void ExitFlow()
        {
            CurrentState?.ExitState();
            CurrentState = null;
        }

        internal bool TryChangeState(ECombatStateType p_nextState)
        {
            if (!_stateDict.TryGetValue(p_nextState, out CombatStateBase nextState))
                return false;

            if (ReferenceEquals(CurrentState, nextState))
                return false;

            CurrentState?.ExitState();
            CurrentState = nextState;

            _core.CombatContext.SetCurrentState(nextState.Type);
            CurrentState.EnterState();

            return true;
        }

        // 입력을 검증하고 교체할 무기를 Pending 상태로 준비한다.
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
