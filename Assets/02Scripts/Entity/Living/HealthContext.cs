using System;
using UnityEngine;

namespace Alpha.Living
{
    // Living Entity 한 개의 체력 상태를 저장하고 변경 사실을 전달한다.
    public sealed class HealthContext
    {
        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;

        // 상태 변경 규칙은 LivingModule이 결정하고 Context는 결과만 기록한다.
        internal void Initialize(float p_maxHealth)
        {
            MaxHealth = Mathf.Max(1f, p_maxHealth);
            CurrentHealth = MaxHealth;
            IsDead = false;

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        internal void SetCurrentHealth(float p_currentHealth)
        {
            float nextHealth = Mathf.Clamp(p_currentHealth, 0f, MaxHealth);
            if (Mathf.Approximately(CurrentHealth, nextHealth))
                return;

            CurrentHealth = nextHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        internal void SetDead()
        {
            if (IsDead)
                return;

            IsDead = true;
            OnDied?.Invoke();
        }
    }
}
