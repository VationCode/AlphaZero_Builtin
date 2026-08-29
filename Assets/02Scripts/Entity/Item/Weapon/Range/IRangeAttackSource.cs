using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // RangeWeapon에 공격자와 Camera 기준 목표점을 제공하는 계약이다.
    public interface IRangeAttackSource
    {
        Transform Attacker { get; }

        float ResolveDamage(float p_baseDamage);

        bool TryGetAttackPose(
            Vector3 p_muzzleOrigin,
            float p_maxDistance,
            out Vector3 p_attackOrigin,
            out Vector3 p_targetPoint);
    }
}
