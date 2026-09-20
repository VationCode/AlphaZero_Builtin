using System;

namespace Alpha.Boss
{
    // Boss 전체 진행 단계다. Combat 내부 행동 상태와 분리한다.
    public enum EBossEncounterState
    {
        WaitingForCinematic = 0,
        Cinematic = 1,
        Combat = 2
    }

    // Boss의 연출 대기, 연출, 전투 진행 상태를 보관한다.
    public sealed class BossEncounterContext
    {
        public EBossEncounterState CurrentState { get; private set; } =
            EBossEncounterState.WaitingForCinematic;

        public event Action<EBossEncounterState> OnStateChanged;

        public bool TryBeginCinematic()
        {
            if (CurrentState !=
                EBossEncounterState.WaitingForCinematic)
            {
                return false;
            }

            ChangeState(EBossEncounterState.Cinematic);
            return true;
        }

        public bool TryBeginCombat()
        {
            if (CurrentState != EBossEncounterState.Cinematic)
                return false;

            ChangeState(EBossEncounterState.Combat);
            return true;
        }

        public void ReturnToWaitingForCinematic()
        {
            ChangeState(EBossEncounterState.WaitingForCinematic);
        }

        private void ChangeState(EBossEncounterState p_nextState)
        {
            if (CurrentState == p_nextState)
                return;

            CurrentState = p_nextState;
            OnStateChanged?.Invoke(CurrentState);
        }
    }
}
