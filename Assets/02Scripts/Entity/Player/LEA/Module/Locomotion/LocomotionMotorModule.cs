
using System;
using UnityEngine;

namespace Alpha.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class LocomotionMotorModule : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField, Min(0f)] private float _rotationSpeed = 720f;

        [Header("Impulse")]
        [SerializeField] private LocomotionImpulseFeature _impulse = new LocomotionImpulseFeature();

        [Header("Gravity")]
        [SerializeField] private LocomotionGravityFeature _gravity = new LocomotionGravityFeature();

        [Header("Ground")]
        [SerializeField] private LocomotionGroundFeature _ground = new LocomotionGroundFeature();

        private CharacterController _controller;
        private Transform _cameraTransform;

        private Vector3 _moveVelocity;

        public LocomotionImpulseFeature Impulse => _impulse;

        public LocomotionGravityFeature Gravity => _gravity;

        public LocomotionGroundFeature Ground => _ground;

        // 카메라 기준 XZ 이동 방향
        public Vector3 MoveDirection { get; private set; }

        // 이번 프레임의 최종 요청 속도
        public Vector3 RequestedVelocity { get; private set; }

        public Vector3 CurrentVelocity => _controller != null? _controller.velocity : Vector3.zero;

        public bool IsRising => !_ground.IsGrounded && _gravity.VerticalVelocity > 0f;

        public bool IsFalling => !_ground.IsGrounded && _gravity.VerticalVelocity <= 0f;
        public CollisionFlags LastCollisionFlags {get; private set;}


        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            _ground.Bind(transform, _controller);
        }

        public void Bind(Transform p_cameraTransform)
        {
            _cameraTransform = p_cameraTransform;
        }

        // 입력으로 기본 XZ 이동 속도를 계산한다.
        public void SetMoveInput(Vector2 p_input, float p_moveSpeed)
        {
            MoveDirection = GetMoveDirection(p_input);

            _moveVelocity = MoveDirection * p_moveSpeed;
        }

        // 모드별 Global Y 속도를 합성하고 최종 이동을 적용한다.
        public void ApplyMovement(float p_deltaTime)
        {
            if (p_deltaTime <= 0f)
                return;

            Vector3 motionVelocity = _impulse.IsActive? _impulse.UpdateImpulse(p_deltaTime) : _moveVelocity;

            RequestedVelocity = motionVelocity + (Vector3.up * _gravity.VerticalVelocity);

            LastCollisionFlags = _controller.Move(RequestedVelocity * p_deltaTime);
        }

        public void ClearMoveInput()
        {
            MoveDirection = Vector3.zero;
            _moveVelocity = Vector3.zero;
        }

        // 카메라 기준 XZ 이동 방향을 계산한다.
        private Vector3 GetMoveDirection(Vector2 p_input)
        {
            p_input = Vector2.ClampMagnitude(p_input, 1f);

            Transform referenceTransform = _cameraTransform != null? _cameraTransform : transform;

            Vector3 forward = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up);

            if (forward.sqrMagnitude < 0.001f)
                return Vector3.zero;

            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 direction = (right * p_input.x) + (forward * p_input.y);

            return Vector3.ClampMagnitude(direction, 1f);
        }

        // Flow가 결정한 방향으로 캐릭터를 회전한다.
        public void Rotate(Vector3 p_direction, bool p_isPlanar, bool p_instant = false)
        {
            if (p_isPlanar)
            {
                p_direction = Vector3.ProjectOnPlane(p_direction, Vector3.up);
            }

            if (p_direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation( p_direction.normalized, Vector3.up);

            if (p_instant)
            {
                transform.rotation = targetRotation;
                return;
            }

            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }
}



/* 
// 접지 갱신
_motor.Ground.Update(_motor.Gravity.VerticalVelocity);

// 중력 갱신
_motor.Gravity.Update(_motor.Ground.IsGrounded, gravityScale, Time.deltaTime);

// 현재 모드의 이동 속도 전달
_motor.SetMoveInput(moveInput, moveSpeed);

// 회전
_motor.Rotate(lookDirection, isPlanar);

// 최종 이동
_motor.ApplyMovement(Time.deltaTime);
*/