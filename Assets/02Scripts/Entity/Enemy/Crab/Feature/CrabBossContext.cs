using Alpha.Enemy.CrabBoss.Combat;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossContext
    {
        public Transform Target { get; private set; }
        public float DistanceToTarget { get; private set; } = float.PositiveInfinity;
        public EAttackPattern SelectedAttackPattern { get; private set; } =
            EAttackPattern.None;

        public int SelectedAttackAnimationIndex { get; private set; } = -1;
        public bool HasTarget => Target != null;

        public void SetTarget(Transform p_target)
        {
            Target = p_target;
        }

        public void SetDistanceToTarget(float p_distance)
        {
            DistanceToTarget = p_distance;
        }

        public void SetAttackPattern(EAttackPattern p_pattern)
        {
            SelectedAttackPattern = p_pattern;
            SelectedAttackAnimationIndex = -1;
        }

        public void ClearAttackPattern()
        {
            SelectedAttackPattern = EAttackPattern.None;
            SelectedAttackAnimationIndex = -1;
        }
        public void ClearTarget()
        {
            Target = null;
            DistanceToTarget = float.PositiveInfinity;
            ClearAttackPattern();
        }

        public void SetAttackSelection(
            EAttackPattern p_pattern,
            int p_animationIndex)
        {
            SelectedAttackPattern = p_pattern;
            SelectedAttackAnimationIndex = p_animationIndex;
        }
    }
}
