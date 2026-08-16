using System;

namespace Alpha.Enemy.CrabBoss.Combat
{
    [Serializable]
    public sealed class CrabBossArenaAttackStrategy
        : CrabBossAttackStrategy
    {
        public override ECrabAttackPattern Pattern =>
            ECrabAttackPattern.ArenaAttack;

        public override bool Begin(
            CrabBossContext p_context,
            CrabBossLocomotionModule p_locomotion)
        {
            // 세부 공격은 해당 패턴 작업 시 구현한다.
            return false;
        }

        public override void Tick(float p_deltaTime) { }
    }
}
