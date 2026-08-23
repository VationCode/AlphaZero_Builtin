using System;
using UnityEngine;

namespace Alpha.Enemy
{
    // 공격 패턴별 쿨다운 종료 시간을 보관하고 준비 상태를 계산한다.
    public sealed class EnemyAttackCooldown
    {
        private float[] _endTimes = Array.Empty<float>();

        public void Configure(int p_patternCount)
        {
            int patternCount = Mathf.Max(0, p_patternCount);

            if (_endTimes.Length != patternCount)
                Array.Resize(ref _endTimes, patternCount);
        }

        public bool IsReady(
            int p_patternIndex,
            float p_currentTime)
        {
            return IsValidIndex(p_patternIndex) &&
                   p_currentTime >= _endTimes[p_patternIndex];
        }

        public float GetRemaining(
            int p_patternIndex,
            float p_currentTime)
        {
            return IsValidIndex(p_patternIndex)
                ? Mathf.Max(
                    0f,
                    _endTimes[p_patternIndex] - p_currentTime)
                : float.PositiveInfinity;
        }

        public void Start(
            int p_patternIndex,
            float p_duration,
            float p_currentTime)
        {
            if (!IsValidIndex(p_patternIndex))
                return;

            _endTimes[p_patternIndex] =
                p_currentTime + Mathf.Max(0f, p_duration);
        }

        private bool IsValidIndex(int p_patternIndex)
        {
            return p_patternIndex >= 0 &&
                   p_patternIndex < _endTimes.Length;
        }
    }
}
