using System;
using UnityEngine;
namespace Alpha.Player
{
    [Serializable]
    public class LocomotionGravityFeature
    {
        [SerializeField, Min(0f)]
        private float _gravity = 15f;

        [SerializeField, Min(0f)]
        private float _groundedForce = 0.5f;

        public float Gravity => _gravity;
        public float VerticalVelocity { get; private set; }

        // 현재 모드의 GravityScale에 따라 중력을 적용한다.
        public void UpdateGravity(bool p_isGrounded, float p_gravityScale, float p_deltaTime)
        {
            if (p_deltaTime <= 0f)
                return;

            if (p_isGrounded && VerticalVelocity <= 0f)
            {
                VerticalVelocity = -_groundedForce;

                return;
            }

            VerticalVelocity -= _gravity * p_gravityScale * p_deltaTime;
        }

        public void SetVelocity(float p_velocity)
        {
            VerticalVelocity = p_velocity;
        }

        public void AddVelocity(float p_velocity)
        {
            VerticalVelocity += p_velocity;
        }

        public void Reset()
        {
            VerticalVelocity = 0f;
        }
    }
}
