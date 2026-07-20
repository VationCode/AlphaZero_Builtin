namespace Alpha.Player.Locomotion
{
    public class FlightMode : ILocomotionMode
    {
        public ELocomotionMode Type => ELocomotionMode.Flight;
        public ELocomotionSpace MovementSpace => ELocomotionSpace.Spatial;
        public float GravityScale => 0f;
    }
}
