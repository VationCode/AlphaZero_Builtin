using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // RangeWeapon에 공격자와 실제 3차원 발사 방향을 제공하는 계약이다.
    public interface IRangeAttackSource
    {
        Transform Attacker { get; }

        bool TryGetAttackDirection(
            Vector3 p_origin,
            float p_maxDistance,
            out Vector3 p_direction);
    }
}
