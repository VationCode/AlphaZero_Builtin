using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public enum CrabState
    {
        Intro,
        Chase,
        Attack,
        Idle
    }

    public sealed class CrabBossStateMachine : MonoBehaviour
    {
        public CrabBossState CurrentState { get; private set; }

        private Dictionary<CrabState, CrabBossState> _states;

        public void Bind(CrabBossCore p_core)
        {
            if (p_core == null)
                return;

            _states = new Dictionary<CrabState, CrabBossState>
            {
                { CrabState.Intro, new CrabBossIntroState(p_core) },
                { CrabState.Chase, new CrabBossChaseState(p_core) },
                { CrabState.Attack, new CrabBossAttackState(p_core) },
                { CrabState.Idle, new CrabBossIdleState(p_core) }
            };

            CurrentState = null;
        }

        private void Update()
        {
            CurrentState?.Tick();
        }

        public bool ChangeState(CrabState p_nextState)
        {
            if (_states == null ||
                !_states.TryGetValue(p_nextState, out CrabBossState nextState))
            {
                return false;
            }

            if (CurrentState == nextState)
                return true;

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();

            return true;
        }
    }
}
