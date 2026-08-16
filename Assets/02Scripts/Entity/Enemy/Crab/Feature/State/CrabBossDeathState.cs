namespace Alpha.Enemy.CrabBoss
{
    // 추후 사망 상태를 다시 작성하기 위한 빈 골격이다.
    public sealed class CrabBossDeathState : CrabBossState
    {
        public CrabBossDeathState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter() { }
        public override void Tick() { }
        public override void Exit() { }
    }
}
