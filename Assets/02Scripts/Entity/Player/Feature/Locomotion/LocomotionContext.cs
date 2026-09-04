using System;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // LocomotionContext 기능의 런타임 상태를 보관하고 변경을 알린다.
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

        // SetCurrentMode 상태 값을 갱신한다.
        internal void SetCurrentMode(ELocomotionMode p_mode)
        {
            CurrentMode = p_mode;
        }

        // SetCurrentState 상태 값을 갱신한다.
        internal void SetCurrentState(ELocoStateType p_state)
        {
            CurrentState = p_state;
            OnStateChanged?.Invoke(CurrentMode, p_state);
        }

        // State가 유지된 Mode 전환도 View가 새 Mode와 함께 다시 평가하도록 알린다.
        internal void NotifyCurrentState()
        {
            if (CurrentState.HasValue)
                OnStateChanged?.Invoke(CurrentMode, CurrentState.Value);
        }
    }
}
