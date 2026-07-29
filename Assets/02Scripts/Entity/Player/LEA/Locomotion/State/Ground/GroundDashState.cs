using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class GroundDashState : StateBase
    {
        public GroundDashState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }
        public override ELocoStateType Type => ELocoStateType.Dash;

        private float _elapsedTime;
        protected override void Enter()
        {
            Transform cameraTr = Camera.main.transform;

            _Core.LocomotionModule.StartDash(_Input.MoveInput, cameraTr);

            _elapsedTime = 0f;

            _Core.AnimationView.PlayDash();
        }
        protected override void Tick()
        {
            _Core.LocomotionModule.Dash(_Core.LocomotionContext.LockedMoveDirection);

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime < _Core.LocomotionModule.DashDuration)
                return;

            ELocoStateType nextState = _Core.LocomotionModule.IsGrounded? ELocoStateType.Move : ELocoStateType.Fall;

            _StateFlow.ChangeState(nextState);
        }
        protected override void Exit()
        {

        }
    }
}
