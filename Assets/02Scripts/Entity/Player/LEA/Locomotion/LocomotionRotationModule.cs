using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // Player의 실제 회전 계산과 적용을 담당한다.
    public class LocomotionRotationModule : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField, Min(0f)]
        private float _rotationSmoothTime = 0.1f;

        [SerializeField, Min(0.01f)]
        private float _combatRotationSmoothTime = 0.05f;

        [SerializeField, Min(0f)]
        private float _spatialRotationSmoothness = 10f;

        private Transform _playerTransform;
        private float _rotationVelocity;

        public bool IsBound => _playerTransform != null;

        public void Bind(Transform p_playerTransform)
        {
            if (p_playerTransform == null)
            {
                Debug.LogError($"{nameof(LocomotionRotationModule)}에 Player Transform이 없습니다.");

                return;
            }

            _playerTransform = p_playerTransform;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_direction"></param>
        /// <param name="p_isSpatial"> 3차원 공간판별 </param>
        /// <param name="p_isCombat"> </param>
        /// <param name="p_isInstant"> 즉시 회전 </param>
        public void ApplyRotation(Vector3 p_direction, Transform p_cameraTransform,
                                  bool p_isSpatial = false, bool p_isCombat = false, bool p_isInstant = false)
        {
            if (!IsBound)
                return;

            _playerTransform.rotation = CalculateRotation(p_direction, p_cameraTransform, p_isSpatial, p_isCombat, p_isInstant);
        }

        // 바라볼 방향 결정(Input, Aim, Mouse)
        // IsFacingDirection 호출 -> ApplyRotation에 바라볼 방향 입력
        public bool IsFacingDirection(Vector3 p_direction, float p_toleranceAngle)
        {
            if (!IsBound)
                return false;

            Vector3 currentForward = Vector3.ProjectOnPlane(_playerTransform.forward, Vector3.up);
            Vector3 targetDirection = Vector3.ProjectOnPlane(p_direction, Vector3.up);

            if (targetDirection.sqrMagnitude < 0.0001f)
                return false;

            float angle = Vector3.Angle(currentForward, targetDirection);
            return angle <= Mathf.Max(0f, p_toleranceAngle);
        }

        private Quaternion CalculateRotation(Vector3 p_direction, Transform p_cameraTransform,
                                             bool p_isSpatial, bool p_isCombat, bool p_isInstant)
        {
            Quaternion currentRotation = _playerTransform.rotation;

            Vector3 forward = p_direction;

            if (!p_isSpatial)
                forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                return currentRotation;

            Vector3 up = p_isSpatial && p_cameraTransform != null? p_cameraTransform.up : Vector3.up;

            Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, up);

            // 타겟방향으로 즉시 회전
            if (p_isInstant)
            {
                _rotationVelocity = 0f;
                return targetRotation;
            }

            // 3차원 공간 계산으로의 회전
            if (p_isSpatial)
            {
                float lerpRatio = 1f - Mathf.Exp(-_spatialRotationSmoothness * Time.deltaTime);

                return Quaternion.Slerp(currentRotation, targetRotation, lerpRatio);
            }

            // Y축 기반 XZ 방향으로의 이동
            float smoothTime = p_isCombat? _combatRotationSmoothTime : _rotationSmoothTime;

            float smoothYaw = Mathf.SmoothDampAngle(currentRotation.eulerAngles.y, targetRotation.eulerAngles.y,
                                                    ref _rotationVelocity, smoothTime);

            return Quaternion.Euler(0f, smoothYaw, 0f);
        }



    }
}
