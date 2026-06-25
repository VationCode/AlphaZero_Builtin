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
        // ==================== Input
        bool isSprintPress = _Core.InputBoundary.IsSprint;
        bool isJumpPress = _Core.InputBoundary.IsJump;
        bool isDashPress = _Core.InputBoundary.IsDash;
        Vector2 moveInput = _Core.InputBoundary.MoveInput;
        EViewType viewType = _Core.CameraCore.State.CurrentViewType;
        // ==================== Move
        _Core.Context.IsGroundMove = moveInput.magnitude > 0;

        if(isSprintPress && _Core.LocoRule.CanSprint(_Core.Context))
            _Core.Context.IsSprint = true;
        else 
            _Core.Context.IsSprint = false;

        bool isCombat = _Core.Context.IsInCombat;
        bool isSprint = _Core.Context.IsSprint;

        _Core.LocoModule.GroundMovement(moveInput, isSprint, isCombat);

        // ==================== Rot
        Vector2 mouseInput = _Core.InputBoundary.MouseInputPos;
        Vector3 currentMoveDir = _Core.LocoModule.CurrentMoveDir;
        Vector3 lookDir = CalculateRot(isCombat, _Core.LocoModule.CurrentMoveDir, mouseInput);

        // ==================== Anim
        _Core.LocoModule.AnimationInput(viewType, moveInput, currentMoveDir, isSprint, isCombat);

        // ==================== Switch State 
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

    private Vector3 CalculateRot(bool p_isCombat, Vector3 p_currentPointDir, Vector2 p_mouseInput)
    {
        EViewType viewType = _Core.CameraCore.State.CurrentViewType;

        if (p_isCombat)
        {
            if (_Core.CameraCore.State.CurrentViewType == EViewType.Quarter)
            {
                p_currentPointDir = _Core.CameraCore.MouseUtility.GetTargetMouseDirection(p_mouseInput, _Core.transform.position);
            }
            else
            {
                p_currentPointDir = Camera.main.transform.forward;
            }
        }

        p_currentPointDir.Normalize();

        _Core.LocoModule.Rotate(p_currentPointDir, false);

        return p_currentPointDir;
    }
}
