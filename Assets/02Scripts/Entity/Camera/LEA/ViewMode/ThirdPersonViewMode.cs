using UnityEngine;

namespace Alpha.AlphaCamera
{
    // ThirdPerson 카메라의 상태 갱신과 Pose 계산을 담당한다.
    public class ThirdPersonViewMode : ICameraViewMode
    {
        private readonly ThirdPersonViewSO _profile;
        private bool _isInitialized;

        public float FollowSpeed =>
            _profile != null && _profile.ViewSettings != null? _profile.ViewSettings.FollowSpeed : 0f;
        public ECameraViewType ViewType => ECameraViewType.ThirdPerson;

        public bool UsesObstruction => true;


        public ThirdPersonViewMode(ThirdPersonViewSO p_profile)
        {
            _profile = p_profile;
        }

        public bool TryInitialize(CameraContext p_context)
        {
            if (!HasValidSettings() || p_context == null)
                return false;

            if (_isInitialized)
                return true;

            p_context.SetRotation(_profile.OrbitSettings.ClampPitch(_profile.InitialPitch), p_context.Yaw);

            p_context.SetZoomDistance(_profile.ViewSettings.DefaultDistance);

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

            float distance = p_context.ZoomDistance - (p_scrollInput * view.ZoomSpeed * deltaTime);

            p_context.SetZoomDistance(Mathf.Clamp(distance, view.MinDistance, view.MaxDistance));

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

            p_pose = new CameraPose( view.PivotLocalPosition, p_context.PivotRotation,
                view.ShoulderLocalPosition, new Vector3(0f, 0f, -p_context.ZoomDistance), view.FieldOfView);

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
