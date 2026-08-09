using UnityEngine;

namespace Alpha.AlphaCamera
{
    // 전용 Shake Transform에 일시적인 위치·회전 흔들림을 적용한다.
    public sealed class CameraShakeModule : MonoBehaviour
    {
        [SerializeField]
        private Transform _shakeRoot;

        [Header("Fire Shake")]
        [SerializeField, Min(0.01f)]
        private float _duration = 0.12f;

        [SerializeField]
        private Vector3 _positionAmplitude =
            new(0.02f, 0.015f, 0.01f);

        [SerializeField]
        private Vector3 _rotationAmplitude =
            new(1.1f, 0.45f, 0.25f);

        [SerializeField, Min(0.01f)]
        private float _frequency = 24f;

        [SerializeField, Min(1f)]
        private float _maxStrength = 2f;

        [SerializeField]
        private AnimationCurve _envelope =
            AnimationCurve.EaseInOut(
                0f,
                1f,
                1f,
                0f);

        private float _remainingTime;
        private float _currentStrength;
        private float _activeDuration;
        private Vector3 _activePositionAmplitude;
        private Vector3 _activeRotationAmplitude;
        private float _activeFrequency;

        public bool Initialize()
        {
            return _shakeRoot != null;
        }

        // 연사 시 시간을 갱신하되 강도는 제한 범위 안에서 유지한다.
        public void Play(float p_strength = 1f)
        {
            BeginShake(
                _duration,
                _positionAmplitude,
                _rotationAmplitude,
                _frequency,
                p_strength);
        }

        // 무기 View가 전달한 설정으로 현재 Shake 표현을 교체한다.
        public void Play(
            in CameraShakeSetting p_setting)
        {
            if (!p_setting.IsValid)
                return;

            BeginShake(
                p_setting.Duration,
                p_setting.PositionAmplitude,
                p_setting.RotationAmplitude,
                p_setting.Frequency,
                1f);
        }

        private void BeginShake(
            float p_duration,
            Vector3 p_positionAmplitude,
            Vector3 p_rotationAmplitude,
            float p_frequency,
            float p_strength)
        {
            if (_shakeRoot == null ||
                p_duration <= 0f ||
                p_frequency <= 0f ||
                p_strength <= 0f)
            {
                return;
            }

            _activeDuration = p_duration;
            _activePositionAmplitude =
                p_positionAmplitude;
            _activeRotationAmplitude =
                p_rotationAmplitude;
            _activeFrequency = p_frequency;

            _remainingTime = _activeDuration;

            _currentStrength = Mathf.Clamp(
                Mathf.Max(
                    _currentStrength,
                    p_strength),
                0f,
                _maxStrength);
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

            float weight =
                envelopeWeight *
                _currentStrength;

            float noiseTime =
                Time.time * _activeFrequency;

            Vector3 positionNoise = new(
                SignedNoise(noiseTime, 11.3f),
                SignedNoise(noiseTime, 27.7f),
                SignedNoise(noiseTime, 43.1f));

            Vector3 rotationNoise = new(
                SignedNoise(noiseTime, 58.9f),
                SignedNoise(noiseTime, 71.5f),
                SignedNoise(noiseTime, 93.7f));

            _shakeRoot.localPosition =
                Vector3.Scale(
                    positionNoise,
                    _activePositionAmplitude) * weight;

            _shakeRoot.localRotation =
                Quaternion.Euler(
                    Vector3.Scale(
                        rotationNoise,
                        _activeRotationAmplitude) * weight);

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
            _currentStrength = 0f;

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

#if UNITY_EDITOR
        [ContextMenu("Camera Test/Shake")]
        private void TestShake()
        {
            Play();
        }
#endif
    }
}
