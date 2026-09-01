using Alpha.Combat;
using UnityEngine;

namespace Alpha.Enemy
{
    // 활성 공격 Collider가 DamageReceiver에 현재 공격 정보를 제공한다.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackColliderSource :
        MonoBehaviour,
        IDamageSource
    {
        private Collider _attackCollider;
        private AttackSession _session;
        private bool _isActive;

        public int SourceId => GetInstanceID();
        public int AttackId => _isActive
            ? _session.AttackId
            : 0;

        public bool Activate(
            Collider p_attackCollider,
            in AttackSession p_session)
        {
            if (p_attackCollider == null ||
                p_attackCollider.gameObject != gameObject ||
                p_session.AttackId <= 0 ||
                p_session.Attacker == null ||
                p_session.Profile == null ||
                !p_session.Profile.IsValid)
            {
                Deactivate();
                return false;
            }

            _attackCollider = p_attackCollider;
            _session = p_session;
            _isActive = true;
            enabled = true;
            _attackCollider.enabled = true;
            return true;
        }

        public void Deactivate()
        {
            if (_attackCollider != null)
                _attackCollider.enabled = false;

            _attackCollider = null;
            _session = default;
            _isActive = false;
        }

        public bool TryCreateDamageInfo(
            Transform p_target,
            out DamageInfo p_damageInfo)
        {
            p_damageInfo = default;

            if (!_isActive ||
                p_target == null ||
                _session.Attacker == null ||
                _session.Profile == null ||
                !_session.Profile.IsValid)
            {
                return false;
            }

            Vector3 direction =
                p_target.position - _session.Attacker.position;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = _session.Attacker.forward;

            Vector3 normalizedDirection = direction.normalized;

            p_damageInfo = new DamageInfo(
                _session.Attacker,
                _session.Profile.Damage,
                p_target.position,
                -normalizedDirection,
                normalizedDirection,
                p_impact: _session.Profile.Impact,
                p_deliveryType: EDamageDeliveryType.Melee);

            return true;
        }

        private void OnDisable()
        {
            Deactivate();
        }
    }
}
