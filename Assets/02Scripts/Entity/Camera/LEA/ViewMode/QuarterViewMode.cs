using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Quarter 카메라의 Zoom 상태와 고정 Pose를 계산한다.
    public class QuarterViewMode : ICameraViewMode
    {
        private readonly QuarterViewSO _profile;

        private float _zoomDistance;
        private bool _isInitialized;

        public float FollowSpeed =>
            _profile != null && _profile.ViewSettings != null? _profile.ViewSettings.FollowSpeed : 0f;

        public ECameraViewType ViewType => ECameraViewType.Quarter;

        public bool UsesObstruction => false;

        public QuarterViewMode(QuarterViewSO p_profile)
        {
            _profile = p_profile;
        }

        public bool TryInitialize(CameraContext p_context)
        {
            if (!HasValidSettings() || p_context == null)
                return false;

            if (_isInitialized)
                return true;

            _zoomDistance = _profile.ViewSettings.DefaultDistance;

            _isInitialized = true;
            return true;
        }

        public bool TryUpdateContext(CameraContext p_context, Vector2 p_lookInput, float p_scrollInput, float p_deltaTime)
        {
            if (!TryInitialize(p_context))
                return false;

            CameraViewSettings view = _profile.ViewSettings;

            float deltaTime = Mathf.Max(0f, p_deltaTime);

            // QuarterView에서는 Look 입력을 사용하지 않는다.
            _zoomDistance -= (p_scrollInput * view.ZoomSpeed * deltaTime);

            _zoomDistance = Mathf.Clamp(_zoomDistance, view.MinDistance, view.MaxDistance);

            return true;
        }

        public bool TryCreatePose(CameraContext p_context, out CameraPose p_pose)
        {
            p_pose = default;

            if (!_isInitialized || !HasValidSettings() || p_context == null)
            {
                return false;
            }

            CameraViewSettings view = _profile.ViewSettings;

            // 월드 +Z가 화면의 위쪽이 되도록 카메라 Up 기준을 고정한다.
            Vector3 viewDirection = Quaternion.AngleAxis(_profile.PitchAngle, Vector3.right) * Vector3.forward;
            Quaternion pivotWorldRotation = Quaternion.LookRotation(viewDirection, Vector3.forward);

            p_pose = new CameraPose(view.PivotLocalPosition, pivotWorldRotation, view.ShoulderLocalPosition,
                                    new Vector3(0f, 0f, -_zoomDistance), view.FieldOfView);

            return true;
        }

        private bool HasValidSettings()
        {
            return _profile != null && _profile.ViewSettings != null;
        }
    }
}
