using Alpha.Combat;
using Alpha.Detection;
using Alpha.Living;
using Alpha.Enemy.Audio;
using Alpha.Enemy.Animation;
using Alpha.Enemy.Effect;
using Alpha.Living.Effect;
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

        [FormerlySerializedAs("_targetSearchModule")]
        [FormerlySerializedAs("_targetModule")]
        [SerializeField]
        private AreaDetectionModule _targetDetectionModule;

        [SerializeField, Min(0.05f)]
        private float _targetSearchInterval = 0.2f;

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

        [SerializeField]
        private EnemyAttackEffectView _attackEffectView;

        [SerializeField]
        private DamageEffectView _damageEffectView;

        public HealthContext HealthContext { get; } = new();
        public EnemyLocomotionFlow LocomotionFlow { get; } = new();
        public EnemyTargetingFlow TargetingFlow { get; } = new();
        public Transform Target { get; private set; }

        public EnemyHealthModule HealthModule => _healthModule;
        public DamageReceiverModule DamageReceiver => _damageReceiver;
        public EnemyLocomotionModule LocomotionModule => _locomotionModule;
        public AreaDetectionModule TargetDetectionModule =>
            _targetDetectionModule;
        public float TargetSearchInterval =>
            Mathf.Max(0.05f, _targetSearchInterval);
        public EnemyCombatModule CombatModule => _combatModule;
        public EnemyCombatFlow CombatFlow => _combatFlow;
        public EnemyActionFlow ActionFlow => _actionFlow;
        public EnemyAudioView AudioView => _audioView;
        public EnemyAttackEffectView AttackEffectView => _attackEffectView;
        public DamageEffectView DamageEffectView => _damageEffectView;

        private void Awake()
        {
            _healthModule ??= GetComponentInChildren<EnemyHealthModule>(true);
            _damageReceiver ??= GetComponentInChildren<DamageReceiverModule>(true);
            _locomotionModule ??= GetComponentInChildren<EnemyLocomotionModule>(true);
            _targetDetectionModule ??=
                GetComponentInChildren<AreaDetectionModule>(true);
            _combatModule ??= GetComponentInChildren<EnemyCombatModule>(true);
            _combatFlow ??= GetComponentInChildren<EnemyCombatFlow>(true);
            _actionFlow ??= GetComponentInChildren<EnemyActionFlow>(true);

            if (_actionFlow == null)
                _actionFlow = ResolveOrCreateActionFlow();

            _animationView ??= GetComponentInChildren<EnemyAnimationView>(true);
            _audioView ??= GetComponentInChildren<EnemyAudioView>(true);
            _attackEffectView ??=
                GetComponentInChildren<EnemyAttackEffectView>(true);
            _damageEffectView ??= GetComponentInChildren<DamageEffectView>(true);

            if (_animationView != null && _combatFlow != null)
            {
                _animationView.OnAttackAnimationElapsed -=
                    _combatFlow.NotifyAttackAnimationElapsed;
                _animationView.OnAttackAnimationElapsed +=
                    _combatFlow.NotifyAttackAnimationElapsed;
                _animationView.OnAttackAnimationCompleted -=
                    _combatFlow.NotifyAttackAnimationCompleted;
                _animationView.OnAttackAnimationCompleted +=
                    _combatFlow.NotifyAttackAnimationCompleted;
            }

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
            _targetDetectionModule?.Bind(transform);
            _combatModule?.Bind(transform);
            _combatFlow?.Bind(this);
            TargetingFlow.Bind(this);
            _audioView?.Bind(
                _actionFlow,
                _combatFlow,
                _damageReceiver,
                _healthModule);
            _attackEffectView?.Bind(
                _animationView,
                _combatFlow);
            _damageEffectView?.Bind(_damageReceiver);
            _actionFlow?.Bind(this);
        }

        private EnemyActionFlow ResolveOrCreateActionFlow()
        {
            Transform actionOwner = transform.Find("Action");

            if (actionOwner == null)
            {
                GameObject actionObject = new("Action");
                actionObject.layer = gameObject.layer;
                actionObject.transform.SetParent(transform, false);
                actionOwner = actionObject.transform;
            }

            return actionOwner.GetComponent<EnemyActionFlow>() ??
                   actionOwner.gameObject.AddComponent<EnemyActionFlow>();
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
            if (_animationView != null && _combatFlow != null)
            {
                _animationView.OnAttackAnimationElapsed -=
                    _combatFlow.NotifyAttackAnimationElapsed;
                _animationView.OnAttackAnimationCompleted -=
                    _combatFlow.NotifyAttackAnimationCompleted;
            }

            _animationView?.Unbind();
            _audioView?.Unbind();
            _attackEffectView?.Unbind();
            _damageEffectView?.Unbind();

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

        // Encounter는 Core 진입점을 통해서만 Player Target을 고정한다.
        public bool BeginTargetLock(
            object p_owner,
            Transform p_target)
        {
            return TargetingFlow.BeginTargetLock(
                p_owner,
                p_target);
        }

        public bool EndTargetLock(object p_owner)
        {
            return TargetingFlow.EndTargetLock(p_owner);
        }
    }
}
