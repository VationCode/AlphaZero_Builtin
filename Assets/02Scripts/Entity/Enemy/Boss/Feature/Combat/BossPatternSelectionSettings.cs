using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 패턴별 선택 거리와 후보 사이의 상대 가중치를 보관한다. 이동거리와는 독립적이다.
    [Serializable]
    public sealed class BossPatternSelectionSettings
    {
        [SerializeField, Min(0f)]
        [Tooltip("공격 가능한 최소 수평 거리입니다. 자동 행동은 이보다 가까우면 거리를 확보합니다.")]
        private float _minimumDistance;

        [SerializeField, Min(0f)]
        [Tooltip("공격 가능한 최대 수평 거리입니다. 자동 행동은 이보다 멀면 사거리까지 접근합니다.")]
        private float _maximumDistance = 25f;

        [SerializeField, Min(0f)]
        [Tooltip("선택 가능한 후보 사이의 상대 비율입니다. 자동 행동은 공격 종류를 함께 비교하며, 0은 후보 제외입니다.")]
        private float _weight = 1f;

        public float Weight => _weight;
        public float MinimumDistance => _minimumDistance;
        public float MaximumDistance => _maximumDistance;

        public bool CanSelectAtDistance(float p_distance) =>
            CanConsiderAtDistance(p_distance) && p_distance <= _maximumDistance;

        // 접근 후보는 최대 거리만 무시한다. 최소 거리와 유효한 설정 조건은 유지한다.
        public bool CanApproachFromDistance(float p_distance) =>
            CanConsiderAtDistance(p_distance) && p_distance > _maximumDistance;

        private bool CanConsiderAtDistance(float p_distance) =>
            CanRepositionFromDistance(p_distance) && p_distance >= _minimumDistance;

        // 이동은 사거리만 조정한다. 비활성 가중치·잘못된 거리 설정은 그대로 제외한다.
        public bool CanRepositionFromDistance(float p_distance) =>
            IsFinite(p_distance) && p_distance >= 0f && IsFinite(_weight) && _weight > 0f &&
            IsFinite(_minimumDistance) && IsFinite(_maximumDistance) && _minimumDistance >= 0f &&
            _maximumDistance >= _minimumDistance;

        public void Validate()
        {
            _minimumDistance = IsFinite(_minimumDistance) ? Mathf.Max(0f, _minimumDistance) : 0f;
            _maximumDistance = IsFinite(_maximumDistance)
                ? Mathf.Max(_minimumDistance, _maximumDistance) : Mathf.Max(_minimumDistance, 25f);
            _weight = IsFinite(_weight) ? Mathf.Max(0f, _weight) : 0f;
        }

        private static bool IsFinite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
    }
}
