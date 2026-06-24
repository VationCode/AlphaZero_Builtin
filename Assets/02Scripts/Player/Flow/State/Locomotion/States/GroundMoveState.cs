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
        bool isSprintPress = _Core.InputBoundary.IsSprint;
        bool isJumpPress = _Core.InputBoundary.IsJump;
        bool isDashPress = _Core.InputBoundary.IsDash;
        Vector3 moveInput = _Core.InputBoundary.MoveInput;

        bool isCombat = _Core.Context.IsInCombat; 
        // CanMove

        _Core.Context.IsGroundMove = moveInput.magnitude > 0;

        if(isSprintPress && _Core.LocoRule.CanSprint(_Core.Context))
            _Core.Context.IsSprint = true;
        else 
            _Core.Context.IsSprint = false;

        _Core.LocoModule.GroundMoveMove(moveInput, _Core.Context.IsSprint, isCombat);

        Vector3 rotTargetDir = Vector3.zero;

        if(isCombat)
        {
            if(_Core.CameraCore.State.CurrentType == EViewType.Quarter)
            {
                Vector2 mousePos = _Core.InputBoundary.MousePos;
                rotTargetDir = _Core.CameraCore.MouseUtility.GetTargetMouseDirection(mousePos, _Core.transform.position);
            }
            else
            {
                rotTargetDir = Camera.main.transform.forward;
                rotTargetDir.y = 0;
            }
        }
        else
            rotTargetDir = _Core.LocoModule.CurrentMoveDir;

        bool isJumping = _Core.LocoModule.IsJumping;
        bool isDashing = _Core.LocoModule.IsDahsing;

        _Core.LocoModule.Rotate(rotTargetDir, (isJumping || isDashing), isCombat);

        if (!_Core.LocoModule.IsGround)
        {
            _Core.StateMachine.ChangeLocoState(ELocomotionType.Fall);
        }
        else if (isJumpPress && _Core.LocoRule.CanJump(_Core.Context))
        {
            _Core.StateMachine.ChangeLocoState(ELocomotionType.Jump);
        }
        else if (isDashPress && _Core.LocoRule.CanDash(_Core.Context))
        {
            _Core.StateMachine.ChangeLocoState(ELocomotionType.Dash);
        }
    }

    public override void Exit()
    {
        _Core.Context.IsGroundMove = false;
    }
}
