using UnityEngine;
namespace Alpha.AlphaCamera
{
    public interface ICameraView
    {
        ECameraViewType Type { get; }
        CameraViewSO Profile { get; }

        void Enter(CameraContext p_context);

        void Update(CameraContext p_context, AlphaInputSystem p_input, float p_deltaTime);

        void Exit(CameraContext p_context);

        CameraPose GetTargetPose(CameraContext p_context);
    }
}
