using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public enum ERootMotionMode
    {
        None,

        // 애니메이션 XZ 이동과 기존 중력을 함께 사용한다.
        Ground,

        // 애니메이션 XYZ 이동을 그대로 사용한다.
        Full
    }

    // Root Motion의 활성 상태와 이동 적용 방식을 관리한다.
    public sealed class RootMotionModule
    {
        private LocomotionMoveModule _moveModule;
        private object _owner;

        public ERootMotionMode CurrentMode { get; private set; } = ERootMotionMode.None;

        public bool IsActive => CurrentMode != ERootMotionMode.None;

        public bool Bind(LocomotionMoveModule p_moveModule)
        {
            if (p_moveModule == null)
                return false;

            ForceEnd();
            _moveModule = p_moveModule;
            return true;
        }

        // 활성 Root Motion을 시작한 소유자만 같은 세션을 종료할 수 있다.
        public bool Begin(object p_owner, ERootMotionMode p_mode)
        {
            if (p_owner == null ||
                _moveModule == null ||
                p_mode == ERootMotionMode.None)
            {
                return false;
            }

            if (IsActive)
            {
                return ReferenceEquals(_owner, p_owner) &&
                       CurrentMode == p_mode;
            }

            _owner = p_owner;
            CurrentMode = p_mode;
            return true;
        }

        public bool End(object p_owner)
        {
            if (!IsActive || !ReferenceEquals(_owner, p_owner))
                return false;

            ForceEnd();
            return true;
        }

        public void ForceEnd()
        {
            _owner = null;
            CurrentMode = ERootMotionMode.None;
        }

        // Animator가 계산한 프레임 이동량을 현재 Mode에 맞게 적용한다.
        public void Apply(Vector3 p_deltaPosition, float p_verticalVelocity)
        {
            if (!IsActive || _moveModule == null)
                return;

            Vector3 moveDelta = CurrentMode switch
            {
                ERootMotionMode.Ground => 
                new Vector3(p_deltaPosition.x, p_verticalVelocity * Time.deltaTime, p_deltaPosition.z),
                ERootMotionMode.Full => p_deltaPosition,
                _ => Vector3.zero
            };

            _moveModule.MoveDelta(moveDelta);
        }
    }
}
