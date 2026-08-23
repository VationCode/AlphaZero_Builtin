using UnityEngine;

namespace Alpha.Enemy
{
    // 현재 거리와 쿨타임을 기준으로 다음 공격 패턴 하나를 선택한다.
    public sealed class EnemyAttackPatternSelector
    {
        // 즉시 실행 가능한 패턴을 우선하고, 없으면 가장 빨리 준비될 패턴을 선택한다.
        public bool TrySelectPattern(
            EnemyCombatModule p_combat,
            Transform p_target,
            out int p_patternIndex)
        {
            if (TrySelectReadyPattern(
                    p_combat,
                    p_target,
                    out p_patternIndex))
            {
                return true;
            }

            return TrySelectShortestCooldownPattern(
                p_combat,
                p_target,
                out p_patternIndex);
        }

        // 실행 가능한 후보 사이에서 Selection Weight 비율로 하나를 선택한다.
        public bool TrySelectReadyPattern(
            EnemyCombatModule p_combat,
            Transform p_target,
            out int p_patternIndex)
        {
            p_patternIndex = -1;

            if (p_combat == null || p_target == null)
                return false;

            float totalWeight = 0f;

            for (int index = 0; index < p_combat.PatternCount; index++)
            {
                if (!p_combat.CanStartPattern(index, p_target))
                    continue;

                EnemyAttackPatternSetting pattern =
                    p_combat.GetPattern(index);

                if (pattern != null)
                    totalWeight += pattern.SelectionWeight;
            }

            if (totalWeight <= 0f)
                return false;

            float selection = Random.value * totalWeight;
            int fallbackIndex = -1;

            for (int index = 0; index < p_combat.PatternCount; index++)
            {
                if (!p_combat.CanStartPattern(index, p_target))
                    continue;

                EnemyAttackPatternSetting pattern =
                    p_combat.GetPattern(index);

                if (pattern == null)
                    continue;

                fallbackIndex = index;
                selection -= pattern.SelectionWeight;

                if (selection > 0f)
                    continue;

                p_patternIndex = index;
                return true;
            }

            // 부동소수점 오차로 합계 끝에 도달한 경우 마지막 후보를 사용한다.
            p_patternIndex = fallbackIndex;
            return p_patternIndex >= 0;
        }

        private static bool TrySelectShortestCooldownPattern(
            EnemyCombatModule p_combat,
            Transform p_target,
            out int p_patternIndex)
        {
            p_patternIndex = -1;

            if (p_combat == null || p_target == null)
                return false;

            float shortestCooldown = float.PositiveInfinity;

            for (int index = 0; index < p_combat.PatternCount; index++)
            {
                if (!p_combat.CanPreparePattern(index, p_target))
                    continue;

                float cooldown =
                    p_combat.GetCooldownRemaining(index);

                if (cooldown >= shortestCooldown)
                    continue;

                shortestCooldown = cooldown;
                p_patternIndex = index;
            }

            return p_patternIndex >= 0;
        }
    }
}
