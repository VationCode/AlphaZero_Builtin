using Alpha.Enemy.CrabBoss.Combat;

namespace Alpha.Enemy.CrabBoss
{
    public abstract class CrabBossState
    {
        protected CrabBossCore Core { get; }
        protected CrabBossStateMachine StateMachine => Core.StateMachine;
        protected CrabBossContext Context => Core.Context;
        protected CrabBossLocomotionModule Locomotion =>
            Core.LocomotionModule;
        protected CrabBossTargetRangeModule TargetRange =>
            Core.TargetRangeModule;
        protected CrabBossAnimationView Anim => Core.AnimView;
        protected CrabBossCombatModule Combat => Core.CombatModule;

        protected CrabBossState(CrabBossCore p_core)
        {
            Core = p_core;
        }

        public abstract void Enter();
        public abstract void Tick();
        public abstract void Exit();
    }
}
