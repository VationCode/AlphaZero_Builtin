

using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class GroundLandState : StateBase
    {
        public GroundLandState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }
        public override EStateType Type => EStateType.Land;
        private float _elapsedTime;

        protected override void Enter()
        {
            _elapsedTime = 0;
            _Core.AnimationView.PlayLand();
        }
        protected override void Tick()
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime < _Core.LocomotionModule.LandDuration) return;

            _StateFlow.ChangeState(EStateType.Move);
        }

        protected override void Exit()
        {
        }
    }
}
