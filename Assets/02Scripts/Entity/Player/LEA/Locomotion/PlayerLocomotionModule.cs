using System;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // Locomotion 세부 기능을 하나의 실행 흐름으로 조합한다.
    [RequireComponent(typeof(LocomotionMoveModule), typeof(LocomotionRotationModule))]
    public class PlayerLocomotionModule : MonoBehaviour
    {
        private CharacterController _controller;

        // 실제 이동과 회전은 각각의 세부 Module이 담당한다.
        private LocomotionMoveModule _moveModule;
        private LocomotionRotationModule _rotationModule;
        private LocomotionContext _context;


        [Header("Jump")]
        [SerializeField] private float _jumpHeight = 2.5f;

        [Header("Land")]
        [SerializeField] private float _landDuration = 0.15f;
        

        [Header("DashUpdate")]
        [SerializeField] private float _dashDistance = 6f;
        [SerializeField] private float _dashDuration = 0.3f;


        [Header("Ground")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField, Min(0f)] private float _groundOffset = 0.07f;

        [Header("Gravity")]
        [SerializeField, Min(0f)] private float _gravity = 15f;
        [SerializeField, Min(0f)] private float _groundedForce = 2f;


        private float _airMoveSpeed;

        // Jump, Fall에 사용하는 수직 속도다.
        public float VerticalVelocity { get; private set; }
        // 실제 최종 이동 속도는 MoveModule이 보관한다.
        public Vector3 Velocity => _moveModule != null ? _moveModule.Velocity : Vector3.zero;

        public float LandDuration => _landDuration;
        public float DashDuration => _dashDuration;


        public bool IsGrounded { get; private set; }
        public bool IsGroundCollisionBelow { get; private set; }

        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController>();

            _moveModule = GetComponent<LocomotionMoveModule>();
            _rotationModule = GetComponent<LocomotionRotationModule>();
        }

        public void Bind(LocomotionContext p_context, Transform p_playerTransform)
        {
            if (p_context == null || p_playerTransform == null ||
        _controller == null || _moveModule == null || _rotationModule == null)
            {
                Debug.LogError($"{nameof(PlayerLocomotionModule)}의 의존성이 없습니다.", this);
                return;
            }

            _context = p_context;

            _moveModule.Bind(p_context);
            _rotationModule.Bind(p_playerTransform);
        }

        #region ======================================== Movement
        // Ground 이동의 계산, 회전, 실제 이동 순서를 조합한다.
        // p_facingDirection : 회전할 방향 결정(Input, Aim, Mouse 방향)
        public void MoveGround(Vector2 p_moveInput, Transform p_cameraTransform, 
                               bool p_isSprint, bool p_isCombat, 
                               Vector3 p_facingDirection)
        {
            // 이동 방향
            Vector3 moveDirection = _moveModule.GetMoveDirection(p_moveInput, p_cameraTransform, ELocomotionMode.Ground);

            // 속력
            float moveSpeed = _moveModule.GetMoveSpeed(ELocomotionMode.Ground, p_isSprint, p_isCombat);

            // 속도
            Vector3 moveVelocity = _moveModule.GetMoveVelocity(moveDirection, moveSpeed, VerticalVelocity, ELocomotionMode.Ground);

            // 별도의 바라볼 방향이 없으면 이동 방향을 사용한다.
            Vector3 rotationDirection = p_facingDirection.sqrMagnitude > 0.0001f? p_facingDirection : moveDirection;

            // 회전 적용
            _rotationModule.ApplyRotation(rotationDirection, p_cameraTransform, false, p_isCombat);

            // 실제 이동
            _moveModule.Move(moveVelocity);
        }

        #endregion ======================================== /Movement

        #region ======================================== Jump
        public void StartJump(Vector2 p_moveInput, Transform p_cameraTransform, bool p_isSprint = false, bool p_isCombat = false)
        {
            // 이동 방향
            Vector3 moveDirection =
                _moveModule.GetMoveDirection(p_moveInput, p_cameraTransform, ELocomotionMode.Ground);

            // 속력
            float moveSpeed =
                _moveModule.GetMoveSpeed(ELocomotionMode.Ground, p_isSprint, p_isCombat);

            // 공중에서 유지할 방향과 속도를 저장한다.(Update에서 호출)
            _context.LockedMoveDirection = moveDirection;
            _airMoveSpeed = moveSpeed;

            // 점프 시작 시 이동 방향으로 즉시 회전한다.
            _rotationModule.ApplyRotation(moveDirection, p_cameraTransform, false, false, true);

            VerticalVelocity = Mathf.Sqrt(2f * _gravity * _jumpHeight);
        }


        public void MoveAirborne(Vector3 p_direction)
        {
            Vector3 velocity = p_direction * _airMoveSpeed;

            velocity.y = VerticalVelocity;

            // 공중 이동도 최종적으로 MoveModule을 사용한다.
            _moveModule.Move(velocity);
        }
        #endregion ======================================== /Jump

        #region ======================================== Fall
        public void StartFall()
        {
            // 마지막 지상 이동 속도를 기준으로 공중 이동을 유지한다.
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_moveModule.Velocity, Vector3.up);

            _airMoveSpeed = horizontalVelocity.magnitude;

            _context.LockedMoveDirection = _airMoveSpeed > 0.001f ? horizontalVelocity.normalized : Vector3.zero;
        }
        #endregion ======================================== /Fall

        public void StartDash(Vector2 p_moveInput, Transform p_cameraTransform)
        {
            Vector3 dashDirection = _moveModule.GetMoveDirection(p_moveInput, p_cameraTransform, ELocomotionMode.Ground);

            // 이동 입력이 없으면 현재 정면으로 Dash한다.
            if (dashDirection.sqrMagnitude < 0.0001f)
            {
                dashDirection = Vector3.ProjectOnPlane(_controller.transform.forward, Vector3.up).normalized;
            }

            _context.LockedMoveDirection = dashDirection;

            // Dash 방향은 시작할 때 즉시 고정한다.
            _rotationModule.ApplyRotation(dashDirection, p_cameraTransform, false, false, true);
        }

        public void DashUpdate(Vector3 p_direction)
        {
            float duration = Mathf.Max(_dashDuration, 0.01f);

            float dashSpeed = _dashDistance / duration;

            Vector3 velocity = p_direction.normalized * dashSpeed;

            velocity.y = VerticalVelocity;

            _moveModule.Move(velocity);
        }

        #region Ground & Gravity
        public void UpdateEnvironment(float p_gravityScale)
        {
            UpdateGroundCheck();
            UpdateGravity(p_gravityScale);
        }

        private Vector3 CalculateGroundCheckPoint(CharacterController p_controller)
        {
            Vector3 center = p_controller.transform.TransformPoint(p_controller.center);

            float bottomOffset = (p_controller.height * 0.5f) - p_controller.radius + _groundOffset;

            return center + (Vector3.down * bottomOffset);
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
                VerticalVelocity = -_groundedForce;
                return;
            }

            VerticalVelocity -= (_gravity * p_gravityScale * Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            CharacterController controller = _controller;

            if (controller == null)
                controller = GetComponent<CharacterController>();

            if (controller == null)
                return;

            Vector3 groundPoint = CalculateGroundCheckPoint(controller);

            Gizmos.color = IsGrounded ? Color.green : Color.red;

            Gizmos.DrawWireSphere(groundPoint, controller.radius);
        }
        #endregion
    }
}
