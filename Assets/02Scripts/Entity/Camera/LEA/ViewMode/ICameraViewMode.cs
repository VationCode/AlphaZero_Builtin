using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Camera ViewMode가 제공해야 하는 공통 동작이다.
    public interface ICameraViewMode
    {
        ECameraViewType ViewType { get; }
        float FollowSpeed { get; }

        bool TryInitialize(CameraContext p_context);

        bool TryUpdateContext(CameraContext p_context, Vector2 p_lookInput, float p_scrollInput, float p_deltaTime);

        bool TryCreatePose(CameraContext p_context, out CameraPose p_pose);

        /// <summary>
        /// 플레이어와 카메라간의 장애물 판단 여부
        /// </summary>
        bool UsesObstruction { get; }
    }
}
