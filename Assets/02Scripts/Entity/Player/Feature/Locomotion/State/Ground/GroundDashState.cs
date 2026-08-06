using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // GroundDashState 상태의 진입, 갱신, 종료 동작을 담당한다.
    public class GroundDashState : StateBase
    {
        private float _elapsedTime;

        // 전달받은 값으로 초기 상태를 구성한다.
        public GroundDashState(
            PlayerCore p_core,
            StateFlowBase p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type =>
            ELocoStateType.Dash;

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected override void Enter()
        {
            Transform cameraTr = _Core.CameraCore?.RenderCamera.transform;

            // 진입 순간 입력 방향을 고정해 대시 도중 방향이 흔들리지 않게 한다.
            _Core.LocomotionModule.StartDash(
                _Input.MoveInput,
                cameraTr);

            _elapsedTime = 0f;
            _Core.AnimationView.PlayDash();
        }

        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            // 고정된 방향으로 대시를 유지하면서 경과 시간을 누적한다.
            _Core.LocomotionModule.DashUpdate(
                _Core.LocomotionContext.LockedMoveDirection);

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime <
                _Core.LocomotionModule.DashDuration)
            {
                return;
            }

            // 대시 종료 시 접지 여부에 따라 이동 또는 낙하로 전환한다.
            ELocoStateType nextState =
                _Core.LocomotionModule.IsGrounded
                    ? ELocoStateType.Move
                    : ELocoStateType.Fall;

            _StateFlow.ChangeState(nextState);
        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
        }
    }
}
