using UnityEngine;

// 발사 순간 하나의 Ray로 충돌을 검사한다.
public class HitscanGunFireStrategy : IGunFireStrategy
{
    public bool TryFire(Gun p_gun, in WeaponAttackContext p_context)
    {
        if (p_gun == null || p_gun.GunData == null ||
            p_gun.FirePoint == null || !p_context.IsValid)
        {
            return false;
        }

        float maxDistance = p_gun.GunData.MaxDistance;

        if (maxDistance <= 0f)
            return false;

        bool hasHit = 
            Physics.Raycast(p_gun.FirePoint.position, p_context.AttackDirection, 
                            out RaycastHit hit, maxDistance, p_context.HitMask, QueryTriggerInteraction.Ignore);

        if (hasHit)
        {
            // 다음 단계에서 피격 처리로 연결한다.
        }

        // 명중 여부와 관계없이 Ray가 발사됐다면 성공이다.
        return true;
    }
}
