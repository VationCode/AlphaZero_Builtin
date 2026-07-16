using UnityEngine;

public abstract class BaseState
{
    protected PlayerCore _Core;

    public virtual void Initialize(PlayerCore p_core)
    {
        _Core = p_core;
    }

    public virtual void Enter()
    {
        _Core.Context.LocomotionType = _Core.StateMachineFlow.CurrentLoco;
        _Core.Context.CombatType = _Core.StateMachineFlow.CurrentCombat;
    }
    public abstract void Update();
    public abstract void Exit();
}
