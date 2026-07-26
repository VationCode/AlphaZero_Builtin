using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Windows;
public enum EMoveSpace
{
    Planar,  // 수평면
    Spatial // 3차원 공간
}

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

        if (p_isSprint)
            return SprintSpeed;

        return WalkSpeed;
    }
}

namespace Alpha.Player.Locomotion
{
    public class PlayerLocomotionModule : MonoBehaviour
    {
        private CharacterController _controller;

        [Header("Move Speed")]
        [SerializeField] private MoveSpeedSet _groundMoveSpeed;
        [SerializeField] private MoveSpeedSet _flightMoveSpeed;

        [Header("Rotation")]
        private float _rotationSmoothTime = 0.1f;
        private float _spatialRotationSmoothness = 10f;

        [Header("Jump")]
        [SerializeField] private float _jumpHeight = 2.5f;

        [Header("Land")]
        [SerializeField] private float _landDuration = 0.15f;
        public float LandDuration => _landDuration;

        [Header("Dash")]
        [SerializeField] private float _dashDistance = 6f;
        [SerializeField] private float _dashDuration = 0.3f;
        public float DashDuration => _dashDuration;
        [Header("Ground")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField, Min(0f)] private float _groundOffset = 0.07f;

        [Header("Gravity")]
        [SerializeField, Min(0f)] private float _gravity = 15f;
        [SerializeField, Min(0f)] private float _groundedForce = 2;

        private LocomotionContext _context;

        public bool IsGrounded { get; private set; }
        public float VerticalVelocity { get; private set; }
        public Vector3 Velocity { get; private set; }
        private float _rotationVelocity;


        private float _airMoveSpeed;

        public bool IsGroundCollisionBelow { get; private set; }
        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController>();
        }
        public void Bind(LocomotionContext p_context)
        {
            _context = p_context;
        }

        #region ======================================== Move & Rotation
        public void Move(Vector3 p_velocity)
        {
            Velocity = p_velocity;
            _context.Velocity = p_velocity;

            _controller.Move(p_velocity * Time.deltaTime);
        }

        public void Movement(Vector2 p_inputDir, Transform p_cameraTransform, 
                             bool p_isSprint, bool p_isCombat, ELocomotionMode p_mode)
        {
            bool isSpatial = p_mode == ELocomotionMode.Flight;

            // 방향
            Vector3 direction = CalculateMoveDirection(p_inputDir, p_cameraTransform, isSpatial); // Ground는 수평 이동

            // 속력
            float moveSpeed = GetMoveSpeed(p_isSprint, p_isCombat, p_mode);

            // 속도
            Vector3 velocity = direction * moveSpeed;

            // Ground에서는 중력 속도도 한 번에 적용
            if (!isSpatial)
                velocity.y = VerticalVelocity;

            Transform playerTransform = _controller.transform;

            // 계산된 회전을 실제 Player에 적용
            ApplyMoveRotation(direction, p_cameraTransform, isSpatial, p_isCombat);

            Move(velocity);
        }

        // 이동 방향
        private Vector3 CalculateMoveDirection(Vector2 p_inputDir, Transform p_cameraTransform, bool p_isSpatial = false)
        {
            Vector3 forward = p_cameraTransform.forward;
            Vector3 right = p_cameraTransform.right;

            // 높이 성분을 제거하여 수평 방향만 사용
            if(!p_isSpatial)
            {
                forward.y = 0f;
                right.y = 0f;
            }

            forward.Normalize();
            right.Normalize();

            Vector3 direction = forward * p_inputDir.y + right * p_inputDir.x;

            // 대각선 이동 속도 증가 방지 및 아날로그 입력 세기 유지
            return Vector3.ClampMagnitude(direction, 1f);
        }

        public float GetMoveSpeed(bool p_isSprint, bool p_isCombat, ELocomotionMode p_mode)
        {
            MoveSpeedSet speedSet;

            switch (p_mode)
            {
                case ELocomotionMode.Ground:
                    speedSet = _groundMoveSpeed;
                    break;

                case ELocomotionMode.Flight:
                    speedSet = _flightMoveSpeed;
                    break;

                default:
                    throw new NotSupportedException($"지원하지 않는 이동 Mode: {p_mode}");
            }

            return speedSet.GetSpeed(p_isSprint, p_isCombat);
        }

        public void ApplyMoveRotation(Vector3 p_direction, Transform p_cameraTransform,
                                      bool p_isSpatial = false, bool p_isCombat = false, bool p_isInstant = false)
        {
            _controller.transform.rotation = 
                CalculateMoveRotation(p_direction, p_cameraTransform, p_isSpatial, p_isCombat, p_isInstant);
        }

        // 회전
        private Quaternion CalculateMoveRotation(Vector3 p_inputDir, Transform p_cameraTransform, 
                                                bool p_isSpatial = false, bool p_isCombat = false, bool p_isInstant = false)
        {
            // Combat/Aim은 이동 입력과 무관하게 카메라 정면을 바라봄
            Vector3 forward = p_isCombat ? p_cameraTransform.forward : p_inputDir;

            if (!p_isSpatial)
            {
                // Ground에서는 캐릭터가 기울어지지 않도록 처리
                forward.y = 0f;
            }

            // 회전할 방향이 없다면 현재 회전 유지
            if (forward.sqrMagnitude < 0.0001f)
                return transform.rotation;

            Vector3 up = p_isSpatial? p_cameraTransform.up : Vector3.up;

            Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, up);

            // Combat은 즉시 회전
            if (p_isCombat || p_isInstant)
            {
                _rotationVelocity = 0f;
                return targetRotation;
            }

            // 공간 회전에는 SmoothDampAngle이 적합하지 않으며 짐벌락 발생할 수도 있다.
            if (p_isSpatial)
            {
                // 프레임률에 독립적인 보간 비율
                float lerpRatio = 1f - Mathf.Exp(-_spatialRotationSmoothness * Time.deltaTime);

                return Quaternion.Slerp(transform.rotation, targetRotation, lerpRatio);
            }

            // Ground는 Y축을 부드럽게 회전
            float smoothYaw = 
                Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation.eulerAngles.y, ref _rotationVelocity, _rotationSmoothTime);

            return Quaternion.Euler(0f, smoothYaw, 0f);
        }

        #endregion ======================================== /Move & Rotation


        #region ======================================== Jump & Dash
        public void StartJump(Vector2 p_moveInput, Transform p_cameraTransform, bool p_isSprint = false, bool p_isCombat = false)
        {
            Vector3 direction = LockMoveDirection(p_moveInput, p_cameraTransform, false); // 입력이 없으면 제자리 점프

            _airMoveSpeed = GetMoveSpeed(p_isSprint, p_isCombat, ELocomotionMode.Ground);

            ApplyMoveRotation(direction, p_cameraTransform, false, false, true);

            VerticalVelocity = Mathf.Sqrt(2f * _gravity * _jumpHeight);
        }

        private Vector3 LockMoveDirection(Vector2 p_input, Transform p_cameraTransform, bool p_useForwardFallback)
        {
            Vector3 direction = CalculateMoveDirection(p_input, p_cameraTransform);

            if (p_useForwardFallback && direction.sqrMagnitude < 0.0001f)
            {
                direction = _controller.transform.forward;
                direction.y = 0f;
            }

            _context.LockedMoveDirection = direction;

            return direction;
        }
        public Vector3 GetMoveDirection(Vector2 p_inputDir, Transform p_cameraTransform, bool p_isSpatial = false)
        {
            return CalculateMoveDirection(p_inputDir, p_cameraTransform, p_isSpatial);
        }

        public void MoveAirborne(Vector3 p_dir)
        {
            // 점프중 공중 이중
            //Vector3 velocity = Vector3.ProjectOnPlane(p_horizontalVelocity, Vector3.up);

            Vector3 velocity = p_dir * _airMoveSpeed;

            velocity.y = VerticalVelocity;

            Move(velocity);
        }

        public void StartFall()
        {
            // 직전 지상 이동의 수평 속도 보존
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(Velocity, Vector3.up);

            _airMoveSpeed = horizontalVelocity.magnitude;

            _context.LockedMoveDirection = _airMoveSpeed > 0.001f ? horizontalVelocity.normalized : Vector3.zero;
        }


        public void StartDash(Vector2 p_input, Transform p_cameraTransform)
        {
            Vector3 direction = LockMoveDirection(p_input, p_cameraTransform, true); // 입력이 없으면 현재 정면

            ApplyMoveRotation(direction, p_cameraTransform, false, false ,true);
        }
        public void Dash(Vector3 p_direction)
        {
            float duration = Mathf.Max(_dashDuration, 0.01f);
            float dashSpeed = _dashDistance / duration;

            Vector3 velocity =
                p_direction.normalized * dashSpeed;

            // 지면 접촉 유지
            velocity.y = VerticalVelocity;

            Move(velocity);
        }

        #endregion ======================================== /Jump & Dash
        #region ======================================== Ground & Gravity
        public void UpdateEnvironment(float p_gravityScale)
        {
            UpdateGroundCheck();
            UpdateGravity(p_gravityScale);
        }

        private Vector3 CalculateGroundCheckPoint(CharacterController p_controller)
        {
            Vector3 center =
                p_controller.transform.TransformPoint(p_controller.center);

            float bottomOffset = (p_controller.height * 0.5f) - p_controller.radius + _groundOffset;

            return center + Vector3.down * bottomOffset;
        }

        private void UpdateGroundCheck()
        {
            Vector3 groundPoint = CalculateGroundCheckPoint(_controller);

            IsGrounded = 
                Physics.CheckSphere(groundPoint, _controller.radius, _groundLayer, QueryTriggerInteraction.Ignore);

            _context.IsGrounded = IsGrounded;
        }

        private void UpdateGravity(float p_gravityScale)
        {
            if (IsGrounded && VerticalVelocity <= 0f)
            {
                // 지면에 붙어 있도록 작은 하강 속도 유지
                VerticalVelocity = -_groundedForce;
                return;
            }

            // p_gravityScale의 경우 무중력을 위해서 1 : 0
            VerticalVelocity -= _gravity * p_gravityScale * Time.deltaTime;
        }

        private void OnDrawGizmos()
        {
            CharacterController controller = _controller;

            if (controller == null)
                controller = GetComponent<CharacterController>();

            if (controller == null)
                return;

            Vector3 groundPoint =
                CalculateGroundCheckPoint(controller);

            // 접지 상태에 따라 색상 변경
            Gizmos.color = IsGrounded? Color.green : Color.red;

            Gizmos.DrawWireSphere(groundPoint, controller.radius);
        }
        #endregion ======================================== /Ground & Gravity
    }
}