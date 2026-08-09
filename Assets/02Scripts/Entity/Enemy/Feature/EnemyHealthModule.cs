using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Enemy.Health
{
    // 공용 피해 요청을 Enemy 체력 상태에 적용한다.
    public class EnemyHealthModule :
        MonoBehaviour,
        IDamageable
    {
        [SerializeField, Min(1f)]
        private float _maxHealth = 100f;

        public EnemyHealthState State { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageInfo> OnDied;

        private void Awake()
        {
            State = new EnemyHealthState(_maxHealth);
        }

        public bool TryApplyDamage(
            in DamageInfo p_damageInfo)
        {
            if (!p_damageInfo.IsValid ||
                State == null ||
                !State.TryTakeDamage(p_damageInfo.Amount))
            {
                return false;
            }

            OnHealthChanged?.Invoke(
                State.CurrentHealth,
                State.MaxHealth);

            if (State.IsDead)
                OnDied?.Invoke(p_damageInfo);

            return true;
        }
    }
}