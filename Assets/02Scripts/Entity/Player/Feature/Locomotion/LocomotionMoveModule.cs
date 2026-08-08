
/// <summary>
/// [ LocomotionMoveModule 책임 ]
/// 입력 기준 이동 방향 계산 
/// 이동 Mode별 속도 선택
/// 최종 이동 Velocity 계산
/// CharacterController.Move() 적용
/// 현재 Velocity 기록
/// </summary>

using System;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // MoveSpeedSet 처리에 함께 사용되는 값들을 묶는다.
    [Serializable]
    public struct MoveSpeedSet
    {
        [Min(0f)] public float WalkSpeed;
        [Min(0f)] public float SprintSpeed;
        [Min(0f)] public float CombatSpeed;

        // 전투 이동을 우선하고 그 외에는 달리기 여부로 속도를 선택한다.
        public float GetSpeed(bool p_isSprint, bool p_isCombat)
        {
            if (p_isCombat)
                return CombatSpeed;

            return p_isSprint? SprintSpeed : WalkSpeed;
        }
    }

    // 이동 계산과 CharacterController 이동을 담당한다.
    public class LocomotionMoveModule : MonoBehaviour
    {
        [Header("Move Speed")]
        [SerializeField] private MoveSpeedSet _groundMoveSpeed;

        [SerializeField] private MoveSpeedSet _flightMoveSpeed;

        private CharacterController _controller;
        private LocomotionContext _context;

        public Vector3 Velocity { get; private set; }

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController>();
        }

        // 이동 결과를 기록할 Context와 CharacterController를 연결한다.
        public void Bind(LocomotionContext p_context)
        {
            if (_controller == null || p_context == null)
            {
                Debug.LogError($"{nameof(LocomotionMoveModule)}의 의존성이 없습니다.", this);
                return;
            }

            _context = p_context;
        }

        // 카메라 축과 입력을 결합해 이동 모드에 맞는 정규화 방향을 계산한다.
        public Vector3 GetMoveDirection(Vector2 p_input, Transform p_cameraTransform, ELocomotionMode p_mode)
        {
            if (p_cameraTransform == null)
                return Vector3.zero;

            bool isSpatial = p_mode == ELocomotionMode.Flight;

            Vector3 forward = p_cameraTransform.forward;
            Vector3 right = p_cameraTransform.right;

            // 지상 이동은 카메라의 높이 기울기를 제거하고 XZ 평면만 사용한다.
            if (!isSpatial)
            {
                forward.y = 0f;
                right.y = 0f;
            }

            forward.Normalize();
            right.Normalize();

            // 현재의 기준 forward와 right에 입력값 처리
            Vector3 direction = (forward * p_input.y) + (right * p_input.x);

            return Vector3.ClampMagnitude(direction, 1f);
        }

        // 이동 모드별 속도 세트에서 전투·달리기 조건에 맞는 값을 선택한다.
        public float GetMoveSpeed(ELocomotionMode p_mode, bool p_isSprint, bool p_isCombat)
        {
            MoveSpeedSet speedSet = p_mode switch
            {
                ELocomotionMode.Ground => _groundMoveSpeed,
                ELocomotionMode.Flight => _flightMoveSpeed,

                _ => throw new NotSupportedException($"지원하지 않는 이동 Mode: {p_mode}")
            };

            return speedSet.GetSpeed(p_isSprint, p_isCombat);
        }

        // 이동 방향과 속력을 합치고 지상 모드에는 수직 속도도 반영한다.
        public Vector3 GetMoveVelocity(Vector3 p_direction, float p_moveSpeed, float p_verticalVelocity, ELocomotionMode p_mode)
        {
            Vector3 velocity = p_direction * p_moveSpeed;

            if (p_mode == ELocomotionMode.Ground)
                velocity.y = p_verticalVelocity;

            return velocity;
        }

        // 최종 속도를 Context에 기록하고 CharacterController에 적용한다.
        public void Move(Vector3 p_velocity)
        {
            if (_controller == null || _context == null)
                return;

            ApplyMovement(
                p_velocity,
                p_velocity * Time.deltaTime);
        }

        // Animator가 계산한 프레임 이동량을 CharacterController 충돌 경로로 적용한다.
        public void MoveDelta(Vector3 p_deltaPosition)
        {
            if (_controller == null || _context == null)
                return;

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 velocity = p_deltaPosition / deltaTime;

            ApplyMovement(velocity, p_deltaPosition);
        }

        private void ApplyMovement(
            Vector3 p_velocity,
            Vector3 p_deltaPosition)
        {
            Velocity = p_velocity;
            _context.Velocity = p_velocity;

            _controller.Move(p_deltaPosition);
        }
    }
}
