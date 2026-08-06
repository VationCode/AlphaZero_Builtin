

using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // GroundLandState 상태의 진입, 갱신, 종료 동작을 담당한다.
    public class GroundLandState : StateBase
    {
        // 전달받은 값으로 초기 상태를 구성한다.
        public GroundLandState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }
        public override ELocoStateType Type => ELocoStateType.Land;
        private float _elapsedTime;

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected override void Enter()
        {
            _elapsedTime = 0;
            _Core.AnimationView.PlayLand();
        }
        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime < _Core.LocomotionModule.LandDuration) return;

            _StateFlow.ChangeState(ELocoStateType.Move);
        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
        }
    }
}
