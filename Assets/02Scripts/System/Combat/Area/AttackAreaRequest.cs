using UnityEngine;

namespace Alpha.Combat
{
    // 한 번의 범위 검색에 필요한 월드 기준 자세와 설정을 보관한다.
    public readonly struct AttackAreaRequest
    {
        public Vector3 Origin { get; }
        public Vector3 Forward { get; }
        public Vector3 Up { get; }
        public Quaternion Rotation { get; }
        public Transform Attacker { get; }
        public AttackAreaSettings Settings { get; }

        public Vector3 AreaOrigin => Settings != null
            ? Origin + Rotation * Settings.LocalOffset
            : Origin;

        public bool IsValid => Settings != null && Settings.IsValid;

        public AttackAreaRequest(
            Vector3 p_origin,
            Vector3 p_forward,
            Vector3 p_up,
            Transform p_attacker,
            AttackAreaSettings p_settings)
        {
            Origin = p_origin;
            Attacker = p_attacker;
            Settings = p_settings;

            Vector3 forward = p_forward.sqrMagnitude > 0.0001f
                ? p_forward.normalized
                : Vector3.forward;

            Vector3 up = p_up.sqrMagnitude > 0.0001f
                ? p_up.normalized
                : Vector3.up;

            // Forward와 Up이 나란하면 LookRotation이 실패하므로 대체 축을 만든다.
            if (Vector3.Cross(forward, up).sqrMagnitude <= 0.0001f)
            {
                up = Vector3.ProjectOnPlane(Vector3.up, forward);

                if (up.sqrMagnitude <= 0.0001f)
                    up = Vector3.ProjectOnPlane(Vector3.forward, forward);

                up.Normalize();
            }

            Rotation = Quaternion.LookRotation(forward, up);
            Forward = Rotation * Vector3.forward;
            Up = Rotation * Vector3.up;
        }
    }
}
