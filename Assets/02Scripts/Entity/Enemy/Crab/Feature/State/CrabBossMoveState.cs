using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossMoveState : CrabBossState
    {
        public CrabBossMoveState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter() { }
        public override void Tick() 
        {
            if (!Context.HasTarget || Locomotion == null || Combat == null)
                return;

            if (!Locomotion.TryCalculateDistanceTo(
                    Context.Target,
                    out float distance))
            {
                return;
            }

            Context.SetDistanceToTarget(distance);

            // Melee 진입 또는 Move 영역 이탈 시 Idle에서 다시 판단한다.
            if (distance <= Combat.MeleeAttackDistance ||
                distance > Combat.MoveMaxDistance)
            {
                StateMachine.ChangeState(CrabState.Idle);
                return;
            }

            Locomotion.RotateTowards(Context.Target);

            if (!Locomotion.TryApproachTarget(
                    Context.Target,
                    Combat.MeleeAttackDistance,
                    Time.deltaTime,
                    out float updatedDistance,
                    out bool reached))
            {
                return;
            }

            Context.SetDistanceToTarget(updatedDistance);

            if (reached)
                StateMachine.ChangeState(CrabState.Idle);
        }
        public override void Exit() { }
    }
}
