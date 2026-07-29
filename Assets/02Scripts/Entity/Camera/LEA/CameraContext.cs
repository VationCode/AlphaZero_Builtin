using UnityEngine;
public enum ECameraViewType
{
    ThirdPerson,
    Aim,
    Quarter
}

namespace Alpha.AlphaCamera
{
    // 입력, 회전값, 줌값 등 런타임 상태
    public class CameraContext
    {
        public float Pitch { get; private set; }
        public float Yaw { get; private set; }
        public float ZoomDistance { get; private set; }

        public Quaternion PivotRotation => Quaternion.Euler(Pitch, Yaw, 0f);

        public ECameraViewType BaseViewType { get; internal set; } = ECameraViewType.ThirdPerson;

        public ECameraViewType CurrentViewType { get; internal set; } = ECameraViewType.ThirdPerson;
        internal void SetRotation(float p_pitch, float p_yaw)
        {
            Pitch = p_pitch;
            Yaw = p_yaw;
        }

        internal void SetZoomDistance(float p_distance)
        {
            ZoomDistance = Mathf.Max(0f, p_distance);
        }
    }
}