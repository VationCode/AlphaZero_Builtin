namespace Alpha.Enemy.Health
{
    // Enemy가 소유한 체력 상태를 보관한다.
    public sealed class EnemyHealthState
    {
        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth <= 0f;

        public EnemyHealthState(float p_maxHealth)
        {
            MaxHealth = p_maxHealth > 0f
                ? p_maxHealth
                : 1f;

            CurrentHealth = MaxHealth;
        }

        public bool TryTakeDamage(float p_damage)
        {
            if (p_damage <= 0f || IsDead)
                return false;

            CurrentHealth -= p_damage;

            if (CurrentHealth < 0f)
                CurrentHealth = 0f;

            return true;
        }
    }
}