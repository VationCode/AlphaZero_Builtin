using Alpha.Player.Combat;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // GroundMoveState 상태의 진입, 갱신, 종료 동작을 담당한다.
    public class GroundMoveState : StateBase
    {
        // 전달받은 값으로 초기 상태를 구성한다.
        public GroundMoveState(PlayerCore p_core, StateFlowBase p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Move;

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected override void Enter()
        {
        }

        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            // 대시와 점프 입력은 이동 계산보다 먼저 상태를 전환한다.
            if (_Input.IsDash)
            {
                _StateFlow.ChangeState(ELocoStateType.Dash);
                return;
            }

            if (_Input.IsJump)
            {
                _StateFlow.ChangeState(ELocoStateType.Jump);
                return;
            }

            // 지면을 벗어났다면 낙하 속도를 준비하고 Fall 상태로 이동한다.
            if (!_Core.LocomotionModule.IsGrounded)
            {
                _Core.LocomotionModule.StartFall();
                _StateFlow.ChangeState(ELocoStateType.Fall);
                return;
            }

            Transform cameraTransform = _Core.CameraCore?.RenderCamera.transform;
            bool isSprint = _Input.IsSprint;
            Vector2 moveInput = _Input.MoveInput;

            // 조준 중이면 이동 방향과 별도로 캐릭터가 바라볼 방향을 구한다.
            bool isAimFacing =
                TryResolveAimFacingDirection(cameraTransform, out Vector3 facingDirection);

            _Core.LocomotionModule.MoveGround(
                moveInput,
                cameraTransform,
                isSprint,
                isAimFacing,
                facingDirection);

            // 실제 이동 속도를 Player 로컬 축으로 변환해 애니메이션에 전달한다.
            Vector2 animationMoveInput = ResolveAnimationMoveInput(moveInput);
            _Core.AnimationView.PlayGroundLocomotion(
                animationMoveInput,
                isSprint,
                isAimFacing);
        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
        }

        // 카메라 모드에 따라 마우스 월드 방향 또는 카메라 정면을 조준 방향으로 사용한다.
        private Vector3 ResolveAimDirection(Transform p_cameraTransform)
        {
            bool usesQuarterAim = _Core.CameraCore.Context.CurrentViewType == ECameraViewType.Quarter;

            // Quarter 시점에서는 마우스 Ray와 Player 높이 평면의 교차 방향을 사용한다.
            if (usesQuarterAim && _Core.MouseSystem != null)
            {
                Vector3 mousePosition = _Input.MouseInputPos;

                if (_Core.MouseSystem.TryGetWorldDirection(
                    new Vector2(mousePosition.x, mousePosition.y),
                    _Core.PlayerTr.position,
                    out Vector3 mouseDirection))
                {
                    return mouseDirection;
                }
            }

            // TPS에서는 카메라 정면을 조준 방향으로 사용한다.
            Vector3 cameraDirection =
                Vector3.ProjectOnPlane(p_cameraTransform.forward, Vector3.up);

            return cameraDirection.sqrMagnitude > 0.0001f
                ? cameraDirection.normalized
                : Vector3.zero;
        }

        // 실제 월드 이동 방향을 Player 기준 전후좌우 값으로 변환한다.
        private Vector2 ResolveAnimationMoveInput(Vector2 p_rawMoveInput)
        {
            // 실제 수평 이동 속도를 기준으로 Player Local 방향을 계산한다.
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(
                _Core.LocomotionModule.Velocity,
                Vector3.up);

            if (horizontalVelocity.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            Vector3 localMoveDirection =
                _Core.PlayerTr.InverseTransformDirection(horizontalVelocity.normalized);

            // 원본 입력 세기는 유지하되 BlendTree 입력 범위는 1로 제한한다.
            float inputMagnitude = Mathf.Clamp01(p_rawMoveInput.magnitude);
            Vector2 localMoveInput =
                new(localMoveDirection.x, localMoveDirection.z);

            return Vector2.ClampMagnitude(localMoveInput * inputMagnitude, 1f);
        }

        // 전투 조준이 활성화됐을 때만 별도의 바라보기 방향을 제공한다.
        private bool TryResolveAimFacingDirection(
            Transform p_cameraTransform,
            out Vector3 p_facingDirection)
        {
            p_facingDirection = Vector3.zero;

            CombatContext context = _Core.CombatContext;

            // 전투 차단 또는 비조준 상태에서는 이전 조준 방향을 제거한다.
            if (_Core.BlockCombat || !context.IsAiming)
            {
                context.ClearAimDirection();
                return false;
            }

            Vector3 aimDirection = ResolveAimDirection(p_cameraTransform);
            context.SetAimDirection(aimDirection);

            if (!context.HasAimDirection)
                return false;

            p_facingDirection = context.AimDirection;
            return true;
        }
    }
}
