using UnityEngine;


// Rig에 적용할 카메라 구도
namespace Alpha.AlphaCamera
{
    public readonly struct CameraPose
    {
        public Vector3 PivotPosition { get; }
        public Quaternion PivotRotation { get; }
        public Vector3 ShoulderPosition { get; }
        public Vector3 ZoomPosition { get; }
        public float FieldOfView { get; }

        public CameraPose(Vector3 p_pivotPosition, Quaternion p_pivotRotation,
                          Vector3 p_shoulderPosition, Vector3 p_zoomPosition, float p_fieldOfView)
        {
            PivotPosition = p_pivotPosition;
            PivotRotation = p_pivotRotation;
            ShoulderPosition = p_shoulderPosition;
            ZoomPosition = p_zoomPosition;
            FieldOfView = p_fieldOfView;
        }

        public static CameraPose Lerp(
                CameraPose p_from,
                CameraPose p_to,
                float p_t)
        {
            float t = Mathf.Clamp01(p_t);

            return new CameraPose(Vector3.Lerp(p_from.PivotPosition, p_to.PivotPosition,t),
                                  Quaternion.Slerp(p_from.PivotRotation, p_to.PivotRotation,t),
                                  Vector3.Lerp(p_from.ShoulderPosition, p_to.ShoulderPosition, t),
                                  Vector3.Lerp(p_from.ZoomPosition, p_to.ZoomPosition, t),
                                  Mathf.Lerp(p_from.FieldOfView, p_to.FieldOfView, t));
        }
    }
}
