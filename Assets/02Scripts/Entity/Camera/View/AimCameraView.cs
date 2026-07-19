using Alpha.Mouse;
using System;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    public class AimCameraView : ICameraView
    {
        public ECameraViewType Type => ECameraViewType.Aim;

        public CameraViewSO Profile { get; }
        private readonly CameraRigModule _rigModule;
        private readonly MouseSystem _mouseSystem;

        public AimCameraView(CameraViewSO p_profile, CameraRigModule p_rigModule, MouseSystem mouseSystem)
        {
            Profile = p_profile ?? throw new ArgumentNullException(nameof(p_profile));

            _rigModule = p_rigModule ?? throw new ArgumentNullException(nameof(p_rigModule));
            _mouseSystem = mouseSystem;
        }

        public void Enter(CameraContext p_context)
        {
            _mouseSystem.SetViewCursor(false);
        }

        public void Update(CameraContext p_context, AlphaInputSystem p_input, float p_deltaTime)
        {
            _rigModule.Rotate(p_context, p_input.LookInput, Profile, p_deltaTime);
        }

        public void Exit(CameraContext p_context)
        {
        }

        public CameraPose GetTargetPose(CameraContext p_context)
        {
            return Profile.CreatePose(p_context.PivotRotation, Profile.ZoomMaxDistance);
        }
    }
}