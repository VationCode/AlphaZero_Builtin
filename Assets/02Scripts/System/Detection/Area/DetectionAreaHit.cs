using UnityEngine;

namespace Alpha.Detection
{
    // 공용 공간 탐지에서 찾은 Collider와 대표 지점·방향을 반환한다.
    public readonly struct DetectionAreaHit
    {
        public Collider Collider { get; }
        public Vector3 HitPoint { get; }
        public Vector3 Direction { get; }

        public bool IsValid => Collider != null;

        public DetectionAreaHit(
            Collider p_collider,
            Vector3 p_hitPoint,
            Vector3 p_direction)
        {
            Collider = p_collider;
            HitPoint = p_hitPoint;
            Direction = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;
        }
    }
}
