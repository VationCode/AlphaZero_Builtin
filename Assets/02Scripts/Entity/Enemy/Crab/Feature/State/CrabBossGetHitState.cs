namespace Alpha.Enemy.CrabBoss
{
    // 추후 피격 상태를 다시 작성하기 위한 빈 골격이다.
    public sealed class CrabBossGetHitState : CrabBossState
    {
        public CrabBossGetHitState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter() { }
        public override void Tick() { }
        public override void Exit() { }
    }
}
