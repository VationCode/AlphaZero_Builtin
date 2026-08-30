using Alpha.Player.Combat;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // GroundMoveState 상태의 진입, 갱신, 종료 동작을 담당한다.
    public class GroundMoveState : StateBase
    {
        private const float FallGraceDuration = 0.03f;
        private float _ungroundedElapsed;

        // 전달받은 값으로 초기 상태를 구성한다.
        public GroundMoveState(PlayerCore p_core, StateFlowBase p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Move;

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected override void Enter()
        {
            _ungroundedElapsed = 0f;
        }

        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            if (_Core.LocomotionModule.BlocksInput)
            {
                // 행동 중에는 입력 이동을 멈추고 Base Layer가 Idle을 준비하게 한다.
                _Core.AnimationView.PlayGroundLocomotion(
                    Vector2.zero,
                    false,
                    _Core.CombatContext.IsCombatStanceActive);
                return;
            }

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

            // 내리막의 순간적인 접지 손실은 유예하고, 지속될 때만 Fall로 이동한다.
            if (!_Core.LocomotionModule.IsGrounded)
            {
                _ungroundedElapsed += Time.deltaTime;

                if (_ungroundedElapsed >= FallGraceDuration)
                {
                    _Core.LocomotionModule.StartFall();
                    _StateFlow.ChangeState(ELocoStateType.Fall);
                    return;
                }
            }
            else
            {
                _ungroundedElapsed = 0f;
            }

            Transform cameraTransform = _Core.CameraCore?.RenderCamera.transform;
            bool isSprint = _Input.IsSprint;
            Vector2 moveInput = _Input.MoveInput;

            bool isCombatStance =
                _Core.CombatContext.IsCombatStanceActive;

            // Range 조준 또는 공격 중이면 이동과 별도의 바라볼 방향을 구한다.
            bool isAimFacing =
                TryResolveAimFacingDirection(out Vector3 facingDirection);

            float rangeMoveSpeedMultiplier = isAimFacing
                ? _Core.CombatModule.CurrentRangeWeapon?
                    .FireResponseSettings?.MoveSpeedMultiplier ?? 1f
                : 1f;

            _Core.LocomotionModule.MoveGround(
                moveInput,
                cameraTransform,
                isSprint,
                isCombatStance,
                facingDirection,
                rangeMoveSpeedMultiplier);

            // 실제 이동 속도를 Player 로컬 축으로 변환해 애니메이션에 전달한다.
            Vector2 animationMoveInput = ResolveAnimationMoveInput(moveInput);
            _Core.AnimationView.PlayGroundLocomotion(
                animationMoveInput,
                isSprint,
                isCombatStance);
        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
            _ungroundedElapsed = 0f;
            ClearRangeAimPresentation();
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

        // 조준 또는 Range 공격 중에는 실제 공격 조준점을 바라보게 한다.
        private bool TryResolveAimFacingDirection(
            out Vector3 p_facingDirection)
        {
            p_facingDirection = Vector3.zero;

            CombatContext context = _Core.CombatContext;

            // 전투 차단 또는 Aim Facing이 필요하지 않으면 이전 방향을 제거한다.
            if (!_Core.CanUseCombat ||
                !context.UsesAimFacing)
            {
                ClearRangeAimPresentation();
                return false;
            }

            // 전투 태세가 유지되는 동안에도 현재 Camera 중앙 에임을 매 프레임 추적한다.
            if (!_Core.CombatModule.TryGetRangeAimDirection(
                    out Vector3 aimDirection))
            {
                ClearRangeAimPresentation();
                return false;
            }

            context.SetAimDirection(aimDirection);
            _Core.RigView?.SetAimDirection(aimDirection);

            if (!context.HasAimDirection)
                return false;

            p_facingDirection = Vector3.ProjectOnPlane(
                context.AimDirection,
                Vector3.up);

            if (p_facingDirection.sqrMagnitude <= 0.0001f)
                return false;

            p_facingDirection.Normalize();
            return true;
        }

        // 조준 조건이 끝나면 Domain과 View의 임시 방향을 함께 초기화한다.
        private void ClearRangeAimPresentation()
        {
            _Core.CombatContext.ClearAimDirection();
            _Core.RigView?.ClearAimDirection();
        }
    }
}
