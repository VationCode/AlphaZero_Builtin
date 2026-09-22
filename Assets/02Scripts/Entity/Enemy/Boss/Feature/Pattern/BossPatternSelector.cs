using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Boss
{
    // 거리·쿨다운·추가 조건을 통과한 후보를 가중치로 선택한다. 공격 타입은 필터로 사용하지 않는다.
    public sealed class BossPatternSelector
    {
        private BossPattern[] _patterns = Array.Empty<BossPattern>();
        private bool _selecting;
        private readonly Func<double> _random;
        private readonly List<BossPattern> _candidates = new();

        public BossPatternSelector(Func<double> p_random = null) =>
            _random = p_random ?? (() => UnityEngine.Random.value);

        // 중복 인스턴스로 선택 확률이 늘어나지 않도록 한 번만 등록한다.
        public void SetPatterns(params BossPattern[] p_patterns)
        {
            var unique = new List<BossPattern>();
            if (p_patterns != null)
                foreach (BossPattern pattern in p_patterns)
                    if (pattern != null && !unique.Contains(pattern)) unique.Add(pattern);
            _patterns = unique.ToArray();
        }

        public bool TrySelect(BossContext p_context, float p_distance, out BossPattern p_pattern)
            => TrySelect(p_context, p_distance, false, out p_pattern);

        // 실행 후보가 없을 때만 쿨다운 중인 패턴을 접근·대기 거리의 기준으로 선택한다.
        public bool TrySelectWaiting(BossContext p_context, float p_distance, out BossPattern p_pattern)
            => TrySelect(p_context, p_distance, true, out p_pattern);

        private bool TrySelect(BossContext p_context, float p_distance, bool p_waiting, out BossPattern p_pattern)
        {
            p_pattern = null;
            if (_selecting || p_context == null || p_context.Target == null) return false;
            _selecting = true;
            try
            {
                _candidates.Clear();
                double totalWeight = 0d;
                foreach (BossPattern candidate in _patterns)
                {
                    if (!CanSelect(candidate, p_context, p_distance, p_waiting)) continue;
                    _candidates.Add(candidate);
                    totalWeight += candidate.Settings.SelectionWeight;
                }
                if (_candidates.Count == 0) return false;
                double sample = _random();
                if (double.IsNaN(sample) || double.IsInfinity(sample) || sample < 0d || sample > 1d)
                    throw new InvalidOperationException("Pattern random sample must be between 0 and 1.");
                double selection = sample * totalWeight;
                foreach (BossPattern candidate in _candidates)
                {
                    selection -= candidate.Settings.SelectionWeight;
                    if (selection >= 0d) continue;
                    p_pattern = candidate;
                    return true;
                }
                // Unity Random.value가 1을 반환하거나 끝 경계에 도달한 경우 마지막 후보를 선택한다.
                p_pattern = _candidates[_candidates.Count - 1];
                return true;
            }
            finally { _candidates.Clear(); _selecting = false; }
        }

        // 접근 중에는 다시 추첨하지 않고 등록·선택 거리·쿨다운·추가 조건만 확인한다.
        public bool CanSelect(BossPattern p_pattern, BossContext p_context, float p_distance)
            => CanSelect(p_pattern, p_context, p_distance, false);

        public bool CanWaitFor(BossPattern p_pattern, BossContext p_context, float p_distance)
            => CanSelect(p_pattern, p_context, p_distance, true);

        private bool CanSelect(BossPattern p_pattern, BossContext p_context, float p_distance, bool p_waiting)
        {
            if (p_pattern == null || p_context == null || p_context.Target == null ||
                Array.IndexOf(_patterns, p_pattern) < 0 || p_pattern.IsOwned ||
                p_pattern.Settings.SelectionWeight <= 0f || !p_pattern.IsInRange(p_distance)) return false;
            bool coolingDown = p_pattern.Runtime.GetCooldownRemaining(p_context.ElapsedTime) > 0d;
            if (coolingDown != p_waiting) return false;
            try { return p_pattern.CanExecute(p_context); }
            catch (Exception exception) { Debug.LogException(exception); return false; }
        }
    }
}
