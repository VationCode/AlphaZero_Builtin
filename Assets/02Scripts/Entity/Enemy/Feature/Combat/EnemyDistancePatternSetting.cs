using System;
using UnityEngine;

namespace Alpha.Enemy
{
    // 하나의 거리 구간과 해당 구간에서 선택할 공격 패턴을 연결한다.
    [Serializable]
    public sealed class EnemyDistancePatternSetting
    {
        [SerializeField, Min(0f)]
        private float _minimumDistance;

        [SerializeField, Min(0.01f)]
        private float _maximumDistance = 2f;

        [SerializeField]
        private int _patternIndex;

        // 같은 거리 구간의 후보들이 가진 Weight 합계로 선택 비율을 결정한다.
        [Tooltip(
            "현재 거리에서 선택 가능한 패턴 사이의 선택 비율입니다. " +
            "예: Weight 1과 3은 각각 25%, 75% 확률로 선택됩니다.")]
        [SerializeField, Min(0.01f)]
        private float _selectionWeight = 1f;

        public float MinimumDistance => _minimumDistance;
        public float MaximumDistance => _maximumDistance;
        public int PatternIndex => _patternIndex;
        public float SelectionWeight => _selectionWeight;

        public EnemyDistancePatternSetting()
        {
        }

        public EnemyDistancePatternSetting(
            float p_minimumDistance,
            float p_maximumDistance,
            int p_patternIndex,
            float p_selectionWeight)
        {
            _minimumDistance = p_minimumDistance;
            _maximumDistance = p_maximumDistance;
            _patternIndex = p_patternIndex;
            _selectionWeight = p_selectionWeight;
        }

        public bool IsValid(int p_patternCount)
        {
            return _patternIndex >= 0 &&
                   _patternIndex < p_patternCount &&
                   _maximumDistance >= _minimumDistance;
        }

        public bool IsWithinDistance(float p_distance)
        {
            return p_distance >= _minimumDistance &&
                   p_distance <= _maximumDistance;
        }

        // 중첩 직렬화 데이터는 소유 MonoBehaviour의 OnValidate에서 보정한다.
        public void Validate(int p_patternCount)
        {
            _minimumDistance = Mathf.Max(0f, _minimumDistance);
            _maximumDistance = Mathf.Max(
                Mathf.Max(0.01f, _minimumDistance),
                _maximumDistance);

            if (p_patternCount <= 0)
            {
                _patternIndex = -1;
            }
            else if (_patternIndex >= p_patternCount)
            {
                // 삭제된 패턴을 다른 패턴으로 자동 교체하지 않는다.
                _patternIndex = -1;
            }

            _selectionWeight = Mathf.Max(0.01f, _selectionWeight);
        }
    }
}
