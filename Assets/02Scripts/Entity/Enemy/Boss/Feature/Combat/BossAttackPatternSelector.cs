using System;
using System.Collections.Generic;

namespace Alpha.Boss
{
    // 실행 조건·사용 이력·거리 조정 비용으로 다음 공격을 판단하는 Flow 역할이다.
    public sealed class BossAttackPatternSelector
    {
        private readonly Random _random;
        private readonly BossPatternHistoryContext _history;
        private readonly List<BossPatternData> _candidates = new();
        private readonly HashSet<BossPatternData> _seen = new();
        internal BossPatternHistoryContext History => _history;

        public BossAttackPatternSelector() : this(new BossPatternHistoryContext(), new Random()) { }
        public BossAttackPatternSelector(BossPatternHistoryContext p_history) : this(p_history, new Random()) { }
        internal BossAttackPatternSelector(Random p_random) : this(new BossPatternHistoryContext(), p_random) { }
        internal BossAttackPatternSelector(BossPatternHistoryContext p_history, Random p_random)
        {
            _history = p_history ?? throw new ArgumentNullException(nameof(p_history));
            _random = p_random ?? throw new ArgumentNullException(nameof(p_random));
        }

        public bool TrySelectPattern(BossPatternGroup[] p_groups, EBossPatternGroupType p_groupType,
            EBossAttackType p_attackType, float p_distance, Func<BossPatternData, bool> p_canStart,
            out BossPatternData p_pattern)
            => TrySelect(p_groups, p_groupType, p_attackType, p_distance, false, p_canStart, out p_pattern);

        // 이동 가능하면 모든 그룹에서 거리 조정 후 사용할 패턴도 함께 비교한다.
        public bool TrySelectActionPattern(BossPatternGroup[] p_groups, float p_distance,
            bool p_canReposition, Func<BossPatternData, bool> p_canStart, out BossPatternData p_pattern) =>
            TrySelect(p_groups, null, null, p_distance, p_canReposition, p_canStart, out p_pattern);

        private bool TrySelect(BossPatternGroup[] p_groups, EBossPatternGroupType? p_groupType,
            EBossAttackType? p_attackType, float p_distance, bool p_canReposition,
            Func<BossPatternData, bool> p_canStart, out BossPatternData p_pattern)
        {
            p_pattern = null;
            _candidates.Clear();
            _seen.Clear();
            if (p_groups == null || p_canStart == null)
                return false;

            foreach (BossPatternGroup group in p_groups)
            {
                if (group == null || !group.isActiveAndEnabled ||
                    (p_groupType.HasValue && group.GroupType != p_groupType.Value))
                    continue;
                for (int i = 0; i < group.PatternCount; i++)
                {
                    BossPatternData pattern = group.GetPattern(i);
                    if (pattern == null || (p_attackType.HasValue && pattern.AttackType != p_attackType.Value) ||
                        !_seen.Add(pattern) || pattern.Selection == null ||
                        !(p_canReposition ? pattern.Selection.CanRepositionFromDistance(p_distance) :
                            pattern.Selection.CanSelectAtDistance(p_distance)) ||
                        !p_canStart(pattern))
                        continue;
                    _candidates.Add(pattern);
                }
            }
            if (_candidates.Count == 0)
                return false;

            // 대안이 하나라도 있을 때만 직전 공격을 제외한다.
            if (_candidates.Count > 1)
                _candidates.Remove(_history.LastPattern);

            long longestWait = 0;
            foreach (BossPatternData candidate in _candidates)
                longestWait = Math.Max(longestWait, _history.GetUnusedAttackCount(candidate));
            // 가중치가 낮아도 계속 누락되지 않도록 오래 기다린 후보부터 기회를 준다.
            bool prioritizeOldest = longestWait >= Math.Max(6, _candidates.Count * 2);
            if (prioritizeOldest)
                _candidates.RemoveAll(candidate => _history.GetUnusedAttackCount(candidate) < longestWait);

            double totalWeight = 0d;
            foreach (BossPatternData candidate in _candidates)
                totalWeight += GetWeight(candidate, p_distance);

            // 제외된 패턴의 가중치는 합계에 넣지 않는다. 남은 후보끼리 확률을 다시 계산한다.
            double selection = _random.NextDouble() * totalWeight;
            foreach (BossPatternData candidate in _candidates)
            {
                selection -= GetWeight(candidate, p_distance);
                if (selection < 0d)
                {
                    p_pattern = candidate;
                    return true;
                }
            }
            p_pattern = _candidates[_candidates.Count - 1];
            return true;
        }

        private double GetWeight(BossPatternData p_pattern, float p_distance)
        {
            double weight = p_pattern.Selection.Weight;
            weight *= 1d + Math.Min(12L, _history.GetUnusedAttackCount(p_pattern)) * 0.25d;
            if (_history.WasUsedRecently(p_pattern))
                weight *= 0.2d;
            if (_history.LastPattern != null && _history.LastPattern.AttackType == p_pattern.AttackType)
                weight *= 0.5d;
            if (!p_pattern.Selection.CanSelectAtDistance(p_distance))
                weight *= 0.35d;
            return weight;
        }
    }
}
