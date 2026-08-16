using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public class CrabBossLocomotionModule : MonoBehaviour
    {
        [SerializeField] private Transform _owner;

        [Header("Approach")]
        [SerializeField, Min(0f)]
        private float _approachSpeed = 5f;

        [SerializeField, Min(0f)]
        private float _arrivalTolerance = 0.1f;

        [Header("Rotation")]
        [SerializeField, Min(0f)]
        private float _rotationSpeed = 180f;

        [SerializeField, Range(0f, 10f)]
        private float _rotationCompleteAngle = 2f;

        public Transform Owner => _owner;

        public float ApproachSpeed => _approachSpeed;


        private void Awake()
        {
            if (_owner == null)
            {
                CrabBossCore core = GetComponentInParent<CrabBossCore>();
                _owner = core != null ? core.transform : transform.parent;
            }
        }

        public bool TryCalculateDistanceTo(Transform p_target, out float p_distance)
        {
            p_distance = float.PositiveInfinity;

            if (_owner == null || p_target == null)
                return false;

            // 높이를 제외한 전투용 수평 거리를 계산한다.
            Vector3 direction = p_target.position - _owner.position;
            direction.y = 0f;
            p_distance = direction.magnitude;

            return true;
        }

        public bool TryApproachTarget(
            Transform p_target,
            float p_stopDistance,
            float p_deltaTime,
            out float p_distance,
            out bool p_reached)
        {
            p_distance = float.PositiveInfinity;
            p_reached = false;

            if (_owner == null || p_target == null)
                return false;

            Vector3 direction = p_target.position - _owner.position;
            direction.y = 0f;

            p_distance = direction.magnitude;

            float stopDistance = Mathf.Max(0f, p_stopDistance);
            float remainingDistance = p_distance - stopDistance;

            if (remainingDistance <= _arrivalTolerance)
            {
                p_reached = true;
                return true;
            }

            // 정지 거리를 침범하지 않도록 이동량 제한
            float moveDistance = 
                Mathf.Min(_approachSpeed * p_deltaTime, remainingDistance);

            _owner.position +=
                direction.normalized * moveDistance;

            p_distance -= moveDistance;
            p_reached = p_distance - stopDistance <= _arrivalTolerance;

            return true;
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

        public bool MoveTowards(
            Vector3 p_destination,
            float p_speed,
            float p_deltaTime)
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
