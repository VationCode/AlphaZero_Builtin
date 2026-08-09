using System;
using UnityEngine;
// ECameraViewType 관련 선택 값을 정의한다.
public enum ECameraViewType
{
    ThirdPerson,
    Aim,
    Quarter,
    Scope
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

        public bool IsTransitioning { get; private set; }

        public ECameraViewType EffectiveViewType => IsTransitioning ? TargetViewType : CurrentViewType;
        // SetRotation 상태 값을 갱신한다.
        internal void SetRotation(float p_pitch, float p_yaw)
        {
            Pitch = p_pitch;
            Yaw = p_yaw;
        }

        // SetZoomDistance 상태 값을 갱신한다.
        internal void SetZoomDistance(float p_zoomDistance)
        {
            ZoomDistance = p_zoomDistance;
        }

        // 전환 목표 상태를 기록한다.
        internal void BeginTransition(ECameraViewType p_from, ECameraViewType p_target)
        {
            TransitionFromViewType = p_from;
            TargetViewType = p_target;
            IsTransitioning = true;
        }

        internal void CompleteTransition(ECameraViewType p_viewType)
        {
            CurrentViewType = p_viewType;
            TargetViewType = p_viewType;
            IsTransitioning = false;
        }
    }
}
