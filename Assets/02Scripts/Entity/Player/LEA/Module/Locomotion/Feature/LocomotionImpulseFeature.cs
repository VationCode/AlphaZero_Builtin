using System;
using UnityEngine;

namespace Alpha.Player
{
    [Serializable]
    public class LocomotionImpulseFeature
    {
        [Header("Dash")]
        [SerializeField, Min(0f)]
        private float _dashDistance = 7f;

        [SerializeField, Min(0.001f)]
        private float _dashDuration = 0.4f;

        [Header("Knockback")]
        [SerializeField, Min(0f)]
        private float _knockbackDistance = 3f;

        [SerializeField, Min(0.001f)]
        private float _knockbackDuration = 0.25f;

        private Vector3 _direction;
        private float _distance;
        private float _duration;
        private float _movedDistance;

        public bool IsActive { get; private set; }

        public void StartDash(Vector3 p_direction, bool p_isPlanar)
        {
            StartImpulse(p_direction, _dashDistance, _dashDuration, p_isPlanar);
        }

        public void StartKnockback(Vector3 p_direction, bool p_isPlanar)
        {
            StartImpulse(p_direction, _knockbackDistance, _knockbackDuration, p_isPlanar);
        }

        // 사용자 정의 Impulse도 실행할 수 있다.
        private void StartImpulse(Vector3 p_direction, float p_distance, float p_duration, bool p_isPlanar)
        {
            if (p_isPlanar)
            {
                p_direction = Vector3.ProjectOnPlane(
                    p_direction,
                    Vector3.up);
            }

            if (p_direction.sqrMagnitude < 0.001f)
                return;

            if (p_distance <= 0f)
                return;

            _direction = p_direction.normalized;
            _distance = p_distance;
            _duration = Mathf.Max(p_duration, 0.001f);

            _movedDistance = 0f;
            IsActive = true;
        }

        // 현재 프레임의 Impulse 속도를 반환한다.
        public Vector3 UpdateImpulse(float p_deltaTime)
        {
            if (!IsActive || p_deltaTime <= 0f)
                return Vector3.zero;

            float speed = _distance / _duration;

            float remainingDistance = _distance - _movedDistance;

            float moveDistance = Mathf.Min(speed * p_deltaTime, remainingDistance);

            _movedDistance += moveDistance;

            Vector3 velocity = _direction * (moveDistance / p_deltaTime);

            if (_movedDistance >= _distance) Cancel();

            return velocity;
        }

        public void Cancel()
        {
            IsActive = false;

            _direction = Vector3.zero;
            _distance = 0f;
            _duration = 0f;
            _movedDistance = 0f;
        }
    }
}