
using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Player.Locomotion
{
    public class GroundStateFlow : StateFlowBase
    {

        private Dictionary<EStateType, StateBase> _stateDict;

        public GroundStateFlow(PlayerCore p_core) : base(p_core)
        {
            _stateDict = new Dictionary<EStateType, StateBase>
            {
                { EStateType.Move, new GroundMoveState(p_core, this) },
                { EStateType.Jump, new GroundJumpState(p_core, this) },
                { EStateType.Fall, new GroundFallState(p_core, this) },
                { EStateType.Land, new GroundLandState(p_core, this) },
                { EStateType.Dash, new GroundDashState(p_core, this) }
            };
        }

        protected override StateBase GetState(EStateType p_stateType)
        {
            return _stateDict[p_stateType];
        }

        internal override bool CanChangeMode(out ELocomotionMode p_nextMode, out EStateType p_entryState)
        {
            if (_Core.Input.IsFlight && _Rule.CanFlight(_Core.LocomotionContext))
            {
                p_nextMode = ELocomotionMode.Flight;
                p_entryState = EStateType.Rising;
                return true;
            }

            p_nextMode = default;
            p_entryState = default;
            return false;
        }
    }
}
