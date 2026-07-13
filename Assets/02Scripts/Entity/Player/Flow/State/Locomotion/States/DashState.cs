using UnityEngine;

public class DashState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.LocoModule.StartDash();
        _Core.Context.IsDash = true;
    }
    public override void Update()
    {
        _Core.LocoModule.Dashing();

        if (!_Core.LocoModule.IsDahsing)
            _Core.StateMachine.ChangeLocoState(ELocomotionType.GroundMove);
    }

    public override void Exit()
    {
        _Core.Context.IsDash = false;
    }
}
