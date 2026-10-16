using Alpha.Living;
using UnityEngine;

namespace Alpha.Enemy
{
    public class TargetDetectionModule : MonoBehaviour
    {
        [Tooltip("본체"), SerializeField]
        private GameObject _origin;

        [SerializeField, Min(0f)]
        private float _maxDetectionDistance = 60f;
        public float MaxDetectionDistance => _maxDetectionDistance;

        [SerializeField]
        private GameObject _target;
        private LivingModule _targetLiving;

        public bool IsChase;

        private void Awake()
        {
            FindTargetIfNeeded();
        }

        public void Bind(GameObject origin)
        {
            _origin = origin;
        }

        // 타깃을 잃거나 탐지 범위를 벗어난 동안은 공격 결정을 중단한다.
        public bool TryGetTarget(out Transform target)
        {
            target = null;
            FindTargetIfNeeded();

            if (_origin == null || _target == null ||
                !_target.activeInHierarchy || _maxDetectionDistance <= 0f ||
                (_targetLiving != null && _targetLiving.IsDead))
                return false;

            Vector3 offset = _target.transform.position - _origin.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > _maxDetectionDistance * _maxDetectionDistance)
                return false;

            target = _target.transform;
            return true;
        }

        public float CalculationDistance()
        {
            if (_origin == null || _target == null)
                return float.PositiveInfinity;

            Vector3 offset = _target.transform.position - _origin.transform.position;
            offset.y = 0f; // 높이 차이를 제외한 수평 거리
            return offset.magnitude;
        }

        private void FindTargetIfNeeded()
        {
            if (_target == null)
                _target = GameObject.FindGameObjectWithTag("Player");

            if (_target != null && _targetLiving == null)
                _targetLiving = _target.GetComponentInChildren<LivingModule>(true);
        }
    }
}
