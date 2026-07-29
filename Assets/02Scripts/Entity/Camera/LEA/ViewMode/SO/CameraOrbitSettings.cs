using System;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    // 회전 가능한 Camera ViewMode의 조작 설정이다.
    [Serializable]
    public class CameraOrbitSettings
    {
        [SerializeField] private float _lookSensitivity = 15f;

        [SerializeField] private float _minPitch = -70f;

        [SerializeField] private float _maxPitch = 70f;

        public float LookSensitivity =>
            _lookSensitivity;

        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;

        public float ClampPitch(float p_pitch)
        {
            return Mathf.Clamp(p_pitch, _minPitch, _maxPitch);
        }

        internal void Validate()
        {
            _lookSensitivity = Mathf.Max(0f, _lookSensitivity);

            _minPitch = Mathf.Clamp(_minPitch, -89f, 89f);

            _maxPitch = Mathf.Clamp(_maxPitch, _minPitch, 89f);
        }
    }
}
