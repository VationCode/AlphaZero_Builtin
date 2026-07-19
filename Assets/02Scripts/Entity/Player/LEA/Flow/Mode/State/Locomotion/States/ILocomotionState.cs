
namespace Alpha.Player
{
    public interface ILocomotionState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}