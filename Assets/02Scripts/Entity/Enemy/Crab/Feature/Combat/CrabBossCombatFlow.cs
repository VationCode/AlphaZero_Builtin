using UnityEngine;

namespace Alpha.Enemy.CrabBoss.Combat
{
    public sealed class CrabBossCombatFlow
    {
        public CrabBossContext Context { get; }
        public CrabBossCombatModule Module { get; }

        public CrabBossCombatFlow(
            CrabBossContext p_context,
            CrabBossCombatModule p_module)
        {
            Context = p_context;
            Module = p_module;
        }

        public bool TryDecideNextState(out CrabState p_nextState)
        {
            p_nextState = CrabState.Idle;

            if (Context == null || Module == null || !Context.HasTarget)
                return false;

            float distance = Context.DistanceToTarget;

            if (distance <= Module.MeleeAttackDistance)
            {
                Context.SetAttackPattern(
                    ECrabAttackPattern.MeleeAttack);
                p_nextState = CrabState.Attack;
                return true;
            }

            // 근거리 바깥부터 중거리 일부까지는 직접 접근한다.
            if (distance <= Module.MoveMaxDistance)
            {
                Context.ClearAttackPattern();
                p_nextState = CrabState.Move;
                return true;
            }

            Context.SetAttackPattern(SelectLongRangePattern());
            p_nextState = CrabState.Attack;
            return true;
        }

        private static ECrabAttackPattern SelectLongRangePattern()
        {
            return Random.value < 0.5f
                ? ECrabAttackPattern.RangeAttack
                : ECrabAttackPattern.RushAttack;
        }
    }
}
