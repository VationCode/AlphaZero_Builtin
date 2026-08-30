using Alpha.Combat;
using Alpha.Detection;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    // Skill이 정한 영역을 검사하고 Melee 피해를 실제로 적용한다.
    internal sealed class MeleeWeaponAttackModule
    {
        private MeleeWeaponContext _context;
        private float _baseDamage;
        private int _hitBufferCapacity;
        private Action<MeleeSkillDefinition> _hitConfirmed;

        private Collider[] _overlapBuffer;
        private DetectionAreaHit[] _hitBuffer;
        private readonly HashSet<IDamageable> _damagedTargets = new();

        public bool IsConfigured =>
            _context != null &&
            _baseDamage > 0f &&
            _hitBufferCapacity > 0;

        public bool Bind(
            MeleeWeaponContext p_context,
            float p_baseDamage,
            int p_hitBufferCapacity,
            Action<MeleeSkillDefinition> p_hitConfirmed)
        {
            if (p_context == null || p_baseDamage <= 0f)
                return false;

            _context = p_context;
            _baseDamage = Mathf.Max(0f, p_baseDamage);
            _hitBufferCapacity = Mathf.Max(1, p_hitBufferCapacity);
            _hitConfirmed = p_hitConfirmed;
            EnsureHitBuffers();
            return true;
        }

        public void Unbind()
        {
            _context = null;
            _baseDamage = 0f;
            _hitConfirmed = null;
            _damagedTargets.Clear();
        }

        public bool Execute(MeleeSkillDefinition p_skill)
        {
            MeleeSkillAttackSettings settings = p_skill?.AttackSettings;
            Transform attacker = _context?.Attacker;
            Transform attackSource = _context?.AttackSource;

            if (!IsConfigured ||
                _context?.HasUser != true ||
                settings == null ||
                !settings.IsValid ||
                attacker == null ||
                attackSource == null)
            {
                return false;
            }

            float damage = Mathf.Max(
                0f,
                _baseDamage * settings.DamageMultiplier) +
                _context.AdditionalDamage;

            if (damage <= 0f)
                return false;

            EnsureHitBuffers();
            Physics.SyncTransforms();

            DetectionAreaRequest request = new(
                attackSource.position,
                attackSource.forward,
                attackSource.up,
                attacker,
                settings.Area);

            int hitCount = DetectionAreaSystem.CollectHits(
                request,
                _overlapBuffer,
                _hitBuffer);

            bool hasConfirmedHit = false;
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
                    hitDirection = attackSource.forward;

                DamageInfo damageInfo = new(
                    attacker,
                    damage,
                    hit.HitPoint,
                    -hit.Direction,
                    hitDirection,
                    p_impact: settings.Impact,
                    p_deliveryType: EDamageDeliveryType.Melee);

                if (DamageSystem.TryApply(hit.Collider, damageInfo))
                    hasConfirmedHit = true;
            }

            if (hasConfirmedHit)
                _hitConfirmed?.Invoke(p_skill);

            return true;
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
    }
}
