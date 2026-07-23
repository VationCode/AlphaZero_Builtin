using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class FlightStateFlow : StateFlowBase
    {
        private Dictionary<EStateType, StateBase> _stateDict;
        public FlightStateFlow(PlayerCore p_core) : base(p_core)
        {
            _stateDict = new Dictionary<EStateType, StateBase>
            {
                { EStateType.Rising, new FlightRisingState(p_core, this) },
                { EStateType.Move, new FlightMoveState(p_core, this) },
                { EStateType.Fall, new FlightFallState(p_core, this) },
                { EStateType.Dash, new FlightDashState(p_core, this) }
            };
        }

        protected override StateBase GetState(EStateType p_stateType)
        {
            return _stateDict[p_stateType];
        }

        internal override bool CanChangeMode(out ELocomotionMode p_nextMode, out EStateType p_entryState)
        {
            if (_Core.Input.IsFlight && _Rule.CanGround(_Core.LocomotionContext))
            {
                p_nextMode = ELocomotionMode.Ground;
                p_entryState = EStateType.Move;
                return true;
            }

            p_nextMode = default;
            p_entryState = default;
            return false;
        }
    }
}