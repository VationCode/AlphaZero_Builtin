using UnityEngine;

namespace Alpha.Enemy.Boss
{
    public class BossLocomotionModule : MonoBehaviour
    {
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

        public Vector3 Position => _body != null ? _body.position : transform.position;
        public Vector3 Forward => _body != null ? _body.rotation * Vector3.forward : transform.forward;

        public void Bind(Rigidbody body)
        {
            _body = body;
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
