using UnityEngine;
using static UnityEditor.Timeline.TimelinePlaybackControls;

public class LocomotionRule
{
    public bool CanMove(StateContext p_context)
    {
        return true;
    }
    public bool CanSprint(StateContext p_context)
    {
        if (p_context.IsInCombat) return false;
        return true;
    }
    public bool CanJump(StateContext p_context)
    {
        if (p_context.IsAim) return false;
        return true;
    }
    public bool CanDash(StateContext p_context)
    {
        if (p_context.IsAim) return false;
        return true;
    }
}
