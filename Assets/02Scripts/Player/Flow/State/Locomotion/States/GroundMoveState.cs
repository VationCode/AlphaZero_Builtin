using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GroundMoveState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsGroundMove = true;
    }
    public override void Update()
    {
        bool isSprint = _Core.InputBoundary.IsSprint;
        bool isJump = _Core.InputBoundary.IsJump;
        bool isDash = _Core.InputBoundary.IsDash;
        Vector3 moveInput = _Core.InputBoundary.MoveInput;

        bool isCombat = _Core.Context.IsInCombat; 
        // CanMove

        _Core.Context.IsGroundMove = moveInput.magnitude > 0;

        if(isSprint && _Core.LocoRule.CanSprint(_Core.Context))
            _Core.Context.IsSprint = true;
        else 
            _Core.Context.IsSprint = false;

        _Core.LocoModule.GroundMoveMove(moveInput, _Core.Context.IsSprint, isCombat);

        
        /*if(_Core.LocoModule.IsGround)
        {
            _Core.StateMachine.ChangeLocoState(ELocomotionType.Fall);
        }
        else*/ if (isJump && _Core.LocoRule.CanJump(_Core.Context))
        {
            _Core.StateMachine.ChangeLocoState(ELocomotionType.Jump);
        }
        else if (isDash && _Core.LocoRule.CanDash(_Core.Context))
        {
            _Core.StateMachine.ChangeLocoState(ELocomotionType.Dash);
        }
    }

    public override void Exit()
    {
        _Core.Context.IsGroundMove = false;
    }
}
