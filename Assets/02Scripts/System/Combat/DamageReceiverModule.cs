using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Combat
{
    // 공용 피해 검증 후 Entity가 제공한 체력 감소 기능으로 전달한다.
    [DisallowMultipleComponent]
    public sealed class DamageReceiverModule : MonoBehaviour, IDamageable
    {
        [Tooltip("Impact 무적을 사용하지 않는 Entity의 기본 재피격 간격입니다. Player는 Impact 값을 사용합니다.")]
        [SerializeField, Min(0f)]
        private float _minTimeBetweenDamaged = 0.1f;

        private readonly Dictionary<int, int> _lastReceivedAttackIds = new();
        private readonly HashSet<object> _invulnerabilityOwners = new();

        private Transform _owner;
        private Func<float, bool> _tryDecreaseHealth;
        private float _lastDamagedTime = float.NegativeInfinity;
        private float _impactInvulnerabilityEndTime = float.NegativeInfinity;
        private bool _useImpactInvulnerability;

        public bool IsBound =>
            _owner != null &&
            _tryDecreaseHealth != null;

        public bool IsExternallyInvulnerable =>
            _invulnerabilityOwners.Count > 0;

        public event Action<DamageInfo> OnDamaged;

        // Core가 Entity 경계와 실제 체력 감소 기능을 연결한다.
        public void Bind(
            Transform p_owner,
            Func<float, bool> p_tryDecreaseHealth,
            bool p_useImpactInvulnerability = false)
        {
            _owner = p_owner != null
                ? p_owner
                : throw new ArgumentNullException(nameof(p_owner));
            _tryDecreaseHealth = p_tryDecreaseHealth ??
                throw new ArgumentNullException(nameof(p_tryDecreaseHealth));
            _useImpactInvulnerability = p_useImpactInvulnerability;

            ResetSession();
        }

        public void Unbind()
        {
            _owner = null;
            _tryDecreaseHealth = null;
            _useImpactInvulnerability = false;
            _invulnerabilityOwners.Clear();
            ResetSession();
        }

        public bool BeginInvulnerability(object p_owner)
        {
            return p_owner != null &&
                   _invulnerabilityOwners.Add(p_owner);
        }

        public bool EndInvulnerability(object p_owner)
        {
            return p_owner != null &&
                   _invulnerabilityOwners.Remove(p_owner);
        }

        // 직접 공격과 Trigger 공격이 공유하는 피해 수신 진입점이다.
        public bool TryApplyDamage(DamageInfo p_damageInfo)
        {
            if (!IsBound ||
                !p_damageInfo.IsValid ||
                IsInvulnerable ||
                IsSelfAttack(p_damageInfo.Attacker) ||
                !_tryDecreaseHealth(p_damageInfo.Amount))
            {
                return false;
            }

            _lastDamagedTime = Time.time;
            if (_useImpactInvulnerability)
            {
                _impactInvulnerabilityEndTime = _lastDamagedTime +
                    p_damageInfo.Impact.DamageInvulnerabilityDuration;
            }
            OnDamaged?.Invoke(p_damageInfo);
            return true;
        }

        // Trigger에 진입한 활성 공격에서 피해 정보를 생성한다.
        private void OnTriggerEnter(Collider p_other)
        {
            if (!IsBound)
                return;

            IDamageSource damageSource =
                p_other.GetComponentInParent<IDamageSource>();

            if (damageSource == null ||
                damageSource.AttackId <= 0 ||
                IsDuplicateAttack(damageSource))
            {
                return;
            }

            if (!damageSource.TryCreateDamageInfo(
                    _owner,
                    out DamageInfo damageInfo) ||
                !TryApplyDamage(damageInfo))
            {
                return;
            }

            _lastReceivedAttackIds[damageSource.SourceId] =
                damageSource.AttackId;
        }

        private bool IsInvulnerable =>
            IsExternallyInvulnerable ||
            Time.time < (_useImpactInvulnerability
                ? _impactInvulnerabilityEndTime
                : _lastDamagedTime + _minTimeBetweenDamaged);

        private bool IsDuplicateAttack(IDamageSource p_damageSource)
        {
            return _lastReceivedAttackIds.TryGetValue(
                       p_damageSource.SourceId,
                       out int lastAttackId) &&
                   lastAttackId == p_damageSource.AttackId;
        }

        private bool IsSelfAttack(Transform p_attacker)
        {
            return p_attacker == _owner ||
                   _owner.IsChildOf(p_attacker) ||
                   p_attacker.IsChildOf(_owner);
        }

        private void OnEnable()
        {
            ResetSession();
        }

        private void OnDisable()
        {
            ResetSession();
        }

        private void ResetSession()
        {
            _lastDamagedTime = float.NegativeInfinity;
            _impactInvulnerabilityEndTime = float.NegativeInfinity;
            _lastReceivedAttackIds.Clear();
        }
    }
}
