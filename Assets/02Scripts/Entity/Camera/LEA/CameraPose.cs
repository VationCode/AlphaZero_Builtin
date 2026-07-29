using UnityEngine;


namespace Alpha.AlphaCamera
{
    // ViewMode가 계산하여 Rig에 전달하는 최종 구도
    public readonly struct CameraPose
    {
        public Vector3 PivotLocalPosition { get; }
        public Quaternion PivotWorldRotation { get; }
        public Vector3 ShoulderLocalPosition { get; }
        public Vector3 CameraLocalPosition { get; }
        public float FieldOfView { get; }

        public CameraPose(Vector3 p_pivotLocalPosition, Quaternion p_pivotWorldRotation,
            Vector3 p_shoulderLocalPosition, Vector3 p_cameraLocalPosition, float p_fieldOfView)
        {
            PivotLocalPosition = p_pivotLocalPosition;
            PivotWorldRotation = p_pivotWorldRotation;
            ShoulderLocalPosition = p_shoulderLocalPosition;
            CameraLocalPosition = p_cameraLocalPosition;
            FieldOfView = p_fieldOfView;
        }

        // 현재 Pose와 목표 Pose 사이의 전체 카메라 구도를 보간한다.
        public static CameraPose Lerp(CameraPose p_from, CameraPose p_to, float p_ratio)
        {
            float ratio = Mathf.Clamp01(p_ratio);

            return new CameraPose(
                Vector3.Lerp(p_from.PivotLocalPosition, p_to.PivotLocalPosition,ratio),
                Quaternion.Slerp(p_from.PivotWorldRotation, p_to.PivotWorldRotation, ratio),
                Vector3.Lerp(p_from.ShoulderLocalPosition, p_to.ShoulderLocalPosition, ratio),
                Vector3.Lerp(p_from.CameraLocalPosition, p_to.CameraLocalPosition, ratio),
                Mathf.Lerp(p_from.FieldOfView, p_to.FieldOfView, ratio));
        }
    }
}
