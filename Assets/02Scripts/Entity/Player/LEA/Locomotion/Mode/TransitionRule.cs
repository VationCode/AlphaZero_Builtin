using System;
using System.Collections.Generic;

namespace Alpha.Player.Locomotion
{
    public class TransitionRule
    {
        public bool CanFlight(LocomotionContext p_context)
        {
            if (!p_context.CurrentState.HasValue)
                return false;

            if (p_context.CurrentState == ELocoStateType.Dash)
                return false;

            if (p_context.CurrentState == ELocoStateType.Die)
                return false;

            return true;
        }

        public bool CanGround(LocomotionContext p_context)
        {
            if (!p_context.CurrentState.HasValue)
                return false;

            if (p_context.CurrentState == ELocoStateType.Dash)
                return false;

            if (!p_context.IsGrounded) 
                return false;

            if (p_context.CurrentState == ELocoStateType.Die)
                return false;

            return true;
        }

        // Ground -> Flight
        // 지상모드에서 비행기능 On 입력 시 전환

        // Flight -> Ground
        // 비행기능 Off 입력 혹은 게이지 소모에 의한 Fall상태가 되었을 때 지면에 닿았을 경우

        // Ground -> Swim

        // Swim -> Ground

        // Flight -> Swim

        // Swim -> Flight
    }
}

