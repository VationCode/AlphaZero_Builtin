using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 조준 미리보기가 사용할 계산된 궤적 수와 예상 충돌 정보를 보관한다.
    public readonly struct ProjectileTrajectoryResult
    {
        public int PointCount { get; }
        public bool HasImpact { get; }
        public Vector3 ImpactPoint { get; }
        public Vector3 ImpactNormal { get; }

        public ProjectileTrajectoryResult(
            int p_pointCount,
            bool p_hasImpact,
            Vector3 p_impactPoint,
            Vector3 p_impactNormal)
        {
            PointCount = Mathf.Max(0, p_pointCount);
            HasImpact = p_hasImpact;
            ImpactPoint = p_impactPoint;
            ImpactNormal = p_impactNormal;
        }
    }

    // 한 탄도의 즉시 판정 경로를 View에 전달하는 값 객체다.
    public readonly struct RangeAttackResult
    {
        public Vector3 StartPoint { get; }
        public Vector3 EndPoint { get; }
        public bool HasCollision { get; }
        public Vector3 CollisionNormal { get; }

        public bool HasVisiblePath =>
            (EndPoint - StartPoint).sqrMagnitude > 0.0001f;

        public RangeAttackResult(
            Vector3 p_startPoint,
            Vector3 p_endPoint,
            bool p_hasCollision,
            Vector3 p_collisionNormal)
        {
            StartPoint = p_startPoint;
            EndPoint = p_endPoint;
            HasCollision = p_hasCollision;
            CollisionNormal = p_collisionNormal.sqrMagnitude > 0.0001f
                ? p_collisionNormal.normalized
                : Vector3.up;
        }
    }
}
