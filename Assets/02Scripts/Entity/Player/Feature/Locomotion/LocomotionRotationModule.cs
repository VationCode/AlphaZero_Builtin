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

        // 회전을 적용할 Player Transform을 연결한다.
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
        /// 방향과 이동 공간에 맞는 회전을 계산해 Player에 적용한다.
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

        // 즉시·3차원·지상 회전 방식 중 현재 조건에 맞는 결과를 계산한다.
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

            // 즉시 회전은 이전 SmoothDamp 속도까지 초기화한다.
            if (p_isInstant)
            {
                _rotationVelocity = 0f;
                return targetRotation;
            }

            // 비행처럼 3차원 회전이 필요한 경우 프레임 독립 지수 보간을 사용한다.
            if (p_isSpatial)
            {
                float lerpRatio = 1f - Mathf.Exp(-_spatialRotationSmoothness * Time.deltaTime);

                return Quaternion.Slerp(currentRotation, targetRotation, lerpRatio);
            }

            // 지상에서는 Yaw만 부드럽게 보간하고 전투용 회전 시간을 구분한다.
            float smoothTime = p_isCombat? _combatRotationSmoothTime : _rotationSmoothTime;

            float smoothYaw = Mathf.SmoothDampAngle(currentRotation.eulerAngles.y, targetRotation.eulerAngles.y,
                                                    ref _rotationVelocity, smoothTime);

            return Quaternion.Euler(0f, smoothYaw, 0f);
        }



    }
}
