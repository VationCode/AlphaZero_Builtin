using UnityEngine;

namespace Alpha.Combat
{
    // 한 번의 넉백 요청에 필요한 공격자, 방향, 거리, 시간을 보관한다.
    public readonly struct KnockbackInfo
    {
        public Transform Attacker { get; }
        public Vector3 Direction { get; }
        public float Distance { get; }
        public float Duration { get; }

        public bool IsValid =>
            Attacker != null &&
            Direction.sqrMagnitude > 0.0001f &&
            Distance > 0f &&
            Duration > 0f;

        public KnockbackInfo(
            Transform p_attacker,
            Vector3 p_direction,
            float p_distance,
            float p_duration)
        {
            Attacker = p_attacker;
            Direction = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;
            Distance = Mathf.Max(0f, p_distance);
            Duration = Mathf.Max(0f, p_duration);
        }
    }
}
