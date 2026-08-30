using Alpha.Item.Weapon.Melee;
using System;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player의 공격 출처와 능력치만 MeleeWeapon에 전달하고 결과를 중계한다.
    [Serializable]
    public sealed class PlayerMeleeWeaponUseModule
    {
        private Transform _attacker;
        private MeleeWeapon _activeWeapon;
        private Action<MeleeSkillDefinition> _effectRequested;
        private Action<MeleeSkillDefinition> _hitConfirmed;

        public int CurrentSkillIndex =>
            _activeWeapon?.CurrentSkillIndex ?? -1;
        public MeleeSkillDefinition CurrentSkill =>
            _activeWeapon?.CurrentSkill;
        public string CurrentSkillId => CurrentSkill?.SkillId;
        public string CurrentAnimationKey =>
            CurrentSkill?.AnimationKey;
        public bool IsGuarding =>
            _activeWeapon?.IsGuarding == true;
        public Transform AttackSource =>
            _activeWeapon?.AttackSource;

        public bool Bind(
            Transform p_attacker,
            Action<MeleeSkillDefinition> p_effectRequested,
            Action<MeleeSkillDefinition> p_hitConfirmed)
        {
            if (p_attacker == null ||
                p_effectRequested == null ||
                p_hitConfirmed == null)
            {
                return false;
            }

            UnbindCurrentWeapon();
            _attacker = p_attacker;
            _effectRequested = p_effectRequested;
            _hitConfirmed = p_hitConfirmed;
            return true;
        }

        public bool TryBindWeapon(
            MeleeWeapon p_weapon,
            float p_additionalDamage)
        {
            if (p_weapon == null ||
                !p_weapon.IsInitialized ||
                _attacker == null)
            {
                return false;
            }

            UnbindCurrentWeapon();

            MeleeWeaponUseContext useContext = new(
                _attacker,
                _attacker,
                p_additionalDamage);

            if (!p_weapon.BindUseContext(useContext))
                return false;

            _activeWeapon = p_weapon;
            _activeWeapon.OnSkillEffectRequested +=
                HandleSkillEffectRequested;
            _activeWeapon.OnSkillHitConfirmed +=
                HandleSkillHitConfirmed;
            return true;
        }

        public void UnbindCurrentWeapon()
        {
            if (_activeWeapon != null)
            {
                _activeWeapon.OnSkillEffectRequested -=
                    HandleSkillEffectRequested;
                _activeWeapon.OnSkillHitConfirmed -=
                    HandleSkillHitConfirmed;
                _activeWeapon.UnbindUseContext();
            }

            _activeWeapon = null;
        }

        public MeleeSkillDefinition GetSkillDefinition(int p_skillIndex)
        {
            return _activeWeapon?.GetSkillDefinition(p_skillIndex);
        }

        private void HandleSkillEffectRequested(
            MeleeSkillDefinition p_skill)
        {
            _effectRequested?.Invoke(p_skill);
        }

        private void HandleSkillHitConfirmed(
            MeleeSkillDefinition p_skill)
        {
            _hitConfirmed?.Invoke(p_skill);
        }
    }
}
