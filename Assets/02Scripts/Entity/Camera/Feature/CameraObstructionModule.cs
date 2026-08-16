using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Shoulder와 희망 Camera 위치 사이의 장애물을 검사해 안전 거리를 계산한다.
    public class CameraObstructionModule : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField] private LayerMask _obstructionMask;
        [SerializeField, Min(0.01f)] private float _castRadius = 0.2f;
        [SerializeField, Min(0f)] private float _surfacePadding = 0.05f;

        [Header("Recovery")]
        [SerializeField, Min(0f)] private float _recoverySpeed = 15f;

        private Transform _castOrigin;
        private float _currentDistance;
        private bool _hasCurrentDistance;

        public bool IsInitialized { get; private set; }

        // 장애물 검사의 시작점을 연결하고 거리 보정 상태를 초기화한다.
        public bool Initialize(Transform p_castOrigin)
        {
            if (IsInitialized)
                return true;

            if (p_castOrigin == null || _obstructionMask.value == 0)
            {
                Debug.LogError(
                    $"{nameof(CameraObstructionModule)}의 Cast Origin 또는 Obstruction Mask가 설정되지 않았습니다.",
                    this);
                return false;
            }

            _castOrigin = p_castOrigin;
            IsInitialized = true;
            ResetDistance();

            return true;
        }

        // 장애물이 가까워지면 즉시 당기고, 사라지면 희망 거리로 부드럽게 복귀한다.
        public bool TryResolveDistance(
            float p_desiredDistance,
            float p_deltaTime,
            out float p_resolvedDistance)
        {
            p_resolvedDistance = 0f;

            if (!IsInitialized)
                return false;

            float desiredDistance = Mathf.Max(0f, p_desiredDistance);
            float targetDistance = desiredDistance;

            if (desiredDistance > Mathf.Epsilon)
            {
                Vector3 castDirection = _castOrigin.TransformDirection(Vector3.back);

                if (Physics.SphereCast(
                        _castOrigin.position,
                        _castRadius,
                        castDirection,
                        out RaycastHit hit,
                        desiredDistance,
                        _obstructionMask,
                        QueryTriggerInteraction.Ignore))
                {
                    targetDistance = Mathf.Clamp(
                        hit.distance - _surfacePadding,
                        0f,
                        desiredDistance);
                }
            }

            if (!_hasCurrentDistance || targetDistance < _currentDistance)
            {
                // 관통 방지를 위해 장애물 방향으로는 보간하지 않는다.
                _currentDistance = targetDistance;
                _hasCurrentDistance = true;
            }
            else
            {
                float recoveryRatio =
                    1f - Mathf.Exp(-_recoverySpeed * Mathf.Max(0f, p_deltaTime));

                _currentDistance = Mathf.Lerp(
                    _currentDistance,
                    targetDistance,
                    recoveryRatio);
            }

            p_resolvedDistance = _currentDistance;
            return true;
        }

        // View 전환 시작 시 이전 View의 보정 거리를 제거한다.
        public void ResetDistance()
        {
            _currentDistance = 0f;
            _hasCurrentDistance = false;
        }

        private void OnValidate()
        {
            _castRadius = Mathf.Max(0.01f, _castRadius);
            _surfacePadding = Mathf.Max(0f, _surfacePadding);
            _recoverySpeed = Mathf.Max(0f, _recoverySpeed);
        }
    }
}
