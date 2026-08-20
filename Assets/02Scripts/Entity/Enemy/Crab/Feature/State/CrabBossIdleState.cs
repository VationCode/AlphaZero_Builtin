using Alpha.Enemy.CrabBoss.Combat;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossIdleState : CrabBossState
    {
        public CrabBossIdleState(CrabBossCore p_core) : base(p_core) { }

        private float _delayTime = 1.5f;
        private float _time;
        public override void Enter()
        {
            Anim?.PlayIdle();
            _time = 0f;
        }

        public override void Tick()
        {
            _time += Time.deltaTime;

            if (_time < _delayTime)
                return;

            if (!Context.HasTarget ||
                Locomotion == null ||
                TargetRange == null ||
                Anim == null)
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
                if (TrySelectRandomAttack(
                        EAttackPattern.MeleeAttack))
                {
                    StateMachine.ChangeState(CrabState.Attack);
                }

                return;
            }

            // 근접 거리 안의 측면 타겟도 Chase에서 다시 회전한다.
            if (TargetRange.IsWithinChaseAllowedRange(distance))
            {
                StateMachine.ChangeState(CrabState.Chase);
                return;
            }

            // 원거리 공격과 돌진 전에 현재 타겟 방향으로 회전을 완료한다.
            if (!Locomotion.RotateTowards(direction, Time.deltaTime))
                return;

            bool selected = TargetRange.IsWithinRangeAttackRange(distance)
                ? TrySelectRandomLongRangeAttack()
                : TrySelectRandomAttack(EAttackPattern.RushAttack);

            if (selected)
                StateMachine.ChangeState(CrabState.Attack);
        }

        private bool TrySelectRandomLongRangeAttack()
        {
            int rangeCount = Anim.GetAttackAnimationCount(
                EAttackPattern.RangeAttack);
            int rushCount = Anim.GetAttackAnimationCount(
                EAttackPattern.RushAttack);
            int totalCount = rangeCount + rushCount;

            if (totalCount <= 0)
                return false;

            // 패턴이 아니라 등록된 각 공격 이름을 동일 확률로 선택한다.
            int selectedIndex = Random.Range(0, totalCount);

            if (selectedIndex < rangeCount)
            {
                Context.SetAttackSelection(
                    EAttackPattern.RangeAttack,
                    selectedIndex);
                return true;
            }

            Context.SetAttackSelection(
                EAttackPattern.RushAttack,
                selectedIndex - rangeCount);

            return true;
        }

        private bool TrySelectRandomAttack(
            EAttackPattern p_pattern)
        {
            int animationCount =
                Anim.GetAttackAnimationCount(p_pattern);

            if (animationCount <= 0)
                return false;

            Context.SetAttackSelection(
                p_pattern,
                Random.Range(0, animationCount));

            return true;
        }

        public override void Exit() { }
    }
}
