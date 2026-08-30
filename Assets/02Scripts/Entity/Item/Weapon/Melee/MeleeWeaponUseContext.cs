using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    // Weapon이 사용자 구현을 알지 않고 공격 출처와 보정값만 전달받는다.
    public readonly struct MeleeWeaponUseContext
    {
        public Transform Attacker { get; }
        public Transform AttackSource { get; }
        public float AdditionalDamage { get; }
        public bool IsValid => Attacker != null && AttackSource != null;

        public MeleeWeaponUseContext(
            Transform p_attacker,
            Transform p_attackSource,
            float p_additionalDamage)
        {
            Attacker = p_attacker;
            AttackSource = p_attackSource != null
                ? p_attackSource
                : p_attacker;
            AdditionalDamage = Mathf.Max(0f, p_additionalDamage);
        }
    }
}
