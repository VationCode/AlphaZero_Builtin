using System;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Boss
{
    // 근접·돌진의 직접 타격 판정만 보관한다. 피해량은 공통 부모 설정이 소유한다.
    [Serializable]
    public sealed class BossDirectHitSettings
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private EBossDamageTiming _timing;
        [SerializeField] private DetectionAreaSettings _area = new();
        [SerializeField, Min(0f)] private float _startTimeSeconds = 0.2f;
        [SerializeField, Min(0f)] private float _endTimeSeconds = 0.6f;

        public bool Enabled => _enabled;
        public EBossDamageTiming Timing => _timing;
        public DetectionAreaSettings Area => _area;
        public float StartTimeSeconds => _startTimeSeconds;
        public float EndTimeSeconds => _endTimeSeconds;

        public bool IsConfigured(EBossAttackType p_type)
        {
            if (!_enabled)
                return true;
            if (!Enum.IsDefined(typeof(EBossDamageTiming), _timing) ||
                (p_type == EBossAttackType.Melee && _timing != EBossDamageTiming.Automatic &&
                 _timing != EBossDamageTiming.AnimationWindow) ||
                _area == null || !_area.IsValid || _area.TargetLayers.value == 0 ||
                !IsFinite(_area.LocalOffset.sqrMagnitude) || !IsFinite(_area.YawOffset) ||
                !IsFinite(_area.Width) || !IsFinite(_area.Length) || !IsFinite(_area.Height) ||
                !IsFinite(_area.Radius) || !IsFinite(_area.Angle))
                return false;
            return !UsesAnimationWindow(p_type) ||
                (IsFinite(_startTimeSeconds) && IsFinite(_endTimeSeconds) &&
                 _startTimeSeconds >= 0f && _endTimeSeconds > _startTimeSeconds);
        }

        public bool UsesAnimationWindow(EBossAttackType p_type) =>
            _timing == EBossDamageTiming.AnimationWindow ||
            (_timing == EBossDamageTiming.Automatic && p_type == EBossAttackType.Melee);

        public bool OverlapsAnimationStep(float p_previous, float p_current) =>
            p_current >= _startTimeSeconds && p_previous < _endTimeSeconds;

        internal void Validate()
        {
            _area ??= new DetectionAreaSettings();
            _area.Validate();
        }

        internal static BossDirectHitSettings FromLegacy(BossDamageSettings p_source) => p_source == null
            ? new BossDirectHitSettings()
            : new BossDirectHitSettings
            {
                _enabled = p_source.Enabled,
                _timing = p_source.Timing,
                _area = p_source.Area,
                _startTimeSeconds = p_source.StartTimeSeconds,
                _endTimeSeconds = p_source.EndTimeSeconds
            };

        private static bool IsFinite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
    }
}
