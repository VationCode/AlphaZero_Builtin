
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
    [Serializable]
    public struct MoveSpeedSet
    {
        [Min(0f)] public float WalkSpeed;
        [Min(0f)] public float SprintSpeed;
        [Min(0f)] public float CombatSpeed;

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

        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController>();
        }

        public void Bind(LocomotionContext p_context)
        {
            if (_controller == null || p_context == null)
            {
                Debug.LogError($"{nameof(LocomotionMoveModule)}의 의존성이 없습니다.", this);
                return;
            }

            _context = p_context;
        }

        // 입력 기준 이동 방향 계산 
        public Vector3 GetMoveDirection(Vector2 p_input, Transform p_cameraTransform, ELocomotionMode p_mode)
        {
            if (p_cameraTransform == null)
                return Vector3.zero;

            bool isSpatial = p_mode == ELocomotionMode.Flight;

            Vector3 forward = p_cameraTransform.forward;
            Vector3 right = p_cameraTransform.right;

            // 3차원 공간이 아닐경우 XZ값만 처리
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

        public Vector3 GetMoveVelocity(Vector3 p_direction, float p_moveSpeed, float p_verticalVelocity, ELocomotionMode p_mode)
        {
            Vector3 velocity = p_direction * p_moveSpeed;

            if (p_mode == ELocomotionMode.Ground)
                velocity.y = p_verticalVelocity;

            return velocity;
        }

        // 실제 이동 처리
        public void Move(Vector3 p_velocity)
        {
            if (_controller == null || _context == null)
                return;

            Velocity = p_velocity;
            _context.Velocity = p_velocity;

            _controller.Move(p_velocity * Time.deltaTime);
        }
    }
}
