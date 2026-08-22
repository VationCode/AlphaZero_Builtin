using Alpha.Combat;
using Alpha.Living;
using Alpha.Enemy.Audio;
using Alpha.Enemy.Animation;
using UnityEngine;

namespace Alpha.Enemy
{
    public class EnemyCore : MonoBehaviour, IDamageable, IKnockbackable
    {
        [SerializeField]
        private EnemyHealthModule _healthModule;

        [SerializeField]
        private EnemyLocomotionModule _locomotionModule;

        [SerializeField]
        private EnemyDetectionModule _targetModule;

        [SerializeField]
        private EnemyCombatModule _combatModule;

        [SerializeField]
        private EnemyAttackFlow _attackFlow;

        [SerializeField]
        private EnemyActionFlow _actionFlow;

        [SerializeField]
        private EnemyAnimationView _animationView;

        [SerializeField]
        private EnemyAudioView _audioView;

        public HealthContext HealthContext { get; } = new();
        public Transform Target { get; private set; }

        public EnemyHealthModule HealthModule => _healthModule;
        public EnemyLocomotionModule LocomotionModule => _locomotionModule;
        public EnemyDetectionModule TargetModule => _targetModule;
        public EnemyCombatModule CombatModule => _combatModule;
        public EnemyAttackFlow AttackFlow => _attackFlow;
        public EnemyActionFlow ActionFlow => _actionFlow;
        public EnemyAnimationView AnimationView => _animationView;
        public EnemyAudioView AudioView => _audioView;

        public bool CanReceiveKnockback =>
            _healthModule != null &&
            _healthModule.IsBound &&
            !_healthModule.IsDead &&
            _locomotionModule != null &&
            _locomotionModule.CanApplyKnockback;

        private void Awake()
        {
            _healthModule ??= GetComponentInChildren<EnemyHealthModule>(true);
            _locomotionModule ??= GetComponentInChildren<EnemyLocomotionModule>(true);
            _targetModule ??= GetComponentInChildren<EnemyDetectionModule>(true);
            _combatModule ??= GetComponentInChildren<EnemyCombatModule>(true);
            _attackFlow ??= GetComponentInChildren<EnemyAttackFlow>(true);
            _actionFlow ??= GetComponentInChildren<EnemyActionFlow>(true);
            _animationView ??= GetComponentInChildren<EnemyAnimationView>(true);
            _audioView ??= GetComponentInChildren<EnemyAudioView>(true);

            if (_healthModule != null)
            {
                _healthModule.Bind(HealthContext);
                _healthModule.OnDamaged += HandleDamaged;
                _healthModule.OnDeath += HandleDeath;
            }

            _locomotionModule?.Bind(transform);
            _targetModule?.Bind(transform);
            _combatModule?.Bind(transform);
            _attackFlow?.Bind(this);
            _audioView?.Bind(
                _actionFlow,
                _attackFlow,
                _healthModule);
            _actionFlow?.Bind(this);
        }

        // Enemy 루트의 피격 진입점을 실제 체력 Module로 전달한다.
        public bool TryApplyDamage(DamageInfo p_damageInfo)
        {
            return _healthModule != null &&
                   _healthModule.TryApplyDamage(p_damageInfo);
        }

        // 공용 넉백 요청을 Enemy 이동 Module로 전달한다.
        public bool TryApplyKnockback(in KnockbackInfo p_knockbackInfo)
        {
            return CanReceiveKnockback &&
                   _locomotionModule.TryApplyKnockback(p_knockbackInfo);
        }

        // Core는 공용 피해 이벤트를 Enemy 행동 Flow로 연결만 한다.
        private void HandleDamaged(DamageInfo p_damageInfo)
        {
            _actionFlow?.HandleDamaged(p_damageInfo);
        }

        private void HandleDeath()
        {
            _locomotionModule?.CancelKnockback();
            _actionFlow?.HandleDeath();
        }

        private void OnDestroy()
        {
            _audioView?.Unbind();

            if (_healthModule != null)
            {
                _healthModule.OnDamaged -= HandleDamaged;
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
