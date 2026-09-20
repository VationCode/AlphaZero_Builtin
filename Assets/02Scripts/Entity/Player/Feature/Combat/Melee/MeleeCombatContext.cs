using Alpha.Item.Weapon.Melee;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // 현재 근접 무기와 Player 공격에 필요한 런타임 값을 보관한다.
    internal sealed class MeleeCombatContext
    {
        public MeleeWeapon Weapon { get; private set; }
        public Transform Attacker { get; private set; }
        public Transform AttackSource { get; private set; }
        public float AdditionalDamage { get; private set; }

        public bool IsValid =>
            Weapon != null &&
            Weapon.IsInitialized &&
            Attacker != null &&
            AttackSource != null;

        public bool Bind(
            MeleeWeapon p_weapon,
            Transform p_attacker,
            Transform p_attackSource,
            float p_additionalDamage)
        {
            if (p_weapon == null ||
                !p_weapon.IsInitialized ||
                p_attacker == null)
            {
                return false;
            }

            Weapon = p_weapon;
            Attacker = p_attacker;
            AttackSource = p_attackSource != null
                ? p_attackSource
                : p_attacker;
            AdditionalDamage = Mathf.Max(0f, p_additionalDamage);
            return true;
        }

        public void Clear()
        {
            Weapon = null;
            Attacker = null;
            AttackSource = null;
            AdditionalDamage = 0f;
        }
    }
}
