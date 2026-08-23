using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // Locomotion 세부 기능을 하나의 실행 흐름으로 조합한다.
    [RequireComponent(typeof(LocomotionMoveModule), typeof(LocomotionRotationModule))]
    public class LocomotionModule : MonoBehaviour, IKnockbackable
    {
        private const float DirectionEpsilon = 0.0001f;

        private CharacterController _controller;

        // 실제 이동과 회전은 각각의 세부 Module이 담당한다.
        private LocomotionMoveModule _moveModule;
        private LocomotionRotationModule _rotationModule;
        private LocomotionContext _context;
        private readonly RootMotionModule _rootMotionModule = new();
        private int _inputLockCount;
        private bool _isKnockbackActive;
        private Vector3 _knockbackVelocity;
        private float _knockbackRemainingTime;


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

        // Jump, Fall에 사용하는 실제 중력 수직 속도다.
        public float VerticalVelocity { get; private set; }
        // 접지 중인 지상 이동에만 별도의 하향 밀착력을 적용한다.
        private float GroundVerticalVelocity =>
            IsGrounded ? -_groundedForce : VerticalVelocity;

        // 실제 최종 이동 속도는 MoveModule이 보관한다.
        public Vector3 Velocity => _moveModule != null ? _moveModule.Velocity : Vector3.zero;

        public float LandDuration => _landDuration;
        public float DashDuration => _dashDuration;
        public bool UsesRootMotion => _rootMotionModule.IsActive;
        public bool BlocksInput =>
            UsesRootMotion || _inputLockCount > 0;
        public bool IsKnockbackActive => _isKnockbackActive;
        public bool CanReceiveKnockback =>
            isActiveAndEnabled &&
            _controller != null &&
            _controller.enabled;


        public bool IsGrounded { get; private set; }
        public bool IsGroundCollisionBelow { get; private set; }

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController>();

            _moveModule = GetComponent<LocomotionMoveModule>();
            _rotationModule = GetComponent<LocomotionRotationModule>();
        }

        private void OnDisable()
        {
            CancelKnockback();
        }

        // 이동 Context와 세부 이동·회전 Module을 Player Transform에 연결한다.
        public void Bind(LocomotionContext p_context, Transform p_playerTransform)
        {
            if (p_context == null || p_playerTransform == null ||
                _controller == null || _moveModule == null || _rotationModule == null)
            {
                Debug.LogError($"{nameof(LocomotionModule)}의 의존성이 없습니다.", this);
                return;
            }

            _context = p_context;
            _inputLockCount = 0;
            CancelKnockback();

            _moveModule.Bind(p_context);
            _rotationModule.Bind(p_playerTransform);

            if (!_rootMotionModule.Bind(_moveModule))
                Debug.LogError($"{nameof(RootMotionModule)}을 연결하지 못했습니다.", this);
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
            Vector3 moveVelocity = _moveModule.GetMoveVelocity(
                moveDirection,
                moveSpeed,
                GroundVerticalVelocity,
                ELocomotionMode.Ground);

            // 별도의 바라볼 방향이 없으면 이동 방향을 사용한다.
            Vector3 rotationDirection = p_facingDirection.sqrMagnitude > 0.0001f? p_facingDirection : moveDirection;

            // 회전 적용
            _rotationModule.ApplyRotation(rotationDirection, p_cameraTransform, false, p_isCombat);

            // 실제 이동
            _moveModule.Move(moveVelocity);
        }

        // 전투 행동 시작 시 Player를 지정된 지상 방향으로 회전시킨다.
        public void FaceGroundDirection(
            Vector3 p_direction,
            Transform p_cameraTransform,
            bool p_isInstant)
        {
            _rotationModule.ApplyRotation(
                p_direction,
                p_cameraTransform,
                false,
                true,
                p_isInstant);
        }

        // 이동 입력을 카메라 기준의 지상 월드 방향으로 변환한다.
        public bool TryGetGroundInputDirection(
            Vector2 p_moveInput,
            Transform p_cameraTransform,
            out Vector3 p_direction)
        {
            p_direction = _moveModule.GetMoveDirection(
                p_moveInput,
                p_cameraTransform,
                ELocomotionMode.Ground);

            return p_direction.sqrMagnitude >= 0.0001f;
        }

        // 행동이 사용할 Root Motion 적용 방식을 시작한다.
        public bool BeginRootMotion(ERootMotionMode p_mode)
        {
            return _rootMotionModule.Begin(p_mode);
        }

        // 현재 Root Motion 행동을 종료한다.
        public void EndRootMotion()
        {
            _rootMotionModule.End();
        }

        // Animator 이동량을 활성화된 Root Motion Module에 전달한다.
        public void ApplyRootMotion(Vector3 p_deltaPosition)
        {
            _rootMotionModule.Apply(
                p_deltaPosition,
                GroundVerticalVelocity);
        }

        // Root Motion을 사용하지 않는 행동이 이동 입력을 잠근다.
        public void BeginInputLock()
        {
            _inputLockCount++;
        }

        // 현재 행동이 소유한 이동 입력 잠금을 해제한다.
        public void EndInputLock()
        {
            _inputLockCount = Mathf.Max(0, _inputLockCount - 1);
        }

        // 공격 방향과 거리/시간을 CharacterController의 수평 속도로 변환한다.
        public bool TryApplyKnockback(
            in KnockbackInfo p_knockbackInfo)
        {
            if (!CanReceiveKnockback ||
                !p_knockbackInfo.IsValid)
            {
                return false;
            }

            Vector3 direction = Vector3.ProjectOnPlane(
                p_knockbackInfo.Direction,
                Vector3.up);

            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                direction = Vector3.ProjectOnPlane(
                    _controller.transform.position -
                    p_knockbackInfo.Attacker.position,
                    Vector3.up);
            }

            if (direction.sqrMagnitude <= DirectionEpsilon)
                return false;

            _knockbackVelocity =
                direction.normalized *
                (p_knockbackInfo.Distance /
                 p_knockbackInfo.Duration);
            _knockbackRemainingTime =
                p_knockbackInfo.Duration;
            _isKnockbackActive = true;

            return true;
        }

        // 넉백은 현재 Locomotion State보다 먼저 이동하고 해당 프레임 입력 이동을 대체한다.
        public bool TickKnockback(float p_deltaTime)
        {
            if (!_isKnockbackActive)
                return false;

            if (!CanReceiveKnockback ||
                _moveModule == null ||
                _knockbackRemainingTime <= 0f)
            {
                CancelKnockback();
                return false;
            }

            float deltaTime = Mathf.Max(0f, p_deltaTime);
            float activeTime = Mathf.Min(
                _knockbackRemainingTime,
                deltaTime);

            Vector3 deltaPosition =
                _knockbackVelocity * activeTime;

            // 수평 넉백 중에도 접지력과 공중 중력은 계속 적용한다.
            deltaPosition.y = GroundVerticalVelocity * deltaTime;
            _moveModule.MoveDelta(deltaPosition);

            _knockbackRemainingTime -= activeTime;

            if (_knockbackRemainingTime <= 0f)
                CancelKnockback();

            return true;
        }

        public void CancelKnockback()
        {
            _isKnockbackActive = false;
            _knockbackVelocity = Vector3.zero;
            _knockbackRemainingTime = 0f;
        }



        #endregion ======================================== /Movement

        #region ======================================== Jump
        // 점프 시작 방향과 공중 속도를 고정하고 초기 수직 속도를 계산한다.
        public void StartJump(Vector2 p_moveInput, Transform p_cameraTransform, bool p_isSprint = false, bool p_isCombat = false)
        {
            // 이동 방향
            Vector3 moveDirection =
                _moveModule.GetMoveDirection(p_moveInput, p_cameraTransform, ELocomotionMode.Ground);

            // 속력
            float moveSpeed =
                _moveModule.GetMoveSpeed(ELocomotionMode.Ground, p_isSprint, p_isCombat);

            // 공중 State가 계속 사용할 방향과 속도를 Context에 저장한다.
            _context.LockedMoveDirection = moveDirection;
            _airMoveSpeed = moveSpeed;

            // 점프 시작 시 이동 방향으로 즉시 회전한다.
            _rotationModule.ApplyRotation(moveDirection, p_cameraTransform, false, false, true);

            VerticalVelocity = Mathf.Sqrt(2f * _gravity * _jumpHeight);
        }


        // 고정된 수평 이동과 현재 수직 속도를 합쳐 공중 이동을 적용한다.
        public void MoveAirborne(Vector3 p_direction)
        {
            Vector3 velocity = p_direction * _airMoveSpeed;

            velocity.y = VerticalVelocity;

            // 공중 이동도 최종적으로 MoveModule을 사용한다.
            _moveModule.Move(velocity);
        }
        #endregion ======================================== /Jump

        #region ======================================== Fall
        // 마지막 지상 속도에서 낙하 중 유지할 수평 방향과 속력을 추출한다.
        public void StartFall()
        {
            // 마지막 지상 이동 속도를 기준으로 공중 이동을 유지한다.
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_moveModule.Velocity, Vector3.up);

            _airMoveSpeed = horizontalVelocity.magnitude;

            _context.LockedMoveDirection = _airMoveSpeed > 0.001f ? horizontalVelocity.normalized : Vector3.zero;
        }
        #endregion ======================================== /Fall

        // 입력 또는 Player 정면으로 대시 방향을 결정하고 고정한다.
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

        // 거리와 지속 시간으로 대시 속도를 계산해 매 프레임 이동한다.
        public void DashUpdate(Vector3 p_direction)
        {
            float duration = Mathf.Max(_dashDuration, 0.01f);

            float dashSpeed = _dashDistance / duration;

            Vector3 velocity = p_direction.normalized * dashSpeed;

            velocity.y = GroundVerticalVelocity;

            _moveModule.Move(velocity);
        }

        #region Ground & Gravity
        // 접지 판정 후 현재 이동 State의 배율로 중력을 갱신한다.
        public void UpdateEnvironment(float p_gravityScale)
        {
            UpdateGroundCheck();
            UpdateGravity(p_gravityScale);
        }

        // CharacterController 하단을 기준으로 접지 검사 구의 중심을 계산한다.
        private Vector3 CalculateGroundCheckPoint(CharacterController p_controller)
        {
            Vector3 center = p_controller.transform.TransformPoint(p_controller.center);

            float bottomOffset = (p_controller.height * 0.5f) - p_controller.radius + _groundOffset;

            return center + (Vector3.down * bottomOffset);
        }

        // 하단 구체 검사 결과를 Module과 Context에 함께 반영한다.
        private void UpdateGroundCheck()
        {
            Vector3 groundPoint = CalculateGroundCheckPoint(_controller);

            IsGrounded = 
                Physics.CheckSphere(groundPoint, _controller.radius, _groundLayer, QueryTriggerInteraction.Ignore);

            _context.IsGrounded = IsGrounded;
        }

        // 접지 중에는 실제 중력 속도를 비우고, 공중에서만 중력을 누적한다.
        private void UpdateGravity(float p_gravityScale)
        {
            if (IsGrounded && VerticalVelocity <= 0f)
            {
                VerticalVelocity = 0f;
                return;
            }

            VerticalVelocity -= (_gravity * p_gravityScale * Time.deltaTime);
        }

        // 접지 검사 범위와 결과를 Scene 뷰 색상으로 표시한다.
        private void OnDrawGizmos()
        {
            // Play Mode 밖에서도 표시할 수 있도록 Controller 참조를 다시 탐색한다.
            CharacterController controller = _controller;

            if (controller == null)
                controller = GetComponent<CharacterController>();

            if (controller == null)
                return;

            // 현재 접지 결과에 따라 검사 Sphere의 색상을 구분한다.
            Vector3 groundPoint = CalculateGroundCheckPoint(controller);

            Gizmos.color = IsGrounded ? Color.green : Color.red;

            Gizmos.DrawWireSphere(groundPoint, controller.radius);
        }
        #endregion
    }
}
