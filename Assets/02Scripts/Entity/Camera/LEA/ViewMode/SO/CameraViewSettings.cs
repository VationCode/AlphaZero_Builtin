using System;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    // 모든 Camera ViewMode가 공유하는 구도와 Zoom 설정이다.
    [Serializable]
    public class CameraViewSettings
    {
        [Header("Pose")]
        [SerializeField] private Vector3 _pivotLocalPosition = new(0f, 1.8f, 0f);

        [SerializeField] private Vector3 _shoulderLocalPosition = new(0f, 0f, 0f);

        [SerializeField] private float _defaultDistance = 3f;
        [SerializeField] private float _minDistance = 1f;
        [SerializeField] private float _maxDistance = 5f;
        [SerializeField] private float _fieldOfView = 60f;

        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 1f;

        [Header("Follow")]
        [SerializeField] private float _followSpeed = 10f;

        public Vector3 PivotLocalPosition =>
            _pivotLocalPosition;

        public Vector3 ShoulderLocalPosition =>
            _shoulderLocalPosition;

        public float DefaultDistance => _defaultDistance;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public float FieldOfView => _fieldOfView;

        public float ZoomSpeed => _zoomSpeed;
        public float FollowSpeed => _followSpeed;

        internal void Validate()
        {
            _minDistance = Mathf.Max(0f, _minDistance);
            _maxDistance = Mathf.Max(_minDistance, _maxDistance);

            _defaultDistance = Mathf.Clamp(_defaultDistance, _minDistance, _maxDistance);

            _fieldOfView = Mathf.Clamp(_fieldOfView, 1f, 179f);

            _zoomSpeed = Mathf.Max(0f, _zoomSpeed);
            _followSpeed = Mathf.Max(0f, _followSpeed);
        }
    }
}
