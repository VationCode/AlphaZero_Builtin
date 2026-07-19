using UnityEngine;
namespace Alpha.Player
{
    public class GroundLocomotionModule : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private LocomotionSpeedSettings _speed = new LocomotionSpeedSettings();

        [Header("Gravity")]
        [SerializeField, Min(0f)] private float _gravityScale = 1f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float _jumpHeight = 2f;

        private LocomotionMotorModule _motor;

        public bool IsGrounded => _motor.Ground.IsGrounded;

        public bool IsRising => !IsGrounded && _motor.Gravity.VerticalVelocity > 0f;

        public bool IsFalling => !IsGrounded && _motor.Gravity.VerticalVelocity <= 0f;

        public float GravityScale => _gravityScale;

        public void Bind(LocomotionMotorModule p_motor)
        {
            _motor = p_motor;
        }

        // Ground 접지와 중력을 순서대로 갱신한다.
        public void UpdateEnvironment(float p_deltaTime)
        {
            _motor.Ground.UpdateGroundChecked(_motor.Gravity.VerticalVelocity);

            _motor.Gravity.UpdateGravity(_motor.Ground.IsGrounded, _gravityScale, p_deltaTime);
        }

        // 현재 Ground 상태에 맞는 속도를 반환한다.
        public float GetMoveSpeed(bool p_isSprint, bool p_isCombat)
        {
            return _speed.GetSpeed(p_isSprint, p_isCombat);
        }

        // 점프 높이를 Y 초기 속도로 변환한다.
        public void Jump()
        {
            float effectiveGravity = _motor.Gravity.Gravity * _gravityScale;

            float jumpVelocity = Mathf.Sqrt(2f *effectiveGravity * _jumpHeight);

            _motor.Gravity.SetVelocity(jumpVelocity);

            _motor.Ground.DisableUntilFalling();
        }
    }
}

/*
1. 접지와 중력 갱신
_groundModule.UpdateEnvironment(
    Time.deltaTime);

2. Ground 모드 속도 선택
float moveSpeed =
    _groundModule.GetMoveSpeed(
        isSprint,
        isCombat);

3. Motor에 입력과 속도 전달
_motor.SetMoveInput(
    moveInput,
    moveSpeed);

4. View에 따른 방향으로 회전
_motor.Rotate(
    lookDirection,
    true);

5. 이동·중력·Impulse 최종 적용
_motor.ApplyMovement(
    Time.deltaTime);
 */