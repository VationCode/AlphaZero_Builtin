using UnityEngine;

public class CombatRule
{
    public bool CanAttack(StateContext p_context)
    {
        if (p_context.IsJump || p_context.IsDash || p_context.IsFall) return false;
        return true;
    }
    public bool CanAim(StateContext p_context)
    {
        if (p_context.IsJump || p_context.IsDash || p_context.IsFall) return false;
        return true;
    }
    public bool CanInCombat(StateContext p_context)
    {
        if (p_context.IsJump || p_context.IsDash || p_context.IsFall) return false;
        return true;
    }
    public bool CanSwap(StateContext p_context)
    {
        if (p_context.IsJump || p_context.IsDash ||
            p_context.IsFall || p_context.IsAim ||
            p_context.IsAttack)
        {
            return false;
        }

        return true;
    }
}
