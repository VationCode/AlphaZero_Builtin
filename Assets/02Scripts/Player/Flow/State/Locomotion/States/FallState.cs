using UnityEngine;

public class FallState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsFall = true;
    }
    public override void Update()
    {
        _Core.LocoModule.PlayCharacterController(_Core.LocoModule.CurrentVelocity);

        if (_Core.LocoModule.IsGround)
            _Core.StateMachine.ChangeLocoState(ELocomotionType.GroundMove);
    }

    public override void Exit()
    {
        _Core.Context.IsFall = false;
    }
}
