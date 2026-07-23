
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class GroundMoveState : StateBase
    {
        public GroundMoveState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow){}

        public override EStateType Type => EStateType.Move;

        protected override void Enter()
        {
            
        }
        protected override void Tick()
        {
            // 상태 전환을 먼저 처리
            if (_Input.IsDash)
            {
                _StateFlow.ChangeState(EStateType.Dash);
                return;
            }

            if (_Input.IsJump)
            {
                _StateFlow.ChangeState(EStateType.Jump);
                return;
            }

            Transform cameraTr = _Core.CameraCore.RenderCamera.transform;

            bool isSprint = _Input.IsSprint;
            bool isCombat = !_Core.BlockCombat && (_Input.IsAim || _Input.IsAttack);
            Vector2 moveInput = _Input.MoveInput;

            _Core.LocomotionModule.Movement(moveInput, cameraTr, isSprint, isCombat, ELocomotionMode.Ground);

            _Core.AnimationView.PlayGroundLocomotion(moveInput, isSprint, isCombat);
        }
        protected override void Exit()
        {
            
        }
    }
}