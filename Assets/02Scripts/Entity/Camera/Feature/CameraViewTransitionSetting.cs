using System;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    // 출발 View와 목표 View 사이의 전환 시간을 정의한다.
    [Serializable]
    public struct CameraViewTransitionSetting
    {
        [SerializeField] private ECameraViewType _from;
        [SerializeField] private ECameraViewType _to;
        [SerializeField, Min(0f)] private float _duration;

        // From → To와 To → From에 같은 시간을 사용한다.
        [SerializeField] private bool _isBidirectional;

        public float Duration => _duration;

        public bool IsDirectMatch(ECameraViewType p_from, ECameraViewType p_to)
        {
            return _from == p_from && _to == p_to;
        }

        public bool IsReverseMatch(ECameraViewType p_from, ECameraViewType p_to)
        {
            return _isBidirectional && _from == p_to && _to == p_from;
        }
    }
}
