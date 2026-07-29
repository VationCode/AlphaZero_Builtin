using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class FlightStateFlow : StateFlowBase
    {
        private Dictionary<ELocoStateType, StateBase> _stateDict;
        public FlightStateFlow(PlayerCore p_core) : base(p_core)
        {
            _stateDict = new Dictionary<ELocoStateType, StateBase>
            {
                { ELocoStateType.Rising, new FlightRisingState(p_core, this) },
                { ELocoStateType.Move, new FlightMoveState(p_core, this) },
                { ELocoStateType.Fall, new FlightFallState(p_core, this) },
                { ELocoStateType.Dash, new FlightDashState(p_core, this) }
            };
        }

        protected override StateBase GetState(ELocoStateType p_stateType)
        {
            return _stateDict[p_stateType];
        }

        internal override bool CanChangeMode(out ELocomotionMode p_nextMode, out ELocoStateType p_entryState)
        {
            if (_Core.Input.IsFlight && _Rule.CanGround(_Core.LocomotionContext))
            {
                p_nextMode = ELocomotionMode.Ground;
                p_entryState = ELocoStateType.Move;
                return true;
            }

            p_nextMode = default;
            p_entryState = default;
            return false;
        }
    }
}