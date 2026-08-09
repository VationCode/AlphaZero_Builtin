using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 공격 실행 후 View가 사용할 즉시 판정 결과다.
    public readonly struct RangeAttackResult
    {
        public bool HasImmediateEndPoint { get; }
        public Vector3 EndPoint { get; }

        private RangeAttackResult(
            bool p_hasImmediateEndPoint,
            Vector3 p_endPoint)
        {
            HasImmediateEndPoint =
                p_hasImmediateEndPoint;

            EndPoint = p_endPoint;
        }

        public static RangeAttackResult Immediate(
            Vector3 p_endPoint)
        {
            return new RangeAttackResult(
                true,
                p_endPoint);
        }

        public static RangeAttackResult Deferred()
        {
            return new RangeAttackResult(
                false,
                Vector3.zero);
        }
    }
}