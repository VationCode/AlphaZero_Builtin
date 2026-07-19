using System;
using UnityEngine;
using Alpha.Mouse;

// 해당 View의 입력과 목표 Pose 결정
namespace Alpha.AlphaCamera
{
    public class ThirdPersonCameraView : ICameraView
    {
        public ECameraViewType Type => ECameraViewType.ThirdPerson;

        public CameraViewSO Profile { get; }

        private readonly CameraRigModule _rigModule;
        private readonly MouseSystem _mouseSystem;
        public ThirdPersonCameraView(CameraViewSO p_profile, CameraRigModule p_rigModule, MouseSystem mouseSystem)
        {
            Profile = p_profile ?? throw new ArgumentNullException(nameof(p_profile));

            _rigModule = p_rigModule ?? throw new ArgumentNullException(nameof(p_rigModule));
            _mouseSystem = mouseSystem;
        }

        public void Enter(CameraContext p_context)
        {
            if (p_context.ZoomDistance <= 0f)
            {
                p_context.ZoomDistance = Profile.ZoomMaxDistance;
            }

            p_context.ZoomDistance = Mathf.Clamp(p_context.ZoomDistance, Profile.ZoomMinDistance, Profile.ZoomMaxDistance);

            _mouseSystem.SetViewCursor(false);
        }

        public void Update(CameraContext p_context, AlphaInputSystem p_input, float p_deltaTime)
        {
            _rigModule.Rotate(p_context, p_input.LookInput, Profile, p_deltaTime);

            _rigModule.Zoom(p_context, p_input.MouseScroll.y, Profile);
        }

        public void Exit(CameraContext p_context)
        {
        }

        public CameraPose GetTargetPose(CameraContext p_context)
        {
            return Profile.CreatePose(p_context.PivotRotation, p_context.ZoomDistance);
        }
    }
}