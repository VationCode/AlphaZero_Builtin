using UnityEngine;

namespace Alpha.Enemy.Boss
{
    public class BossLocomotionModule : MonoBehaviour
    {
        // 모델의 점프 애니메이션이 높이를 표현하므로 몸체는 지형을 넘을 여유만 둔다.
        private const float JumpBodyClearance = 1f;

        [Header("Reference")]
        [SerializeField] private Rigidbody _body;

        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float _moveSpeed = 5f;

        [SerializeField, Min(0f)]
        private float _acceleration = 10f;

        [SerializeField, Min(0f)]
        private float _deceleration = 15f;

        [Header("Rotation")]
        [SerializeField, Min(0f)]
        [Tooltip("초당 회전 각도")]
        private float _turnSpeed = 180f;

        [SerializeField, Range(0f, 180f)]
        [Tooltip("목표 방향에 도달했다고 판단할 각도")]
        private float _facingTolerance = 5f;

        private Vector3 _jumpStart;
        private Vector3 _jumpDestination;
        private bool _jumpActive;
        private float _jumpApexY;
        private bool _jumpGravity;
        private Vector3 _dashStart;
        private Vector3 _dashDestination;
        private bool _dashActive;

        public Vector3 Position => _body != null ? _body.position : transform.position;
        public Vector3 Forward => _body != null ? _body.rotation * Vector3.forward : transform.forward;

        public void Bind(Rigidbody body)
        {
            CancelJump();
            _dashActive = false;
            _body = body;
        }

        // Flow가 확정한 고정 거리의 도착점까지 수평으로 돌진한다.
        public void BeginDashTo(Vector3 destination)
        {
            if (_body == null)
                return;

            Stop();
            _dashStart = _body.position;
            _dashDestination = new Vector3(destination.x, _dashStart.y, destination.z);
            _dashActive = true;
        }

        public void AdvanceDash(float progress)
        {
            if (!_dashActive || _body == null)
                return;

            Vector3 position = Vector3.Lerp(_dashStart, _dashDestination,
                Mathf.Clamp01(progress));
            position.y = _body.position.y;
            _body.MovePosition(position);
        }

        public void CompleteDash()
        {
            if (!_dashActive || _body == null)
                return;

            _body.position = new Vector3(_dashDestination.x,
                _body.position.y, _dashDestination.z);
            _dashActive = false;
            Stop();
        }

        public void CancelDash()
        {
            _dashActive = false;
            Stop();
        }

        // Attack3과 Attack6가 목적지 높이를 포함한 같은 점프 경로를 사용한다.
        public void BeginJumpTo(Vector3 targetPosition)
        {
            if (_body == null)
                return;

            CancelJump();
            Stop();
            _jumpStart = _body.position;
            _jumpDestination = targetPosition;
            _jumpApexY = Mathf.Max(_jumpStart.y, _jumpDestination.y) +
                JumpBodyClearance;
            _jumpGravity = _body.useGravity;
            _body.useGravity = false;
            _body.linearVelocity = Vector3.zero;
            _jumpActive = true;
        }

        public void AdvanceJump(float progress)
        {
            if (!_jumpActive || _body == null)
                return;

            float time = Mathf.Clamp01(progress);
            Vector3 position = Vector3.Lerp(_jumpStart, _jumpDestination, time);
            float midpointY = (_jumpStart.y + _jumpDestination.y) * 0.5f;
            position.y += 4f * (_jumpApexY - midpointY) * time * (1f - time);
            _body.MovePosition(position);
        }

        public void CompleteJump()
        {
            if (!_jumpActive || _body == null)
                return;

            // HitStart 전에 착지점으로 맞춰 이펙트와 피해 범위가 몸 위치와 일치한다.
            _body.position = _jumpDestination;
            _body.useGravity = _jumpGravity;
            _jumpActive = false;
            Stop();
        }

        public void CancelJump()
        {
            if (!_jumpActive || _body == null)
                return;
            _body.useGravity = _jumpGravity;
            _jumpActive = false;
            Stop();
        }


        public bool MoveTo(Vector3 targetPosition, float stopDistance)
        {
            if (_body == null)
                return false;

            Vector3 offset = targetPosition - _body.position;
            offset.y = 0f; // 지면에서의 이동 방향

            float distance = offset.magnitude;
            float stoppingDistance = Mathf.Max(0f, stopDistance);

            if (distance <= stoppingDistance)
            {
                Stop();
                return true; // 목표 거리 도달
            }

            // 한 물리 프레임에 남은 거리보다 멀리 이동하지 않는다.
            float remainingDistance = distance - stoppingDistance;
            float speed = Mathf.Min(_moveSpeed, remainingDistance / Time.fixedDeltaTime);
            Vector3 moveVelocity = offset / distance * speed;

            Vector3 velocity = _body.linearVelocity;
            velocity.x = moveVelocity.x;
            velocity.z = moveVelocity.z;
            _body.linearVelocity = velocity; // 중력을 위한 Y 속도 유지

            return false;
        }

        public void Stop()
        {
            if (_body == null)
                return;

            Vector3 velocity = _body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            _body.linearVelocity = velocity;
        }

        public bool RotateTo(Vector3 targetPosition)
        {
            if (_body == null)
                return false;

            Vector3 direction = targetPosition - _body.position;
            direction.y = 0f; // 보스의 Y축만 회전

            if (direction.sqrMagnitude < 0.0001f)
                return true;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            if (Quaternion.Angle(_body.rotation, targetRotation) <= _facingTolerance)
                return true;

            Quaternion nextRotation = Quaternion.RotateTowards(
                _body.rotation,
                targetRotation,
                _turnSpeed * Time.fixedDeltaTime);

            _body.MoveRotation(nextRotation);
            return Quaternion.Angle(nextRotation, targetRotation) <= _facingTolerance;
        }
    }
}
