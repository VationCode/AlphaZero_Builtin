namespace Alpha.Player.Locomotion
{
    // 공통 Locomotion Flow에서 Mode 전환을 막는 행동 상태만 판정한다.
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

        // 비행 해제 후 Ground Move 또는 Fall로 진입할 수 있는지 검사한다.
        public bool CanGround(LocomotionContext p_context)
        {
            if (!p_context.CurrentState.HasValue)
                return false;

            if (p_context.CurrentState == ELocoStateType.Dash)
                return false;

            if (p_context.CurrentState == ELocoStateType.Die)
                return false;

            return true;
        }

        // Ground -> Flight
        // 지상모드에서 비행기능 On 입력 시 전환

        // Flight -> Ground
        // 공중이면 Ground/Fall, 접지 중이면 Ground/Move로 진입한다.

        // Ground -> Swim

        // Swim -> Ground

        // Flight -> Swim

        // Swim -> Flight
    }
}

