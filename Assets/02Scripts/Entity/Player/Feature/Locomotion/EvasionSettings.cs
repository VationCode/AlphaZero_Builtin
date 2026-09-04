using System;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public enum EEvasionType
    {
        Dash,
        Dodge
    }

    // Dash와 Dodge가 공유하는 이동 및 무적 시간 설정이다.
    [Serializable]
    public sealed class EvasionSettings
    {
        [SerializeField, Min(0f)]
        private float _distance = 6f;

        [SerializeField, Min(0.01f)]
        private float _duration = 0.3f;

        [SerializeField, Min(0f)]
        private float _invulnerabilityStartTime = 0.05f;

        [SerializeField, Min(0f)]
        private float _invulnerabilityDuration = 0.15f;

        public float Distance => _distance;
        public float Duration => _duration;
        public float InvulnerabilityStartTime =>
            _invulnerabilityStartTime;
        public float InvulnerabilityDuration =>
            _invulnerabilityDuration;
        public float InvulnerabilityEndTime =>
            _invulnerabilityStartTime + _invulnerabilityDuration;

        public bool IsValid =>
            _duration > 0f && _distance > 0f;

        public static EvasionSettings CreateDashDefault(
            float p_distance = 6f,
            float p_duration = 0.3f)
        {
            return new EvasionSettings
            {
                _distance = Mathf.Max(0f, p_distance),
                _duration = Mathf.Max(0.01f, p_duration),
                _invulnerabilityStartTime = 0.05f,
                _invulnerabilityDuration = 0.15f
            };
        }

        public static EvasionSettings CreateDodgeDefault()
        {
            return new EvasionSettings
            {
                _distance = 4f,
                _duration = 0.45f,
                _invulnerabilityStartTime = 0.08f,
                _invulnerabilityDuration = 0.25f
            };
        }

        // 무적 구간이 행동 지속 시간을 벗어나지 않도록 보정한다.
        internal void Validate()
        {
            _distance = Mathf.Max(0f, _distance);
            _duration = Mathf.Max(0.01f, _duration);
            _invulnerabilityStartTime = Mathf.Clamp(
                _invulnerabilityStartTime,
                0f,
                _duration);
            _invulnerabilityDuration = Mathf.Clamp(
                _invulnerabilityDuration,
                0f,
                _duration - _invulnerabilityStartTime);
        }
    }
}
