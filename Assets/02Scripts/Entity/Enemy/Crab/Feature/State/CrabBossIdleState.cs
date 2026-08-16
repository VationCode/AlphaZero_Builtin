namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossIdleState : CrabBossState
    {
        public CrabBossIdleState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter() { }

        public override void Tick()
        {
            if (!Context.HasTarget || Locomotion == null)
                return;

            if (!Locomotion.TryCalculateDistanceTo(
                    Context.Target,
                    out float distance))
            {
                return;
            }

            Context.SetDistanceToTarget(distance);

            if (Core.CombatFlow.TryDecideNextState(
                    out CrabState nextState))
            {
                StateMachine.ChangeState(nextState);
            }
        }

        public override void Exit() { }
    }
}
