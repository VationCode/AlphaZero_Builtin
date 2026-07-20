// Mode
public enum ELocomotionMode
{
    Ground,
    Flight,
    Swim

    // 실제 구현할 때 추가
    // Climb,
    // RopeClimb,
    // RopeSwing,
    // Zipline
}


public enum ELocomotionSpace
{
    Planar,
    Spatial
}

// States
public enum ELocomotionState
{
    Idle,
    Move,
    Jump,
    Fall,
    Land,
    Dash,
    Ascend
}

