namespace Alpha.Player
{
    public interface ILocomotionStateFlow
    {
        ELocomotionMode Mode { get; }

        void Tick();
        void Exit();
    }
}