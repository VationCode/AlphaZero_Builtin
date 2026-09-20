using Alpha.Combat;
using Alpha.Living;
using Alpha.Player;
using UnityEngine;

namespace Alpha.Boss
{
    // Boss의 체력·피격·애니메이션과 연출 상태를 연결하는 대표 진입점이다.
    [DisallowMultipleComponent]
    public sealed class BossCore : MonoBehaviour
    {
        [SerializeField]
        private LivingModule _healthModule;

        [SerializeField]
        private DamageReceiverModule _damageReceiver;

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private BossAnimationView _animationView;

        [SerializeField] private BossAttackEffectView _attackEffectView;

        [SerializeField] private BossCombatFlow _combatFlow;
        [SerializeField] private BossActionFlow _actionFlow;
        [SerializeField] private BossLocomotionModule _locomotionModule;
        private readonly BossCombatModule _combatModule = new();

        public HealthContext HealthContext { get; } = new();
        public BossEncounterContext EncounterContext { get; } = new();
        public BossCombatContext CombatContext { get; } = new();
        public Transform Target { get; private set; }

        public LivingModule HealthModule => _healthModule;
        public DamageReceiverModule DamageReceiver => _damageReceiver;
        public Rigidbody Rigidbody => _rigidbody;
        public BossAnimationView AnimationView => _animationView;

        private void Awake()
        {
            ResolveFeatures();

            if (_healthModule != null)
            {
                _healthModule.OnDeath -= HandleDeath;
                _healthModule.OnDeath += HandleDeath;
                _healthModule.Bind(HealthContext);
            }

            if (_damageReceiver != null && _healthModule != null)
            {
                _damageReceiver.Bind(
                    transform,
                    _healthModule.TryDecreaseHealth);
            }

            _animationView?.Bind(_rigidbody);
            if (_animationView != null)
            {
                EncounterContext.OnStateChanged += _animationView.SetEncounterState;
                _animationView.SetEncounterState(EncounterContext.CurrentState);
            }
            _combatFlow.Bind(this, _combatModule, CombatContext,
                GetComponentsInChildren<BossPatternGroup>(true));
            _locomotionModule.Bind(_rigidbody);
            _actionFlow.Bind(this, _combatFlow, _locomotionModule);
            _attackEffectView.Bind(_combatFlow, transform);
            if (_animationView != null)
            {
                _animationView.OnAttackProgress += _combatFlow.NotifyAnimationProgress;
                _animationView.OnAttackCompleted += _combatFlow.NotifyAnimationCompleted;
                _animationView.OnAttackInterrupted += _combatFlow.NotifyAnimationInterrupted;
            }
        }

        // 외부 패턴 선택과 Inspector는 이 진입점으로만 공격을 요청한다.
        public bool TryStartAttack(BossPatternData p_pattern) =>
            _combatFlow != null && _combatFlow.TryStartAttack(p_pattern);

        // 거리 조건을 통과한 같은 공격 종류의 패턴 중 하나를 선택하여 실행한다.
        public bool TryStartRandomAttack(EBossPatternGroupType p_groupType, EBossAttackType p_attackType) =>
            _combatFlow != null && _combatFlow.TryStartRandomAttack(p_groupType, p_attackType);

        public void CancelAttack()
        {
            _actionFlow?.CancelPendingAction();
            _combatFlow?.CancelAttack();
        }

        public void SetAutomaticAttacksEnabled(bool p_enabled) =>
            _actionFlow?.SetAutomaticAttacksEnabled(p_enabled);

        // Installer는 Boss가 추적할 Player만 Entity 경계로 전달한다.
        public void Bind(PlayerCore p_player)
        {
            SetTarget(p_player != null ? p_player.transform : null);
        }

        public void SetTarget(Transform p_target)
        {
            if (Target != p_target)
                _actionFlow?.CancelPendingAction();
            Target = p_target;
        }

        public void ClearTarget()
        {
            SetTarget(null);
        }

        private void ResolveFeatures()
        {
            _healthModule ??=
                GetComponentInChildren<LivingModule>(true);
            _damageReceiver ??=
                GetComponentInChildren<DamageReceiverModule>(true);
            _rigidbody ??= GetComponent<Rigidbody>();
            _animationView ??=
                GetComponentInChildren<BossAnimationView>(true);
            _combatFlow ??= GetComponentInChildren<BossCombatFlow>(true);
            if (_combatFlow == null)
                _combatFlow = gameObject.AddComponent<BossCombatFlow>();
            _actionFlow ??= GetComponentInChildren<BossActionFlow>(true);
            if (_actionFlow == null)
                _actionFlow = gameObject.AddComponent<BossActionFlow>();
            _locomotionModule ??= GetComponentInChildren<BossLocomotionModule>(true);
            if (_locomotionModule == null)
                _locomotionModule = gameObject.AddComponent<BossLocomotionModule>();
            _attackEffectView ??= GetComponentInChildren<BossAttackEffectView>(true);
            if (_attackEffectView == null)
                _attackEffectView = gameObject.AddComponent<BossAttackEffectView>();
        }

        private void HandleDeath()
        {
            CancelAttack();
            _animationView?.PlayDeath();
        }

        private void OnDestroy()
        {
            if (_animationView != null)
                EncounterContext.OnStateChanged -= _animationView.SetEncounterState;
            CancelAttack();
            _actionFlow?.Unbind();
            _attackEffectView?.Unbind();
            if (_animationView != null && _combatFlow != null)
            {
                _animationView.OnAttackProgress -= _combatFlow.NotifyAnimationProgress;
                _animationView.OnAttackCompleted -= _combatFlow.NotifyAnimationCompleted;
                _animationView.OnAttackInterrupted -= _combatFlow.NotifyAnimationInterrupted;
            }
            _animationView?.Unbind();
            _damageReceiver?.Unbind();

            if (_healthModule != null)
                _healthModule.OnDeath -= HandleDeath;
        }

        private void OnDisable() => CancelAttack();
    }
}
