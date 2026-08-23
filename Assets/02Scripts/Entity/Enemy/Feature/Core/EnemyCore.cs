using Alpha.Combat;
using Alpha.Living;
using Alpha.Enemy.Audio;
using Alpha.Enemy.Animation;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Enemy
{
    public class EnemyCore : MonoBehaviour
    {
        [SerializeField]
        private EnemyHealthModule _healthModule;

        [SerializeField]
        private DamageReceiverModule _damageReceiver;

        [SerializeField]
        private EnemyLocomotionModule _locomotionModule;

        [SerializeField]
        private EnemyDetectionModule _targetModule;

        [SerializeField]
        private EnemyCombatModule _combatModule;

        [FormerlySerializedAs("_attackFlow")]
        [SerializeField]
        private EnemyCombatFlow _combatFlow;

        [SerializeField]
        private EnemyActionFlow _actionFlow;

        [SerializeField]
        private EnemyAnimationView _animationView;

        [SerializeField]
        private EnemyAudioView _audioView;

        public HealthContext HealthContext { get; } = new();
        public EnemyLocomotionFlow LocomotionFlow { get; } = new();
        public EnemyTargetingFlow TargetingFlow { get; } = new();
        public Transform Target { get; private set; }

        public EnemyHealthModule HealthModule => _healthModule;
        public DamageReceiverModule DamageReceiver => _damageReceiver;
        public EnemyLocomotionModule LocomotionModule => _locomotionModule;
        public EnemyDetectionModule TargetModule => _targetModule;
        public EnemyCombatModule CombatModule => _combatModule;
        public EnemyCombatFlow CombatFlow => _combatFlow;
        public EnemyActionFlow ActionFlow => _actionFlow;
        public EnemyAudioView AudioView => _audioView;

        private void Awake()
        {
            _healthModule ??= GetComponentInChildren<EnemyHealthModule>(true);
            _damageReceiver ??= GetComponentInChildren<DamageReceiverModule>(true);
            _locomotionModule ??= GetComponentInChildren<EnemyLocomotionModule>(true);
            _targetModule ??= GetComponentInChildren<EnemyDetectionModule>(true);
            _combatModule ??= GetComponentInChildren<EnemyCombatModule>(true);
            _combatFlow ??= GetComponentInChildren<EnemyCombatFlow>(true);
            _actionFlow ??= GetComponentInChildren<EnemyActionFlow>(true);
            _animationView ??= GetComponentInChildren<EnemyAnimationView>(true);
            _audioView ??= GetComponentInChildren<EnemyAudioView>(true);

            if (_healthModule != null)
            {
                _healthModule.Bind(HealthContext);
                _healthModule.OnDeath += HandleDeath;
            }

            if (_damageReceiver != null && _healthModule != null)
            {
                _damageReceiver.Bind(
                    transform,
                    _healthModule.TryDecreaseHealth);
                _damageReceiver.OnDamaged += HandleDamaged;
            }

            _locomotionModule?.Bind(transform);
            _animationView?.Bind(
                _actionFlow,
                LocomotionFlow,
                _combatFlow);
            LocomotionFlow.Bind(this);
            _targetModule?.Bind(transform);
            _combatModule?.Bind(transform);
            _combatFlow?.Bind(this);
            TargetingFlow.Bind(this);
            _audioView?.Bind(
                _actionFlow,
                _combatFlow,
                _damageReceiver,
                _healthModule);
            _actionFlow?.Bind(this);
        }

        // Core는 공용 피해 이벤트를 Enemy 행동 Flow로 연결만 한다.
        private void HandleDamaged(DamageInfo p_damageInfo)
        {
            _actionFlow?.HandleDamaged(p_damageInfo);
        }

        private void HandleDeath()
        {
            _locomotionModule?.SetKnockbackEnabled(false);
            _actionFlow?.HandleDeath();
        }

        private void OnDestroy()
        {
            _animationView?.Unbind();
            _audioView?.Unbind();

            if (_damageReceiver != null)
            {
                _damageReceiver.OnDamaged -= HandleDamaged;
                _damageReceiver.Unbind();
            }

            if (_healthModule != null)
            {
                _healthModule.OnDeath -= HandleDeath;
            }
        }

        internal void SetTarget(Transform p_target)
        {
            Target = p_target;
        }

        internal void ClearTarget()
        {
            Target = null;
        }
    }
}
