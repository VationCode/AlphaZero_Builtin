using Alpha.Combat;
using System;
using UnityEngine;

namespace Alpha.Living
{
    // Living Entity가 공유하는 피해, 회복, 사망 규칙을 실행한다.
    public class LivingModule : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)]
        private float _startHealth = 100f;

        [SerializeField, Min(0f)]
        private float _minTimeBetweenDamaged = 0.1f;

        protected HealthContext HealthContext { get; private set; }

        public float CurrentHealth => HealthContext?.CurrentHealth ?? 0f;
        public bool IsDead => HealthContext?.IsDead ?? false;
        public bool IsBound => HealthContext != null;

        public event Action<DamageInfo> OnDamaged;
        public event Action OnDeath;

        // 피해를 받은 직후 일정 시간 동안 중복 피해를 막는다.
        private float _lastDamagedTime = float.NegativeInfinity;

        protected bool IsInvulnerable =>
            Time.time < _lastDamagedTime + _minTimeBetweenDamaged;

        protected virtual void OnEnable()
        {
            _lastDamagedTime = float.NegativeInfinity;
        }

        // Entity Core가 소유한 Context를 연결하고 최초 체력을 설정한다.
        public void Bind(HealthContext p_healthContext)
        {
            HealthContext = p_healthContext ??
                throw new ArgumentNullException(nameof(p_healthContext));

            ResetHealth();
        }

        public virtual bool TryApplyDamage(DamageInfo p_damageInfo)
        {
            if (!IsBound ||
                !p_damageInfo.IsValid ||
                IsInvulnerable ||
                IsSelfAttack(p_damageInfo.Attacker) ||
                IsDead)
            {
                return false;
            }

            _lastDamagedTime = Time.time;
            HealthContext.SetCurrentHealth(CurrentHealth - p_damageInfo.Amount);

            // 피해를 실제로 적용한 경우에만 Entity별 피격 Flow에 전달한다.
            OnDamaged?.Invoke(p_damageInfo);

            Debug.Log(HealthContext.CurrentHealth);
            if (CurrentHealth <= 0f)
                Die();

            return true;
        }

        // Context가 최대 체력을 보장하므로 양수 회복량만 전달한다.
        public virtual void RestoreHealth(float p_amount)
        {
            if (!IsBound || IsDead || p_amount <= 0f)
                return;

            HealthContext.SetCurrentHealth(CurrentHealth + p_amount);
        }

        public virtual void ResetHealth()
        {
            if (!IsBound)
                return;

            _lastDamagedTime = float.NegativeInfinity;
            HealthContext.Initialize(_startHealth);
        }

        public virtual void Die()
        {
            if (!IsBound || IsDead)
                return;

            HealthContext.SetCurrentHealth(0f);
            HealthContext.SetDead();
            OnDeath?.Invoke();
        }

        private bool IsSelfAttack(Transform p_attacker)
        {
            return p_attacker == transform ||
                   transform.IsChildOf(p_attacker) ||
                   p_attacker.IsChildOf(transform);
        }
    }
}
