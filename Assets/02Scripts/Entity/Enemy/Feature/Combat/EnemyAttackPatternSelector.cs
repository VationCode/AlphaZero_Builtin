using UnityEngine;

namespace Alpha.Enemy
{
    // 현재 타겟 거리에서 실행 가능한 공격 패턴 하나를 가중치로 선택한다.
    public sealed class EnemyAttackPatternSelector
    {
        // 실행 가능한 후보 사이에서 Selection Weight 비율로 하나를 선택한다.
        public bool TrySelectPattern(
            EnemyCombatModule p_combat,
            Transform p_target,
            out int p_patternIndex)
        {
            p_patternIndex = -1;

            if (p_combat == null || p_target == null)
                return false;

            if (!p_combat.TryMeasureTarget(
                    p_target,
                    out _,
                    out float distance))
            {
                return false;
            }

            float totalWeight = 0f;

            for (int index = 0;
                 index < p_combat.DistancePatternCount;
                 index++)
            {
                EnemyDistancePatternSetting distancePattern =
                    p_combat.GetDistancePattern(index);

                if (!CanSelect(
                        p_combat,
                        p_target,
                        distancePattern,
                        distance))
                {
                    continue;
                }

                totalWeight += distancePattern.SelectionWeight;
            }

            if (totalWeight <= 0f)
                return false;

            float selection = Random.value * totalWeight;
            int fallbackIndex = -1;

            for (int index = 0;
                 index < p_combat.DistancePatternCount;
                 index++)
            {
                EnemyDistancePatternSetting distancePattern =
                    p_combat.GetDistancePattern(index);

                if (!CanSelect(
                        p_combat,
                        p_target,
                        distancePattern,
                        distance))
                {
                    continue;
                }

                fallbackIndex = distancePattern.PatternIndex;
                selection -= distancePattern.SelectionWeight;

                if (selection > 0f)
                    continue;

                p_patternIndex = distancePattern.PatternIndex;
                return true;
            }

            // 부동소수점 오차로 합계 끝에 도달한 경우 마지막 후보를 사용한다.
            p_patternIndex = fallbackIndex;
            return p_patternIndex >= 0;
        }

        private static bool CanSelect(
            EnemyCombatModule p_combat,
            Transform p_target,
            EnemyDistancePatternSetting p_distancePattern,
            float p_distance)
        {
            if (p_distancePattern == null ||
                !p_distancePattern.IsValid(p_combat.PatternCount) ||
                !p_distancePattern.IsWithinDistance(p_distance))
            {
                return false;
            }

            int patternIndex = p_distancePattern.PatternIndex;
            return p_combat.CanStartPattern(
                patternIndex,
                p_target);
        }
    }
}
