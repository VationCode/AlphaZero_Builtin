using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.AlphaCamera
{
    // Camera가 소유하는 Shake preset의 실제 표현 수치다.
    [Serializable]
    public struct CameraShakeSetting
    {
        [SerializeField, Min(0.01f)]
        private float _duration;

        [Header("Position")]
        [SerializeField, Min(0f)]
        private float _horizontalAmplitude;

        [SerializeField, Min(0f)]
        private float _verticalAmplitude;

        [Header("Rotation")]
        [Tooltip("Y축을 기준으로 음수와 양수 방향으로 흔들리는 최대 회전각입니다. 단위는 Degree입니다.")]
        [FormerlySerializedAs("_rotationAmount")]
        [SerializeField, Min(0f)]
        private float _yawAngle;

        [Tooltip("Z축을 기준으로 음수와 양수 사이를 왕복하는 최대 기울기입니다. 단위는 Degree입니다.")]
        [FormerlySerializedAs("_rotationAngle")]
        [SerializeField, Min(0f)]
        private float _rollAngle;

        [SerializeField, Min(0.01f)]
        private float _frequency;

        public float Duration => _duration;
        public float HorizontalAmplitude => _horizontalAmplitude;
        public float VerticalAmplitude => _verticalAmplitude;
        public float YawAngle => _yawAngle;
        public float RollAngle => _rollAngle;
        public float Frequency => _frequency;

        public bool IsValid =>
            _duration > 0f &&
            _frequency > 0f &&
            (_horizontalAmplitude > 0f ||
             _verticalAmplitude > 0f ||
             _yawAngle > 0f ||
             _rollAngle > 0f);
    }

    // Inspector에서 추가할 수 있는 이름 기반 Camera Shake 설정이다.
    [Serializable]
    public struct CameraShakePreset
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        private CameraShakeSetting _setting;

        public string Name => _name;
        public CameraShakeSetting Setting => _setting;
    }
}
