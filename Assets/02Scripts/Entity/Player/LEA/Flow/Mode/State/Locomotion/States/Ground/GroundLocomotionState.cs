using UnityEngine;

namespace Alpha.Player
{
    // Ground State들이 공통으로 사용할 기반 클래스
    public abstract class GroundLocomotionState : ILocomotionState
    {
        protected PlayerCore _Core { get; }
        protected GroundLocomotionStateFlow _Flow { get; }

        protected LocomotionModeFlow _ModeFlow => _Flow.ModeFlow;

        protected LocomotionMotorModule _Motor => _Core.LocomotionMotor;

        protected GroundLocomotionModule _Ground => _Core.GroundLocomotion;

        protected GroundLocomotionState(PlayerCore p_core, GroundLocomotionStateFlow p_flow)
        {
            _Core = p_core;
            _Flow = p_flow;
        }

        public virtual void Enter(){}

        public abstract void Tick();

        public virtual void Exit(){}

        // View와 전투 여부에 따라 회전 방향을 결정한다.
        protected Vector3 GetLookDirection()
        {
            // 비전투는 입력 이동 방향을 바라본다.
            if (!_Core.Context.IsInCombat)
                return _Motor.MoveDirection;

            // 전투 QuarterView는 마우스 방향을 바라본다.
            if (_Core.CameraCore.CurrentViewType == ECameraViewType.Quarter)
            {
                bool hasDirection =
                    _Core.MouseSystem.TryGetWorldDirection(_Core.Input.MouseInputPos, _Core.transform.position, out Vector3 mouseDirection);

                if (hasDirection) return mouseDirection;

                // Raycast 실패 시 현재 방향을 유지한다.
                return _Core.transform.forward;
            }

            // 전투 TPS/Aim은 카메라 정면을 바라본다.
            return _Core.CameraCore.RenderCamera.transform.forward;
        }
    }
}