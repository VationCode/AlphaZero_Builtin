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

        /// <summary>
        /// 고정 방향
        /// </summary>
        public Vector3 LockedMoveDirection { get; internal set; }
    }
}