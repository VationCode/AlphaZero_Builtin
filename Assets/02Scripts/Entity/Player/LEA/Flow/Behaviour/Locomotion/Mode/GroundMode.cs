
namespace Alpha.Player.Locomotion
{
    public class GroundMode : ILocomotionMode
    {
        public ELocomotionMode Type => ELocomotionMode.Ground;
        public ELocomotionSpace MovementSpace => ELocomotionSpace.Planar;
        public float GravityScale => 1f;
    }
}
