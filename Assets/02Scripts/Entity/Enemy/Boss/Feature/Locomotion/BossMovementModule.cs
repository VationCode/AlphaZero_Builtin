using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 추적의 이동·회전만 실행한다. 행동 상태는 Flow에서 결정한다.
    [Serializable]
    public sealed class BossMovementModule
    {
        [SerializeField, Min(0f), Tooltip("초당 수평 이동 거리입니다. 낙하 속도는 유지합니다.")]
        private float _moveSpeed = 5f;
        [SerializeField, Min(0f), Tooltip("타겟을 향해 초당 회전하는 각도입니다.")]
        private float _rotationSpeed = 180f;
        private Rigidbody _body;
        public bool IsMoving { get; private set; }
        public Vector3 Position => _body != null ? _body.position : Vector3.zero;

        public void Bind(Rigidbody p_body) { Stop(); _body = p_body; }

        public void Chase(Vector3 p_target, float p_stopDistance, float p_deltaTime)
        {
            if (_body == null || _body.isKinematic || !Finite(p_deltaTime) || p_deltaTime <= 0f)
            { Stop(); return; }
            Vector3 offset = p_target - _body.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (!Finite(distance)) { Stop(); return; }
            if (distance > 0.001f)
                _body.MoveRotation(Quaternion.RotateTowards(_body.rotation,
                    Quaternion.LookRotation(offset / distance, Vector3.up), _rotationSpeed * p_deltaTime));
            float remaining = distance - p_stopDistance;
            // 공격 시작 거리 직전에 멈춰 실행 조건에 도달하지 못하는 틈을 남기지 않는다.
            if (remaining <= 0f || _moveSpeed <= 0f) { Stop(); return; }
            Vector3 velocity = offset / distance * Mathf.Min(_moveSpeed, remaining / p_deltaTime);
            velocity.y = _body.linearVelocity.y;
            _body.linearVelocity = velocity;
            IsMoving = true;
        }

        // 추적이 제어한 수평 속도만 해제한다. 다른 기능이 받은 이동 제어권은 건드리지 않는다.
        public void Stop()
        {
            if (IsMoving && _body != null && !_body.isKinematic)
                _body.linearVelocity = new Vector3(0f, _body.linearVelocity.y, 0f);
            IsMoving = false;
        }

        internal void Validate()
        {
            _moveSpeed = Finite(_moveSpeed) ? Mathf.Max(0f, _moveSpeed) : 5f;
            _rotationSpeed = Finite(_rotationSpeed) ? Mathf.Max(0f, _rotationSpeed) : 180f;
        }
        private static bool Finite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
    }
}
