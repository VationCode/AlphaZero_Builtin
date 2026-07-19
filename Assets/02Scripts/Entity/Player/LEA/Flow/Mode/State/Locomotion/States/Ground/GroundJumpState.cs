
namespace Alpha.Player
{
    public class GroundJumpState : GroundLocomotionState
    {
        public GroundJumpState(PlayerCore p_core, GroundLocomotionStateFlow p_flow) : base(p_core, p_flow) { }
        public override void Tick()
        {
            // Ground 이동과 상태 전환을 판단한다.
        }
    }
}