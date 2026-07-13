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
        _Core.Context.LocomotionType = _Core.StateMachine.CurrentLoco;
        _Core.Context.CombatType = _Core.StateMachine.CurrentCombat;
    }
    public abstract void Update();
    public abstract void Exit();
}
