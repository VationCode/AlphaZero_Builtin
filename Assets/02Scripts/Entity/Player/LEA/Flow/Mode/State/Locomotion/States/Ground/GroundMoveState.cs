using UnityEngine;

namespace Alpha.Player
{
    public class GroundMoveState : GroundLocomotionState
    {
        public GroundMoveState(PlayerCore p_core, GroundLocomotionStateFlow p_flow) : base(p_core, p_flow){}

        public override void Tick()
        {
            Vector2 moveInput = _Core.Input.MoveInput;

            bool isSprint = _Core.Input.IsSprint;

            bool isCombat = _Core.Context.IsInCombat;

            // Ground 상태에 맞는 속도를 선택한다.
            float moveSpeed = _Ground.GetMoveSpeed(isSprint, isCombat);

            // 카메라 기준 XZ 이동을 계산한다.
            _Motor.SetMoveInput(moveInput, moveSpeed);

            // View와 전투 여부에 따른 방향으로 회전한다.
            Vector3 lookDirection = GetLookDirection();

            _Motor.Rotate(lookDirection, true);

            // 이동·중력·Impulse를 최종 적용한다.
            _Motor.ApplyMovement(Time.deltaTime);

        }

        public override void Exit()
        {
            _Motor.ClearMoveInput();
        }


    }
}