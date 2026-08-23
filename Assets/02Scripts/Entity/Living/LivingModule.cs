using System;
using UnityEngine;

namespace Alpha.Living
{
    // Living Entity가 공유하는 체력 증감과 사망 규칙만 실행한다.
    public class LivingModule : MonoBehaviour
    {
        [SerializeField, Min(1f)]
        private float _startHealth = 100f;

        protected HealthContext HealthContext { get; private set; }

        public float CurrentHealth => HealthContext?.CurrentHealth ?? 0f;
        public bool IsDead => HealthContext?.IsDead ?? false;
        public bool IsBound => HealthContext != null;

        public event Action OnDeath;

        // Entity Core가 소유한 Context를 연결하고 최초 체력을 설정한다.
        public void Bind(HealthContext p_healthContext)
        {
            HealthContext = p_healthContext ??
                throw new ArgumentNullException(nameof(p_healthContext));

            ResetHealth();
        }

        // 원인과 무관하게 체력을 감소시키고 0이 되면 사망 상태로 전환한다.
        public virtual bool TryDecreaseHealth(float p_amount)
        {
            if (!IsBound ||
                IsDead ||
                p_amount <= 0f)
            {
                return false;
            }

            HealthContext.SetCurrentHealth(CurrentHealth - p_amount);

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
    }
}
