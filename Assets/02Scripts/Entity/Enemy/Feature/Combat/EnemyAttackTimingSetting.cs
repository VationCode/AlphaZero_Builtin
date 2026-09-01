using System;
using UnityEngine;

namespace Alpha.Enemy
{
    // 공격 애니메이션 진행률에 맞춰 한 번 실행할 기능과 활성 구간을 보관한다.
    [Serializable]
    public sealed class EnemyAttackTimingSetting
    {
        [SerializeField]
        private EEnemyAttackTimingType _eventType;

        [Tooltip("공격 애니메이션에서 기능을 시작할 진행률입니다. 0은 시작, 1은 종료입니다.")]
        [SerializeField, Range(0f, 1f)]
        private float _startNormalizedTime = 0.5f;

        [Tooltip("Collider 이벤트가 제어할 공격 Trigger Collider입니다.")]
        [SerializeField]
        private Collider _attackCollider;

        [Tooltip("공격 Collider를 다시 비활성화할 애니메이션 진행률입니다.")]
        [SerializeField, Range(0f, 1f)]
        private float _endNormalizedTime = 0.65f;

        public EEnemyAttackTimingType EventType => _eventType;
        public float StartNormalizedTime => _startNormalizedTime;
        public Collider AttackCollider => _attackCollider;
        public float EndNormalizedTime => _endNormalizedTime;

        public bool IsExecutable(EEnemyAttackType p_attackType)
        {
            return _eventType switch
            {
                EEnemyAttackTimingType.Projectile =>
                    p_attackType == EEnemyAttackType.Range,

                EEnemyAttackTimingType.Collider =>
                    _attackCollider != null &&
                    _endNormalizedTime > _startNormalizedTime,

                _ => false
            };
        }

        public void Validate()
        {
            _startNormalizedTime = Mathf.Clamp01(
                _startNormalizedTime);
            _endNormalizedTime = Mathf.Clamp(
                _endNormalizedTime,
                _startNormalizedTime,
                1f);
        }
    }
}
