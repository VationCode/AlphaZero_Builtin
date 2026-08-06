using System;
using System.Collections.Generic;

namespace Alpha.Player.Locomotion
{
    // TransitionRule 상태 전환 조건을 판정한다.
    public class TransitionRule
    {
        // 현재 상태가 비행 진입을 막지 않는지 검사한다.
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

        // 접지 상태이며 지상 진입을 막는 State가 아닌지 검사한다.
        public bool CanGround(LocomotionContext p_context)
        {
            if (!p_context.CurrentState.HasValue)
                return false;

            // Dash·공중·사망 상태에서는 Ground Mode 진입을 허용하지 않는다.
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

