using System;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Shake 실행에 필요한 무기별 표현 수치를 전달한다.
    [Serializable]
    public struct CameraShakeSetting
    {
        [SerializeField, Min(0.01f)]
        private float _duration;

        [SerializeField]
        private Vector3 _positionAmplitude;

        [SerializeField]
        private Vector3 _rotationAmplitude;

        [SerializeField, Min(0.01f)]
        private float _frequency;

        public float Duration => _duration;
        public Vector3 PositionAmplitude => _positionAmplitude;
        public Vector3 RotationAmplitude => _rotationAmplitude;
        public float Frequency => _frequency;

        public bool IsValid =>
            _duration > 0f &&
            _frequency > 0f &&
            (_positionAmplitude.sqrMagnitude > 0f ||
             _rotationAmplitude.sqrMagnitude > 0f);
    }
}
