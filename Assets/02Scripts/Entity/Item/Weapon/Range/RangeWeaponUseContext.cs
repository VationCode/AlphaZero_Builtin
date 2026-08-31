using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // Weapon이 사용자의 구체 클래스를 알지 않고 공격 출처와 보정값만 전달받는다.
    public readonly struct RangeWeaponUseContext
    {
        public Transform Attacker { get; }
        public float AdditionalDamage { get; }
        public bool IsValid => Attacker != null;

        public RangeWeaponUseContext(
            Transform p_attacker,
            float p_additionalDamage)
        {
            Attacker = p_attacker;
            AdditionalDamage = Mathf.Max(0f, p_additionalDamage);
        }
    }

    // Player가 계산한 현재 조준 자세를 RangeWeapon에 값으로 전달한다.
    public readonly struct RangeWeaponAttackPose
    {
        public Vector3 Origin { get; }
        public Vector3 TargetPoint { get; }
        public Vector3 Direction { get; }
        public bool IsValid =>
            (TargetPoint - Origin).sqrMagnitude > 0.0001f;

        public RangeWeaponAttackPose(
            Vector3 p_origin,
            Vector3 p_targetPoint)
        {
            Origin = p_origin;
            TargetPoint = p_targetPoint;

            Vector3 direction = p_targetPoint - p_origin;
            Direction = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.zero;
        }
    }
}
