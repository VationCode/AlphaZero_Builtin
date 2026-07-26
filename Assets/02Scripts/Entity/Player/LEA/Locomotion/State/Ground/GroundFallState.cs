using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class GroundFallState : StateBase
    {
        public GroundFallState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }
        public override EStateType Type => EStateType.Fall;
        protected override void Enter()
        {
            _Core.AnimationView.PlayFall();
        }
        protected override void Tick()
        {
            // 낙하 중 수평 이동과 중력 적용
            _Core.LocomotionModule.MoveAirborne(_Core.LocomotionContext.LockedMoveDirection);

            if (_Core.LocomotionModule.IsGrounded && _Core.LocomotionModule.VerticalVelocity <= 0f)
            {
                _StateFlow.ChangeState(EStateType.Land);
            }
        }
        protected override void Exit()
        {

        }
    }
}
