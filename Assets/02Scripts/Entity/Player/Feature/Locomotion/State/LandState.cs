using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // Ground Mode의 착지 표현 시간과 일반 이동 복귀를 담당한다.
    public sealed class LandState : StateBase
    {
        private float _elapsedTime;

        public LandState(
            PlayerCore p_core,
            LocomotionStateFlow p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Land;

        protected override void Enter()
        {
            _elapsedTime = 0f;
            _Core.AnimationView.PlayLand();
        }

        protected override void Tick()
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _Core.LocomotionModule.LandDuration)
                _StateFlow.ChangeState(ELocoStateType.Move);
        }

        protected override void Exit()
        {
            _elapsedTime = 0f;
        }
    }
}
