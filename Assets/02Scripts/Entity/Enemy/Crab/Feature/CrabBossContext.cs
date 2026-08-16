using Alpha.Enemy.CrabBoss.Combat;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CrabBossContext
    {
        public Transform Target { get; private set; }
        public float DistanceToTarget { get; private set; } = float.PositiveInfinity;
        public ECrabAttackPattern SelectedAttackPattern { get; private set; } =
            ECrabAttackPattern.None;

        public bool HasTarget => Target != null;

        public void SetTarget(Transform p_target)
        {
            Target = p_target;
        }

        public void SetDistanceToTarget(float p_distance)
        {
            DistanceToTarget = p_distance;
        }

        public void SetAttackPattern(ECrabAttackPattern p_pattern)
        {
            SelectedAttackPattern = p_pattern;
        }

        public void ClearAttackPattern()
        {
            SelectedAttackPattern = ECrabAttackPattern.None;
        }

        public void ClearTarget()
        {
            Target = null;
            DistanceToTarget = float.PositiveInfinity;
            ClearAttackPattern();
        }
    }
}
