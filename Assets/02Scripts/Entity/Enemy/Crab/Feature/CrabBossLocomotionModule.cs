using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public class CrabBossLocomotionModule : MonoBehaviour
    {
        [SerializeField] private Transform _owner;

        [Header("Chase")]
        [SerializeField, Min(0f)]
        private float _chaseSpeed = 8f;

        [Header("Rotation")]
        [SerializeField, Min(0f)]
        private float _rotationSpeed = 180f;

        [SerializeField, Range(0f, 10f)]
        private float _rotationCompleteAngle = 2f;

        public Transform Owner => _owner;
        public float ChaseSpeed => _chaseSpeed;

        private void Awake()
        {
            if (_owner == null)
            {
                CrabBossCore core = GetComponentInParent<CrabBossCore>();
                _owner = core != null ? core.transform : transform.parent;
            }
        }

        public bool TryChaseTarget(
            Vector3 p_direction,
            float p_distance,
            float p_stopDistance,
            float p_deltaTime,
            out float p_remainingDistance)
        {
            p_remainingDistance = p_distance;

            if (_owner == null ||
                p_distance < 0f ||
                p_direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float stopDistance = Mathf.Max(0f, p_stopDistance);
            float remainingDistance = p_distance - stopDistance;

            if (remainingDistance <= 0f)
                return true;

            // 한 프레임 이동량을 제한하여 정지 거리를 침범하지 않는다.
            float moveDistance = Mathf.Min(
                _chaseSpeed * Mathf.Max(0f, p_deltaTime),
                remainingDistance);

            _owner.position += p_direction.normalized * moveDistance;
            p_remainingDistance = p_distance - moveDistance;

            return true;
        }

        public bool RotateTowards(
            Vector3 p_direction,
            float p_deltaTime)
        {
            if (_owner == null)
                return false;

            p_direction.y = 0f;

            if (p_direction.sqrMagnitude <= 0.0001f)
                return true;

            Quaternion targetRotation =
                Quaternion.LookRotation(p_direction.normalized);

            _owner.rotation = Quaternion.RotateTowards(
                _owner.rotation,
                targetRotation,
                _rotationSpeed * Mathf.Max(0f, p_deltaTime));

            return Quaternion.Angle(
                       _owner.rotation,
                       targetRotation) <= _rotationCompleteAngle;
        }

        public bool RotateTowards(Transform p_target)
        {
            if (_owner == null || p_target == null)
                return false;

            // 매 프레임 현재 플레이어 위치로 방향 재계산
            Vector3 direction = p_target.position - _owner.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return true;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

            _owner.rotation = 
                Quaternion.RotateTowards(_owner.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

            return Quaternion.Angle(_owner.rotation, targetRotation) <= _rotationCompleteAngle;
        }

        public bool MoveTowards(Vector3 p_destination, float p_speed, float p_deltaTime)
        {
            if (_owner == null)
                return false;

            p_destination.y = _owner.position.y;
            _owner.position = Vector3.MoveTowards(
                _owner.position,
                p_destination,
                Mathf.Max(0f, p_speed) * Mathf.Max(0f, p_deltaTime));

            return (_owner.position - p_destination).sqrMagnitude <= 0.0001f;
        }

    }
}
