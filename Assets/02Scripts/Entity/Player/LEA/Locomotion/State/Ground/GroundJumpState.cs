
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class GroundJumpState : StateBase
    {
        public GroundJumpState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }
        public override EStateType Type => EStateType.Jump;

        protected override void Enter()
        {
            Transform cameraTr = _Core.CameraCore.RenderCamera.transform;

            bool isSprint = _Input.IsSprint;

            // 점프 직전의 수평 속도 보존
            _Core.LocomotionModule.StartJump(_Input.MoveInput, cameraTr, isSprint);

            _Core.AnimationView.PlayJump();
        }
        protected override void Tick()
        {

            bool isCombat = !_Core.BlockCombat && _Input.IsAim;

            // 공중에서도 수평 이동 유지
            //_Core.LocomotionModule.Movement(_Input.MoveInput, cameraTr, _Input.IsSprint, isCombat, ELocomotionMode.Ground);

            _Core.LocomotionModule.MoveAirborne(_Core.LocomotionContext.LockedMoveDirection);

            // 상승이 끝나면 낙하
            if (_Core.LocomotionModule.VerticalVelocity <= 0f)
            {
                _StateFlow.ChangeState(EStateType.Fall);
            }
        }
        protected override void Exit()
        {

        }
    }
}
