
namespace Alpha.Player.Locomotion
{
    // 현재 모드가 제공하는 고정된 이동 정책
    public interface ILocomotionMode
    {
        // 현재 이동 모드
        ELocomotionMode Type { get; }

        // 평면 또는 3차원 이동
        ELocomotionSpace MovementSpace { get; }

        // 중력 적용 비율
        float GravityScale { get; }
    }
}