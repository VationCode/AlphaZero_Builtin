using System;
using UnityEngine;
using Alpha.Mouse;

namespace Alpha.AlphaCamera
{
    public class QuarterCameraView : ICameraView
    {
        public ECameraViewType Type => ECameraViewType.Quarter;

        public CameraViewSO Profile { get; }

        private readonly MouseSystem _mouseSystem;

        public QuarterCameraView(CameraViewSO p_profile, MouseSystem p_mouseSystem)
        {
            Profile = p_profile;
            _mouseSystem = p_mouseSystem;
        }

        public void Enter(CameraContext p_context)
        {
            _mouseSystem.SetViewCursor(true);
        }

        public void Update(CameraContext p_context, AlphaInputSystem p_input, float p_deltaTime)
        {
        }

        public void Exit(CameraContext p_context)
        {
            _mouseSystem.SetViewCursor(false);
        }

        public CameraPose GetTargetPose(CameraContext p_context)
        {
            return Profile.CreateDefaultPose();
        }
    }
}