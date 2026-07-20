using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class LocomotionContext
    {
        // 현재 적용 중인 이동 정책 식별값
        public ELocomotionMode CurrentMode { get; internal set; }

        // 현재 행동 흐름의 상태
        public ELocomotionState CurrentState { get; internal set; }

        // 환경과 물리 상태
        public bool IsGrounded { get; internal set; }
        public bool IsSubmerged { get; internal set; }
        public Vector3 Velocity { get; internal set; }

        // 현재 행동 상태(CurrentState와 중복)
        /*public bool IsJumping { get; internal set; }
        public bool IsMoving { get; internal set; }*/
    }
}