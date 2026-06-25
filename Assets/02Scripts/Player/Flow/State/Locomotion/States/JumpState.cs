using UnityEngine;

public class JumpState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.LocoModule.Jump();
        _Core.Context.IsJump = true;
    }
    public override void Update()
    {
        _Core.LocoModule.PlayCharacterController(_Core.LocoModule.CurrentVelocity);

        if (_Core.LocoModule.IsVelocityYZero())
            _Core.StateMachine.ChangeLocoState(ELocomotionType.Fall);
    }

    public override void Exit()
    {
        _Core.Context.IsJump = false;
    }
}
