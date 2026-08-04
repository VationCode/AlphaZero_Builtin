using System;
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
        // 마지막으로 전환이 완료된 View
        public ECameraViewType CurrentViewType { get; private set; }

        // 현재 전환의 출발점과 목표
        public ECameraViewType TransitionFromViewType { get; private set; }
        public ECameraViewType TargetViewType { get; private set; }

        public float Pitch { get; private set; }
        public float Yaw { get; private set; }
        public float ZoomDistance { get; private set; }

        public Quaternion PivotRotation => Quaternion.Euler(Pitch, Yaw, 0f);

        internal void SetViewType(ECameraViewType p_viewType)
        {
            CurrentViewType = p_viewType;
        }

        internal void SetRotation(float p_pitch, float p_yaw)
        {
            Pitch = p_pitch;
            Yaw = p_yaw;
        }

        internal void SetZoomDistance(float p_zoomDistance)
        {
            ZoomDistance = p_zoomDistance;
        }
    }
}