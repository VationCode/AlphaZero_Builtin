using System.Collections.Generic;

namespace Alpha.Boss
{
    // 전투 동안의 사용 이력이다. 개별 공격의 종료·취소로 지우지 않는다.
    public sealed class BossPatternHistoryContext
    {
        private readonly Dictionary<BossPatternData, long> _lastUsed = new();
        private readonly List<BossPatternData> _recent = new();
        private long _attackCount;

        public BossPatternData LastPattern => _recent.Count > 0 ? _recent[0] : null;
        public bool WasUsedRecently(BossPatternData p_pattern) => _recent.Contains(p_pattern);
        public long GetUnusedAttackCount(BossPatternData p_pattern) =>
            _attackCount - (_lastUsed.TryGetValue(p_pattern, out long last) ? last : 0L);

        public void RecordStarted(BossPatternData p_pattern)
        {
            if (p_pattern == null)
                return;
            _lastUsed[p_pattern] = ++_attackCount;
            _recent.Insert(0, p_pattern);
            if (_recent.Count > 3)
                _recent.RemoveAt(_recent.Count - 1);
        }

        public void Clear()
        {
            _lastUsed.Clear();
            _recent.Clear();
            _attackCount = 0;
        }
    }
}
