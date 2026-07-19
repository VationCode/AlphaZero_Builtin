using System;
using UnityEngine;

namespace Alpha.Player
{
    [Serializable]
    public class LocomotionGroundFeature
    {
        [SerializeField]
        private LayerMask _groundLayer;

        [SerializeField, Min(0f)]
        private float _groundOffset = 0.07f;

        private Transform _owner;
        private CharacterController _controller;

        private bool _checkEnabled = true;

        public bool IsGrounded { get; private set; }

        // Motor의 물리 객체를 연결한다.
        public void Bind(Transform p_owner, CharacterController p_controller)
        {
            _owner = p_owner;
            _controller = p_controller;
        }

        // 현재 접지 상태를 갱신한다.
        public void UpdateGroundChecked(float p_verticalVelocity)
        {
            if (_owner == null || _controller == null)
            {
                IsGrounded = false;
                return;
            }

            if (!_checkEnabled)
            {
                IsGrounded = false;

                // 상승이 끝나면 접지 검사를 다시 활성화한다.
                if (p_verticalVelocity > 0f)
                    return;

                _checkEnabled = true;
            }

            Vector3 center = _owner.TransformPoint(_controller.center);

            float bottomOffset = (_controller.height * 0.5f) - _controller.radius + _groundOffset;

            Vector3 groundPoint = center + (Vector3.down * bottomOffset);

            IsGrounded = Physics.CheckSphere(groundPoint, _controller.radius, _groundLayer, QueryTriggerInteraction.Ignore);
        }

        // 점프 직후 자신의 지면을 다시 감지하지 않도록 한다.
        public void DisableUntilFalling()
        {
            _checkEnabled = false;
            IsGrounded = false;
        }

        public void Enable()
        {
            _checkEnabled = true;
        }
    }
}
