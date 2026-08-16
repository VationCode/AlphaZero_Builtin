namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossIntroState : CrabBossState
    {
        public CrabBossIntroState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter() 
        {
            Anim?.PlayIntro();
        }

        public override void Tick() { }
        public override void Exit() { }
    }
}
