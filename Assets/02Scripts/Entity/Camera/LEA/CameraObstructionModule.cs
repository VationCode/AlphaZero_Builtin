using UnityEngine;

namespace Alpha.AlphaCamera
{
    /// <summary>
    /// Camera 시작점부터 희망 위치까지 장애물 안전 거리를 계산한다.
    /// </summary>
    public class CameraObstructionModule : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField] private LayerMask _obstructionMask;
        [SerializeField] private float _castRadius = 0.2f;
        [SerializeField] private float _surfacePadding = 0.05f;

        [Header("Recovery")]
        [SerializeField] private float _recoverySpeed = 15f;

        private Transform _castOrigin;

        private float _currentDistance;
        private bool _hasCurrentDistance;

        public bool IsBound { get; private set; }

        public bool Bind(Transform p_castOrigin)
        {
            if (p_castOrigin == null || _obstructionMask.value == 0)
            {
                Debug.LogError($"{nameof(CameraObstructionModule)}의 참조나 Mask가 설정되지 않았습니다.", this);

                return false;
            }

            _castOrigin = p_castOrigin;
            IsBound = true;

            ResetDistance();
            return true;
        }

        public bool TryResolveDistance(float p_desiredDistance, float p_deltaTime, out float p_resolvedDistance)
        {
            p_resolvedDistance = 0f;

            if (!IsBound) return false;

            float desiredDistance = Mathf.Max(0f, p_desiredDistance);

            Vector3 castDirection = _castOrigin.TransformDirection(Vector3.back);

            float targetDistance = desiredDistance;

            if (Physics.SphereCast(_castOrigin.position, _castRadius, castDirection, out RaycastHit hit,
                                   desiredDistance, _obstructionMask, QueryTriggerInteraction.Ignore))
            {
                targetDistance = Mathf.Clamp(hit.distance - _surfacePadding, 0f, desiredDistance);
            }

            if (!_hasCurrentDistance)
            {
                _currentDistance = targetDistance;
                _hasCurrentDistance = true;
            }
            else if (targetDistance < _currentDistance)
            {
                // 장애물 접근 시에는 관통하지 않도록 즉시 당긴다.
                _currentDistance = targetDistance;
            }
            else
            {
                // 장애물이 사라지면 원래 거리로 부드럽게 복귀한다.
                float recoveryRatio = 1f - Mathf.Exp(-_recoverySpeed * Mathf.Max(0f, p_deltaTime));

                _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, recoveryRatio);
            }

            p_resolvedDistance = _currentDistance;
            return true;
        }

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
