namespace Alpha.Player.Locomotion
{
    public class SwimMode : ILocomotionMode
    {
        public ELocomotionMode Type => ELocomotionMode.Swim;
        public ELocomotionSpace MovementSpace => ELocomotionSpace.Spatial;
        public float GravityScale => 0f;
    }
}