using Alpha.Combat;
using System.Collections;
using UnityEngine;

namespace Alpha.Test.Combat
{
    // Range Attack의 명중과 Damage 전달만 확인하는 테스트 대상이다.
    public class RangeAttackTestTarget :
        MonoBehaviour,
        IDamageable,
        IKnockbackable
    {
        [SerializeField, Min(1f)]
        private float _maxHealth = 100f;

        [SerializeField]
        private float _currentHealth;

        [Header("Knockback")]
        [SerializeField]
        private bool _canReceiveKnockback = true;

        public float CurrentHealth => _currentHealth;
        public bool CanReceiveKnockback => _canReceiveKnockback;

        private Coroutine _knockbackRoutine;

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

        // Inspector 설정이 허용된 대상만 공격 방향으로 일정 시간 이동한다.
        public bool TryApplyKnockback(
            in KnockbackInfo p_knockbackInfo)
        {
            if (!_canReceiveKnockback ||
                !p_knockbackInfo.IsValid)
            {
                return false;
            }

            Vector3 direction = Vector3.ProjectOnPlane(
                p_knockbackInfo.Direction,
                Vector3.up);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.ProjectOnPlane(
                    transform.position -
                    p_knockbackInfo.Attacker.position,
                    Vector3.up);
            }

            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            if (_knockbackRoutine != null)
                StopCoroutine(_knockbackRoutine);

            _knockbackRoutine = StartCoroutine(
                ApplyKnockback(
                    direction.normalized,
                    p_knockbackInfo.Distance,
                    p_knockbackInfo.Duration));

            return true;
        }

        private IEnumerator ApplyKnockback(
            Vector3 p_direction,
            float p_distance,
            float p_duration)
        {
            Vector3 startPosition = transform.position;
            Vector3 targetPosition =
                startPosition + p_direction * p_distance;
            float elapsedTime = 0f;

            while (elapsedTime < p_duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(
                    elapsedTime / p_duration);

                transform.position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    normalizedTime);

                yield return null;
            }

            transform.position = targetPosition;
            _knockbackRoutine = null;
        }

        private void OnDisable()
        {
            if (_knockbackRoutine == null)
                return;

            StopCoroutine(_knockbackRoutine);
            _knockbackRoutine = null;
        }

        [ContextMenu("Reset Health")]
        private void ResetHealth()
        {
            _currentHealth = _maxHealth;
        }
    }
}
