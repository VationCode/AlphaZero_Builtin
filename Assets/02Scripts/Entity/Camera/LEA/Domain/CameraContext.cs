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
        public ECameraViewType CurrentViewType { get; private set; }

        public float Pitch { get; set; }
        public float Yaw { get; set; }
        public float ZoomDistance { get; set; }

        public Quaternion PivotRotation => Quaternion.Euler(Pitch, Yaw, 0f);

        public void ChangeView(ECameraViewType p_viewType)
        {
            CurrentViewType = p_viewType;
        }

        public void ResetRotation()
        {
            Pitch = 0f;
            Yaw = 0f;
        }
    }
}