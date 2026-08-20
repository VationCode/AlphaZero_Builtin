using Alpha.Enemy.CrabBoss.Combat;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossAttackState : CrabBossState
    {
        public CrabBossAttackState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter()
        {
            EAttackPattern pattern = Context.SelectedAttackPattern;

            if (pattern == EAttackPattern.None ||
                Context.SelectedAttackAnimationIndex < 0 ||
                Combat == null ||
                Locomotion == null ||
                Anim == null)
            {
                StateMachine.ChangeState(CrabState.Idle);
                return;
            }

            if (!Combat.BeginAttack(
                    pattern,
                    Context,
                    Locomotion))
            {
                StateMachine.ChangeState(CrabState.Idle);
                return;
            }

            if (!Anim.PlayAttack(
                    pattern,
                    Context.SelectedAttackAnimationIndex))
            {
                StateMachine.ChangeState(CrabState.Idle);
            }
        }

        public override void Tick()
        {
            if (Combat == null || !Combat.IsAttacking)
                return;

            Combat.TickAttack(Time.deltaTime);

            EAttackPattern pattern =
                Context.SelectedAttackPattern;

            // Melee와 Range는 선택된 애니메이션 완료가 공격 종료 시점이다.
            if (IsAnimationDrivenAttack(pattern) &&
                Anim != null &&
                Anim.IsAttackComplete(
                    pattern,
                    Context.SelectedAttackAnimationIndex))
            {
                Combat.CompleteAttack();
            }

            if (Combat.IsAttackComplete)
                StateMachine.ChangeState(CrabState.Idle);
        }

        public override void Exit()
        {
            Combat?.CancelAttack();
            Context.ClearAttackPattern();
        }

        private static bool IsAnimationDrivenAttack(
            EAttackPattern p_pattern)
        {
            return p_pattern == EAttackPattern.MeleeAttack ||
                   p_pattern == EAttackPattern.RangeAttack;
        }
    }
}
