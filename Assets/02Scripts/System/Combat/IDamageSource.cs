using UnityEngine;

namespace Alpha.Combat
{
    // 현재 활성화된 공격의 피해 정보를 제공하는 공용 계약이다.
    public interface IDamageSource
    {
        int SourceId { get; }
        int AttackId { get; }

        bool TryCreateDamageInfo(
            Transform p_target,
            out DamageInfo p_damageInfo);
    }
}
