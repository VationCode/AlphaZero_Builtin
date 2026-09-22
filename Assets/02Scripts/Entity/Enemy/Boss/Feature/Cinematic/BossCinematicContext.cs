using System;

namespace Alpha.Boss
{
    // 시네마틱의 대기·재생·완료 단계다.
    public enum EBossCinematicState
    {
        WaitingForCinematic = 0,
        Cinematic = 1,
        Completed = 2
    }

    // 시네마틱 진행 상태만 보관한다.
    public sealed class BossCinematicContext
    {
        public EBossCinematicState CurrentState { get; private set; } =
            EBossCinematicState.WaitingForCinematic;

        public event Action<EBossCinematicState> OnStateChanged;

        public bool TryBeginCinematic()
        {
            if (CurrentState !=
                EBossCinematicState.WaitingForCinematic)
            {
                return false;
            }

            ChangeState(EBossCinematicState.Cinematic);
            return true;
        }

        public bool TryComplete()
        {
            if (CurrentState != EBossCinematicState.Cinematic)
                return false;

            ChangeState(EBossCinematicState.Completed);
            return true;
        }

        public void ReturnToWaitingForCinematic()
        {
            ChangeState(EBossCinematicState.WaitingForCinematic);
        }

        private void ChangeState(EBossCinematicState p_nextState)
        {
            if (CurrentState == p_nextState)
                return;

            CurrentState = p_nextState;
            OnStateChanged?.Invoke(CurrentState);
        }
    }
}
