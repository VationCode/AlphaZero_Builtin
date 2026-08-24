using Alpha.Combat;
using Alpha.Detection;
using Alpha.Item.Weapon.Melee;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // 선택된 Melee Skill 하나의 시간·공간 판정·피해 적용을 수행한다.
    [Serializable]
    public sealed class PlayerMeleeSkillModule
    {
        [Header("Detection")]
        [Tooltip("한 번의 Skill 공격 판정에서 임시로 저장할 최대 Collider 수입니다. 범위 안의 Collider가 이 값을 초과하면 일부 후보는 처리되지 않을 수 있습니다.")]
        [SerializeField, Min(1)]
        private int _hitBufferCapacity = 16;

        public MeleeSkillDefinition CurrentSkill { get; private set; }
        public float ElapsedTime { get; private set; }
        public Transform AttackSource => _attacker;

        private Transform _attacker;
        private Func<float, float> _damageResolver;
        private Action<MeleeSkillDefinition> _hitConfirmed;
        private MeleeWeapon _activeWeapon;
        private bool _didHitCurrentSkill;

        private Collider[] _overlapBuffer;
        private DetectionAreaHit[] _hitBuffer;
        private HashSet<IDamageable> _damagedTargets = new();

        public bool Bind(
            Transform p_attacker,
            Func<float, float> p_damageResolver,
            Action<MeleeSkillDefinition> p_hitConfirmed)
        {
            if (p_attacker == null ||
                p_damageResolver == null ||
                p_hitConfirmed == null)
            {
                return false;
            }

            _attacker = p_attacker;
            _damageResolver = p_damageResolver;
            _hitConfirmed = p_hitConfirmed;
            _damagedTargets ??= new HashSet<IDamageable>();
            EnsureHitBuffers();
            return true;
        }

        public bool TryBegin(
            MeleeWeapon p_weapon,
            MeleeSkillDefinition p_skill)
        {
            if (p_weapon == null ||
                p_skill == null ||
                !p_skill.IsValid ||
                p_weapon.BaseDamage <= 0f ||
                _attacker == null)
            {
                return false;
            }

            _activeWeapon = p_weapon;
            CurrentSkill = p_skill;
            ElapsedTime = 0f;
            _didHitCurrentSkill = false;
            return true;
        }

        public void Tick(float p_deltaTime)
        {
            if (_activeWeapon == null || CurrentSkill == null)
                return;

            ElapsedTime += Mathf.Max(0f, p_deltaTime);
            TryExecuteHit();
        }

        public void End()
        {
            _activeWeapon = null;
            CurrentSkill = null;
            ElapsedTime = 0f;
            _didHitCurrentSkill = false;
            _damagedTargets?.Clear();
        }

        private void TryExecuteHit()
        {
            if (_didHitCurrentSkill ||
                _activeWeapon == null ||
                CurrentSkill == null ||
                _attacker == null)
            {
                return;
            }

            MeleeSkillAttackSettings settings =
                CurrentSkill.AttackSettings;

            if (settings == null ||
                !settings.IsValid ||
                ElapsedTime < settings.HitTime)
            {
                return;
            }

            _didHitCurrentSkill = true;
            ApplyDamage(settings);
        }

        private void ApplyDamage(MeleeSkillAttackSettings p_settings)
        {
            float weaponDamage =
                _activeWeapon.BaseDamage * p_settings.DamageMultiplier;
            float damage = _damageResolver != null
                ? _damageResolver.Invoke(weaponDamage)
                : weaponDamage;

            if (damage <= 0f)
                return;

            EnsureHitBuffers();
            Physics.SyncTransforms();

            DetectionAreaRequest request = new(
                _attacker.position,
                _attacker.forward,
                _attacker.up,
                _attacker,
                p_settings.Area);

            int hitCount = DetectionAreaSystem.Query(
                request,
                _overlapBuffer,
                _hitBuffer);

            bool hasConfirmedHit = false;
            _damagedTargets ??= new HashSet<IDamageable>();
            _damagedTargets.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];

                if (!DamageSystem.TryGetDamageable(
                        hit.Collider,
                        out IDamageable damageable) ||
                    !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                Vector3 hitDirection = Vector3.ProjectOnPlane(
                    request.Forward,
                    Vector3.up);

                if (hitDirection.sqrMagnitude <= 0.0001f)
                    hitDirection = _attacker.forward;

                DamageInfo damageInfo = new(
                    _attacker,
                    damage,
                    hit.HitPoint,
                    -hit.Direction,
                    hitDirection,
                    p_impact: p_settings.Impact,
                    p_deliveryType: EDamageDeliveryType.Melee);

                if (DamageSystem.TryApply(hit.Collider, damageInfo))
                    hasConfirmedHit = true;
            }

            if (hasConfirmedHit)
                _hitConfirmed?.Invoke(CurrentSkill);
        }

        private void EnsureHitBuffers()
        {
            int capacity = Mathf.Max(1, _hitBufferCapacity);

            if (_overlapBuffer == null ||
                _overlapBuffer.Length != capacity)
            {
                _overlapBuffer = new Collider[capacity];
            }

            if (_hitBuffer == null ||
                _hitBuffer.Length != capacity)
            {
                _hitBuffer = new DetectionAreaHit[capacity];
            }
        }

        public void Validate()
        {
            _hitBufferCapacity = Mathf.Max(1, _hitBufferCapacity);
        }
    }
}
