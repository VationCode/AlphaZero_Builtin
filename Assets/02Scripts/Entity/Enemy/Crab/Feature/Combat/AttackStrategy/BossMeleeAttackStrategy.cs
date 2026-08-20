using System;

namespace Alpha.Enemy.CrabBoss.Combat
{
    [Serializable]
    public sealed class BossMeleeAttackStrategy : BossAttackStrategy
    {
        public override EAttackPattern Pattern => EAttackPattern.MeleeAttack;

        public override bool Begin(CrabBossContext p_context, CrabBossLocomotionModule p_locomotion)
        {
            Cancel();

            if (p_context == null ||
                !p_context.HasTarget)
            {
                return false;
            }

            IsComplete = false;

            return true;
        }

        // 실행 중 갱신은 없으며, AttackState가 애니메이션 완료를 전달한다.
        public override void Tick(float p_deltaTime) { }
    }
}
