using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossChaseState : CrabBossState
    {
        public CrabBossChaseState(CrabBossCore p_core) : base(p_core) { }

        public override void Enter()
        {
            Anim?.PlayWalk();
        }

        public override void Tick()
        {
            if (!Context.HasTarget ||
                Locomotion == null ||
                TargetRange == null)
            {
                return;
            }

            if (!TargetRange.TryMeasure(
                    Context.Target,
                    out Vector3 direction,
                    out float distance))
            {
                Context.SetDistanceToTarget(float.PositiveInfinity);
                return;
            }

            Context.SetDistanceToTarget(distance);

            if (TargetRange.IsWithinMeleeSector(direction, distance))
            {
                // 정면 부채꼴 진입 후 공격 전 Idle을 거친다.
                StateMachine.ChangeState(CrabState.Idle);
                return;
            }

            if (!TargetRange.IsWithinChaseAllowedRange(distance))
            {
                StateMachine.ChangeState(CrabState.Idle);
                return;
            }

            Locomotion.RotateTowards(direction, Time.deltaTime);

            if (!Locomotion.TryChaseTarget(
                    direction,
                    distance,
                    TargetRange.MeleeAttackDistance,
                    Time.deltaTime,
                    out _))
            {
                return;
            }

            if (!TargetRange.TryMeasure(
                    Context.Target,
                    out Vector3 updatedDirection,
                    out float updatedDistance))
            {
                Context.SetDistanceToTarget(float.PositiveInfinity);
                return;
            }

            Context.SetDistanceToTarget(updatedDistance);

            if (TargetRange.IsWithinMeleeSector(
                    updatedDirection,
                    updatedDistance))
            {
                StateMachine.ChangeState(CrabState.Idle);
            }
        }

        public override void Exit() { }
    }
}
