using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    // 전용 Shake Transform에 일시적인 위치·회전 흔들림을 적용한다.
    public sealed class CameraShakeModule : MonoBehaviour
    {
        [SerializeField]
        private Transform _shakeRoot;

        [Header("Shake Presets")]
        [SerializeField]
        private CameraShakePreset[] _presets;

        [SerializeField]
        private AnimationCurve _envelope =
            AnimationCurve.EaseInOut(
                0f,
                1f,
                1f,
                0f);

        private readonly Dictionary<string, CameraShakeSetting>
            _presetCache = new(StringComparer.Ordinal);

        private bool _isPresetCacheReady;

        private float _remainingTime;
        private float _activeDuration;
        private float _activeHorizontalAmplitude;
        private float _activeVerticalAmplitude;
        private float _activeYawAngle;
        private float _activeRollAngle;
        private float _activeFrequency;

        public bool Initialize()
        {
            RebuildPresetCache(true);
            return _shakeRoot != null;
        }

        // 외부에는 수치 대신 Inspector에 등록한 이름만 받아 Shake를 실행한다.
        public bool Play(string p_name)
        {
            if (string.IsNullOrWhiteSpace(p_name))
            {
                Debug.LogWarning(
                    "Camera Shake preset 이름이 비어 있습니다.",
                    this);
                return false;
            }

            if (!TryGetSetting(
                    p_name,
                    out CameraShakeSetting setting))
            {
                Debug.LogWarning(
                    $"Camera Shake preset을 찾을 수 없습니다: {p_name}",
                    this);
                return false;
            }

            if (!setting.IsValid)
            {
                Debug.LogWarning(
                    $"Camera Shake preset 설정값이 유효하지 않습니다: {p_name}. " +
                    "Duration과 Frequency는 0보다 커야 하고, " +
                    "Horizontal, Vertical, Yaw Angle, Roll Angle 중 하나는 0보다 커야 합니다.",
                    this);
                return false;
            }

            BeginShake(setting);

            return true;
        }

        // 다른 Camera 기능도 동일한 이름으로 설정값을 조회할 수 있다.
        public bool TryGetSetting(
            string p_name,
            out CameraShakeSetting p_setting)
        {
            p_setting = default;

            if (string.IsNullOrWhiteSpace(p_name))
                return false;

            EnsurePresetCache();

            return _presetCache.TryGetValue(
                p_name.Trim(),
                out p_setting);
        }

        private void EnsurePresetCache()
        {
            if (!_isPresetCacheReady)
                RebuildPresetCache(false);
        }

        private void RebuildPresetCache(bool p_logWarnings)
        {
            _presetCache.Clear();
            _isPresetCacheReady = true;

            if (_presets == null)
                return;

            for (int index = 0; index < _presets.Length; index++)
            {
                CameraShakePreset preset = _presets[index];
                string presetName = preset.Name?.Trim();

                if (string.IsNullOrWhiteSpace(presetName))
                {
                    if (p_logWarnings)
                    {
                        Debug.LogWarning(
                            $"Camera Shake preset #{index}의 이름이 비어 있습니다.",
                            this);
                    }

                    continue;
                }

                if (_presetCache.ContainsKey(presetName))
                {
                    if (p_logWarnings)
                    {
                        Debug.LogWarning(
                            $"Camera Shake preset 이름이 중복되었습니다: {presetName}",
                            this);
                    }

                    continue;
                }

                _presetCache.Add(
                    presetName,
                    preset.Setting);
            }
        }

        private void BeginShake(in CameraShakeSetting p_setting)
        {
            if (_shakeRoot == null || !p_setting.IsValid)
                return;

            _activeDuration = p_setting.Duration;
            _activeHorizontalAmplitude =
                p_setting.HorizontalAmplitude;
            _activeVerticalAmplitude =
                p_setting.VerticalAmplitude;
            _activeYawAngle = p_setting.YawAngle;
            _activeRollAngle = p_setting.RollAngle;
            _activeFrequency = p_setting.Frequency;

            _remainingTime = _activeDuration;
        }

        private void LateUpdate()
        {
            if (_remainingTime <= 0f)
                return;

            _remainingTime = Mathf.Max(
                0f,
                _remainingTime - Time.deltaTime);

            float elapsedRatio =
                1f - (_remainingTime / _activeDuration);

            float envelopeWeight =
                _envelope != null &&
                _envelope.length > 0
                    ? _envelope.Evaluate(elapsedRatio)
                    : 1f - elapsedRatio;

            float weight = envelopeWeight;

            float noiseTime =
                Time.time * _activeFrequency;

            float horizontalNoise =
                SignedNoise(noiseTime, 11.3f);
            float verticalNoise =
                SignedNoise(noiseTime, 27.7f);

            _shakeRoot.localPosition = new Vector3(
                horizontalNoise * _activeHorizontalAmplitude,
                verticalNoise * _activeVerticalAmplitude,
                0f) * weight;

            // Yaw는 불규칙한 좌우 회전, Roll은 Z축 기준의 명확한 왕복 기울기를 담당한다.
            float yawAngle =
                SignedNoise(noiseTime, 58.9f) *
                _activeYawAngle *
                weight;
            float rollAngle =
                Mathf.Sin(noiseTime) *
                _activeRollAngle *
                weight;

            _shakeRoot.localRotation =
                Quaternion.Euler(0f, yawAngle, rollAngle);

            if (_remainingTime <= 0f)
                ResetPose();
        }

        private static float SignedNoise(
            float p_time,
            float p_offset)
        {
            return Mathf.PerlinNoise(
                p_time,
                p_offset) * 2f - 1f;
        }

        private void ResetPose()
        {
            if (_shakeRoot == null)
                return;

            _shakeRoot.localPosition = Vector3.zero;
            _shakeRoot.localRotation = Quaternion.identity;
        }

        private void OnDisable()
        {
            _remainingTime = 0f;
            ResetPose();
        }

        private void OnValidate()
        {
            _isPresetCacheReady = false;

            // Inspector 입력 중에는 미완성 설정으로 경고하지 않고 캐시만 갱신한다.
            RebuildPresetCache(false);
        }

    }
}
