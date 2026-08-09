using UnityEngine;
namespace Alpha.AlphaCamera
{
    /// <summary>
    /// 하나의 Camera ViewType이 사용할 기본 구도 정보를 보관한다.
    /// 런타임 상태와 전환 계산은 보관하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraView", menuName = "ScriptableObject/Camera/View")]
    public class CameraViewSO : ScriptableObject
    {
        [Header("Type")]
        [SerializeField] private ECameraViewType _viewType;

        [Header("Pose")]
        [SerializeField] private Vector3 _pivotLocalPosition;
        [SerializeField] private Vector3 _shoulderLocalPosition;
        [SerializeField] private Vector3 _pivotEulerAngles;

        [Header("Camera")]
        [SerializeField, Min(0f)] private float _zoomDistance = 5f;
        [SerializeField, Min(0f)] private float _minZoomDistance = 1f;
        [SerializeField, Min(0f)] private float _maxZoomDistance = 10f;
        [SerializeField, Min(0f)] private float _zoomStep = 0.5f;

        [SerializeField, Range(1f, 179f)] private float _fieldOfView = 60f;
        [SerializeField, Min(0f)] private float _rigFollowSpeed = 10f;

        [Header("Input")]
        [SerializeField, Min(0f)]
        private float _lookSensitivityMultiplier = 1f;

        public ECameraViewType ViewType => _viewType;
       
        public Vector3 PivotLocalPosition => _pivotLocalPosition;
        public Vector3 PivotEulerAngles => _pivotEulerAngles;
        public Vector3 ShoulderLocalPosition => _shoulderLocalPosition;

        public float ZoomDistance => _zoomDistance;
        public float MinZoomDistance => _minZoomDistance;
        public float MaxZoomDistance => _maxZoomDistance;
        public float ZoomStep => _zoomStep;
        public float FieldOfView => _fieldOfView;
        public float RigFollowSpeed => _rigFollowSpeed;

        public float LookSensitivityMultiplier => _lookSensitivityMultiplier;
        // Inspector 값이 유효한 범위를 유지하도록 보정한다.
        private void OnValidate()
        {
            _minZoomDistance = Mathf.Max(0f, _minZoomDistance);
            _maxZoomDistance = Mathf.Max(_minZoomDistance, _maxZoomDistance);
            _zoomDistance = Mathf.Clamp(_zoomDistance, _minZoomDistance, _maxZoomDistance);
            _zoomStep = Mathf.Max(0f, _zoomStep);

            _fieldOfView = Mathf.Clamp(_fieldOfView, 1f, 179f);
            _rigFollowSpeed = Mathf.Max(0f, _rigFollowSpeed);
        }
    }
}