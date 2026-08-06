using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // GroundJumpState 상태의 진입, 갱신, 종료 동작을 담당한다.
    public class GroundJumpState : StateBase
    {
        // 전달받은 값으로 초기 상태를 구성한다.
        public GroundJumpState(
            PlayerCore p_core,
            StateFlowBase p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type =>
            ELocoStateType.Jump;

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected override void Enter()
        {
            Transform cameraTr = _Core.CameraCore?.RenderCamera.transform;

            bool isSprint = _Input.IsSprint;

            _Core.LocomotionModule.StartJump(
                _Input.MoveInput,
                cameraTr,
                isSprint);

            _Core.AnimationView.PlayJump();
        }

        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            _Core.LocomotionModule.MoveAirborne(
                _Core.LocomotionContext.LockedMoveDirection);

            if (_Core.LocomotionModule.VerticalVelocity <= 0f)
                _StateFlow.ChangeState(ELocoStateType.Fall);
        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
        }
    }
}
