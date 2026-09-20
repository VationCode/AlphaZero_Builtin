using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Boss
{
    // 보스의 모든 패턴 설정을 소유한다. 거리·AoE 그룹은 고정 ID로 이 원본을 조회한다.
    [DisallowMultipleComponent]
    public sealed class BossPatternCatalog : MonoBehaviour
    {
        [SerializeField] private BossPatternData[] _patterns = Array.Empty<BossPatternData>();

        public int PatternCount => _patterns?.Length ?? 0;

        public BossPatternData GetPattern(int p_index) =>
            p_index >= 0 && p_index < PatternCount ? _patterns[p_index] : null;

        public BossPatternData FindPattern(string p_id)
        {
            if (string.IsNullOrEmpty(p_id))
                return null;
            for (int i = 0; i < PatternCount; i++)
            {
                BossPatternData pattern = _patterns[i];
                if (pattern != null && string.Equals(pattern.Id, p_id, StringComparison.Ordinal))
                    return pattern;
            }
            return null;
        }

        private void Awake() => ValidatePatterns();
        private void OnValidate() => ValidatePatterns();

        private void ValidatePatterns()
        {
            _patterns ??= Array.Empty<BossPatternData>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var settings = new HashSet<BossAttackSettings>();
            for (int i = 0; i < _patterns.Length; i++)
            {
                _patterns[i] ??= new BossPatternData();
                BossPatternData pattern = _patterns[i];
                if (!settings.Add(pattern.Settings))
                {
                    pattern.MakeSettingsIndependent();
                    settings.Add(pattern.Settings);
                }
                pattern.Validate();
                // Inspector에서 항목을 복제한 경우 새 항목에만 별도 ID를 부여한다.
                while (!ids.Add(pattern.Id))
                    pattern.RegenerateId();
            }
        }
    }
}
