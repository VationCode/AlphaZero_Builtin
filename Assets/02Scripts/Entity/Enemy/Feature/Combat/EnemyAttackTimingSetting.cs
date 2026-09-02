using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Enemy
{
    // 공격 애니메이션 시작 후 경과 시간에 맞춰 실행할 기능과 활성 구간을 보관한다.
    [Serializable]
    public sealed class EnemyAttackTimingSetting
    {
        [SerializeField]
        private EEnemyAttackTimingType _eventType;

        [Tooltip("공격 애니메이션이 시작된 뒤 기능을 실행할 시간입니다. 단위는 초입니다.")]
        [FormerlySerializedAs("_startNormalizedTime")]
        [SerializeField, Min(0f)]
        private float _startTimeSeconds = 0.5f;

        [Tooltip("Collider 이벤트가 제어할 공격 Trigger Collider입니다.")]
        [SerializeField]
        private Collider _attackCollider;

        [Tooltip("공격 Collider를 다시 비활성화할 시간입니다. 단위는 초입니다.")]
        [FormerlySerializedAs("_endNormalizedTime")]
        [SerializeField, Min(0f)]
        private float _endTimeSeconds = 0.65f;

        public EEnemyAttackTimingType EventType => _eventType;
        public float StartTimeSeconds => _startTimeSeconds;
        public Collider AttackCollider => _attackCollider;
        public float EndTimeSeconds => _endTimeSeconds;

        public bool IsExecutable(EEnemyAttackType p_attackType)
        {
            return _eventType switch
            {
                EEnemyAttackTimingType.Projectile =>
                    p_attackType == EEnemyAttackType.Range,

                EEnemyAttackTimingType.Collider =>
                    _attackCollider != null &&
                    _endTimeSeconds > _startTimeSeconds,

                _ => false
            };
        }

        public void Validate()
        {
            _startTimeSeconds = Mathf.Max(
                0f,
                _startTimeSeconds);
            _endTimeSeconds = Mathf.Max(
                _startTimeSeconds,
                _endTimeSeconds);
        }
    }
}
