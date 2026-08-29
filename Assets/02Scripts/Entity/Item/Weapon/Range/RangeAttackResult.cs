using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
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
