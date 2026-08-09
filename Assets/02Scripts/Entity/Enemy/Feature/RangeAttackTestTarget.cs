using Alpha.Combat;
using UnityEngine;

namespace Alpha.Test.Combat
{
    // Range Attack의 명중과 Damage 전달만 확인하는 테스트 대상이다.
    public class RangeAttackTestTarget :
        MonoBehaviour,
        IDamageable
    {
        [SerializeField, Min(1f)]
        private float _maxHealth = 100f;

        [SerializeField]
        private float _currentHealth;

        public float CurrentHealth => _currentHealth;

        private void Awake()
        {
            ResetHealth();
        }

        public bool TryApplyDamage(
            in DamageInfo p_damageInfo)
        {
            if (!p_damageInfo.IsValid ||
                _currentHealth <= 0f)
            {
                return false;
            }

            _currentHealth = Mathf.Max(
                0f,
                _currentHealth - p_damageInfo.Amount);

            Debug.Log(
                $"[{name}] Damage: {p_damageInfo.Amount}, " +
                $"Health: {_currentHealth}/{_maxHealth}",
                this);

            if (_currentHealth <= 0f)
                Debug.Log($"[{name}] Test Target Dead", this);

            return true;
        }

        [ContextMenu("Reset Health")]
        private void ResetHealth()
        {
            _currentHealth = _maxHealth;
        }
    }
}