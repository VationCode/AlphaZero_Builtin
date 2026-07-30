using Alpha.Player.Combat;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class GroundMoveState : StateBase
    {
        public GroundMoveState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow){}

        public override ELocoStateType Type => ELocoStateType.Move;

        protected override void Enter()
        {
            
        }
        protected override void Tick()
        {
            bool allowsManualMove = AllowsManualMovement();


            #region 상태 전환
            // 이동이 잠긴 공격 중에는 Dash와 Jump 입력도 차단한다.
            if (allowsManualMove && _Input.IsDash)
            {
                _StateFlow.ChangeState(ELocoStateType.Dash);
                return;
            }

            if (allowsManualMove && _Input.IsJump)
            {
                _StateFlow.ChangeState(ELocoStateType.Jump);
                return;
            }

            // 공격 중이라도 지면을 벗어나면 낙하 상태는 처리한다.
            if (!_Core.LocomotionModule.IsGrounded)
            {
                _Core.LocomotionModule.StartFall();
                _StateFlow.ChangeState(ELocoStateType.Fall);
                return;
            }
            #endregion

            Transform cameraTr = _Core.CameraCore?.RenderCameraTransform;

            bool isSprint = allowsManualMove && _Input.IsSprint;

            Vector2 moveInput = allowsManualMove? _Input.MoveInput : Vector2.zero;

            bool shouldFaceAim = !_Core.BlockCombat && _Core.CombatContext.IsCombatFacing;

            // 바라볼 방향 결정 (Input, Aim, Mouse)
            Vector3 facingDirection = Vector3.zero;

            if (shouldFaceAim)
            {
                facingDirection = ResolveAimDirection(cameraTr);

                _Core.CombatContext.SetAimDirection(facingDirection);
            }
            else
            {
                _Core.CombatContext.ClearAimDirection();
            }

            _Core.LocomotionModule.MoveGround(moveInput, cameraTr, isSprint, shouldFaceAim, facingDirection);

            Vector2 animationMoveInput = ResolveAnimationMoveInput(moveInput);

            _Core.AnimationView.PlayGroundLocomotion(animationMoveInput, isSprint, shouldFaceAim);
        }
        protected override void Exit()
        {
            
        }

        private Vector3 ResolveAimDirection(Transform p_cameraTransform)
        {
            bool usesQuarterAim = _Core.CameraCore.Context.BaseViewType == ECameraViewType.Quarter;

            if (usesQuarterAim && _Core.MouseSystem != null)
            {
                Vector3 mousePosition = _Input.MouseInputPos;

                if (_Core.MouseSystem.TryGetWorldDirection(new Vector2( mousePosition.x, mousePosition.y), _Core.PlayerTr.position, out Vector3 mouseDirection))
                {
                    return mouseDirection;
                }
            }

            // TPS에서는 카메라 정면을 사용한다.
            Vector3 cameraDirection = Vector3.ProjectOnPlane(p_cameraTransform.forward, Vector3.up);

            return cameraDirection.sqrMagnitude > 0.0001f? cameraDirection.normalized : Vector3.zero;
        }

        // 실제 월드 이동 방향을 Player 기준 전후좌우 값으로 변환한다.
        private Vector2 ResolveAnimationMoveInput(Vector2 p_rawMoveInput)
        {
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(
                _Core.LocomotionModule.Velocity,
                Vector3.up);

            if (horizontalVelocity.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            Vector3 localMoveDirection = _Core.PlayerTr.InverseTransformDirection(horizontalVelocity.normalized);

            float inputMagnitude = Mathf.Clamp01(p_rawMoveInput.magnitude);

            Vector2 localMoveInput = new(localMoveDirection.x, localMoveDirection.z);

            return Vector2.ClampMagnitude(localMoveInput * inputMagnitude, 1f);
        }

        // 현재 공격 정책에 따라 일반 이동 입력 허용 여부를 판단한다.
        private bool AllowsManualMovement()
        {
            AttackDefinition activeAttack =
                _Core.CombatContext.ActiveAttack;

            if (activeAttack == null)
                return true;

            return activeAttack.MovePolicy ==
                   EAttackMovePolicy.Free;
        }
    }
}
