using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Boss
{
    // 거리 또는 AoE 그룹의 패턴 ID를 보관하고 중앙 설정의 원본을 반환한다.
    [DisallowMultipleComponent]
    public sealed class BossPatternGroup : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_rangeType")]
        [Tooltip("Near는 근거리, Far는 중·원거리 대응, AoE는 Area·Arena 패턴 그룹입니다.")]
        private EBossPatternGroupType _groupType = EBossPatternGroupType.Near;

        [SerializeField]
        private BossPatternCatalog _catalog;

        [SerializeField]
        private string[] _patternIds = Array.Empty<string>();

        public BossPatternCatalog Catalog => _catalog != null
            ? _catalog : GetComponentInParent<BossPatternCatalog>(true);
        public EBossPatternGroupType GroupType => _groupType;
        public int PatternCount => _patternIds?.Length ?? 0;

        public BossPatternData GetPattern(int p_index)
        {
            BossPatternData pattern = p_index >= 0 && p_index < PatternCount
                ? Catalog?.FindPattern(_patternIds[p_index]) : null;
            return SupportsPattern(pattern) ? pattern : null;
        }

        public bool SupportsPattern(BossPatternData p_pattern) => p_pattern != null &&
            ((_groupType == EBossPatternGroupType.AoE) ==
             (p_pattern.AttackType == EBossAttackType.Area || p_pattern.AttackType == EBossAttackType.Arena));

        private void Awake() => ValidatePatterns();

        private void OnValidate() => ValidatePatterns();
        private void Reset() => ValidatePatterns();

        private void ValidatePatterns()
        {
            // 이전 Middle 저장값을 Near로 초기화하지 않고 통합한 Far로 옮긴다.
            if ((int)_groupType == 1)
                _groupType = EBossPatternGroupType.Far;
            if (!Enum.IsDefined(typeof(EBossPatternGroupType), _groupType))
                _groupType = EBossPatternGroupType.Near;

            if (_catalog == null)
                _catalog = GetComponentInParent<BossPatternCatalog>(true);
            _patternIds ??= Array.Empty<string>();
            // 삭제된 ID도 유지하여 Catalog에서 삭제를 되돌리면 기존 배정이 복원되게 한다.
        }
    }
}
