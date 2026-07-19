using UnityEngine;

// View별 설정 데이터
namespace Alpha.AlphaCamera
{
    [CreateAssetMenu(fileName = "CameraView", menuName = "ScriptableObj/AlphaCamera/View")]
    public class CameraViewSO : ScriptableObject
    {
        [Header("View")]
        [SerializeField] private ECameraViewType _viewType;

        [Header("Pose")]
        [SerializeField] private float _pivotOffsetY;
        [SerializeField] private float _pivotAngle;
        [SerializeField] private float _shoulderOffsetX;
        [SerializeField] private float _zoomMinDistance = 1f;
        [SerializeField] private float _zoomMaxDistance = 5f;
        [SerializeField] private float _fieldOfView = 60f;

        [Header("Input")]
        [SerializeField] private float _lookSensitivity = 15f;
        [SerializeField] private float _minPitch = -70f;
        [SerializeField] private float _maxPitch = 70f;
        [SerializeField] private float _zoomScrollSpeed = 1f;

        [Header("Movement")]
        [SerializeField] private float _followSpeed = 10f;

        [Header("Transition")]
        [SerializeField] private float _transitionDuration = 0.25f;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public ECameraViewType ViewType => _viewType;

        public float ZoomMinDistance => _zoomMinDistance;
        public float ZoomMaxDistance => _zoomMaxDistance;

        public float LookSensitivity => _lookSensitivity;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
        public float ZoomScrollSpeed => _zoomScrollSpeed;

        public float FollowSpeed => _followSpeed;

        public float TransitionDuration => _transitionDuration;

        public AnimationCurve TransitionCurve => _transitionCurve;

        public CameraPose CreatePose(Quaternion p_pivotRotation, float p_zoomDistance)
        {
            float zoomDistance = Mathf.Clamp(p_zoomDistance, _zoomMinDistance, _zoomMaxDistance);

            return new CameraPose(new Vector3(0f, _pivotOffsetY, 0f), p_pivotRotation,
                                  new Vector3(_shoulderOffsetX, 0f, 0f), Vector3.back * zoomDistance,
                                  _fieldOfView);
        }

        public CameraPose CreateDefaultPose()
        {
            return CreatePose(Quaternion.Euler(_pivotAngle, 0f, 0f), _zoomMaxDistance);
        }

        private void OnValidate()
        {
            _zoomMinDistance = Mathf.Max(0f, _zoomMinDistance);

            _zoomMaxDistance = Mathf.Max(_zoomMinDistance, _zoomMaxDistance);

            _minPitch = Mathf.Clamp(_minPitch, -89f, 89f);

            _maxPitch = Mathf.Clamp(_maxPitch, _minPitch, 89f);

            _fieldOfView = Mathf.Clamp(_fieldOfView, 1f, 179f);

            _followSpeed = Mathf.Max(0f, _followSpeed);

            _transitionDuration = Mathf.Max(0f, _transitionDuration);
        }
    }
}
