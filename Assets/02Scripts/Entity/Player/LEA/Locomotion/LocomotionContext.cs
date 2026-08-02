using System;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class LocomotionContext
    {
        // 현재 적용 중인 이동 정책 식별값
        public ELocomotionMode CurrentMode { get; internal set; }
        public ELocoStateType? CurrentState { get; internal set; }

        // 환경·능력 조건
        public bool IsGrounded { get; internal set; }
        
        public Vector3 Velocity { get; internal set; }

        // 고정방향
        public Vector3 LockedMoveDirection { get; internal set; }

        // 현재 Locomotion 상태가 확정되었음을 외부 View에 알린다.
        public event Action<ELocomotionMode, ELocoStateType> OnStateChanged;

        internal void SetCurrentMode(ELocomotionMode p_mode)
        {
            CurrentMode = p_mode;
        }

        internal void SetCurrentState(ELocoStateType p_state)
        {
            CurrentState = p_state;
            OnStateChanged?.Invoke(CurrentMode, p_state);
        }
    }
}