using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Aim 카메라의 회전, Zoom 상태와 Pose를 계산한다.
    public class AimViewMode : ICameraViewMode
    {
        private readonly AimViewSO _profile;

        private float _zoomDistance;
        private bool _isInitialized;

        public float FollowSpeed =>
            _profile != null && _profile.ViewSettings != null? _profile.ViewSettings.FollowSpeed : 0f;

        public ECameraViewType ViewType => ECameraViewType.Aim;

        public bool UsesObstruction => false;


        public AimViewMode(AimViewSO p_profile)
        {
            _profile = p_profile;
        }

        public bool TryInitialize(CameraContext p_context)
        {
            if (!HasValidSettings() || p_context == null)
                return false;

            if (_isInitialized)
                return true;

            // ThirdPerson과 분리된 Aim 전용 거리다.
            _zoomDistance = _profile.ViewSettings.DefaultDistance;

            _isInitialized = true;
            return true;
        }

        public bool TryUpdateContext(CameraContext p_context, Vector2 p_lookInput, float p_scrollInput, float p_deltaTime)
        {
            if (!TryInitialize(p_context))
                return false;

            CameraViewSettings view = _profile.ViewSettings;

            CameraOrbitSettings orbit = _profile.OrbitSettings;

            float deltaTime = Mathf.Max(0f, p_deltaTime);

            float pitch = p_context.Pitch - (p_lookInput.y * orbit.LookSensitivity * deltaTime);

            float yaw = p_context.Yaw + (p_lookInput.x * orbit.LookSensitivity * deltaTime);

            p_context.SetRotation(orbit.ClampPitch(pitch), Mathf.Repeat(yaw, 360f));

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

            p_pose = new CameraPose(view.PivotLocalPosition, p_context.PivotRotation, view.ShoulderLocalPosition,
                                    new Vector3(0f, 0f, -_zoomDistance), view.FieldOfView);

            return true;
        }

        private bool HasValidSettings()
        {
            return _profile != null &&
                   _profile.ViewSettings != null &&
                   _profile.OrbitSettings != null;
        }
    }
}
