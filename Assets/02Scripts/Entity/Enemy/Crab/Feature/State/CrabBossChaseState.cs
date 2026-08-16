namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossChaseState : CrabBossState
    {
        public CrabBossChaseState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter() 
        {
            if (!Locomotion.TryCalculateDistanceTo(
                    Context.Target,
                    out float distance))
            {
                Context.SetDistanceToTarget(float.PositiveInfinity);
                return;
            }

            // Module에서 계산한 결과만 Context에 저장한다.
            Context.SetDistanceToTarget(distance);
        }

        public override void Tick() 
        {
            if (!Context.HasTarget)
                return;

            bool rotationCompleted =
                Locomotion.RotateTowards(Context.Target);

            if (rotationCompleted)
            {
                // 다음 단계에서 Attack 상태로 전환
            }
        }
        public override void Exit() { }
    }
}
