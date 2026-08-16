using Alpha.Enemy.CrabBoss.Combat;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossAttackState : CrabBossState
    {
        public CrabBossAttackState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter()
        {
            ECrabAttackPattern pattern = Context.SelectedAttackPattern;

            if (pattern == ECrabAttackPattern.None ||
                Combat == null ||
                Locomotion == null ||
                !Combat.BeginAttack(pattern, Context, Locomotion) ||
                Anim == null ||
                Anim.PlayRandomAttack(pattern) == null)
            {
                StateMachine.ChangeState(CrabState.Idle);
            }
        }

        public override void Tick()
        {
            if (Combat == null || !Combat.IsAttacking)
                return;

            Combat.TickAttack(Time.deltaTime);

            if (Combat.IsAttackComplete)
                StateMachine.ChangeState(CrabState.Idle);
        }

        public override void Exit()
        {
            Anim?.DisableRootMotion();
            Combat?.CancelAttack();
            Context.ClearAttackPattern();
        }
    }
}
