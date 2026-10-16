using Alpha.Combat;
using Alpha.Living;
using UnityEngine;

namespace Alpha.Enemy.Boss
{
    // 보스의 기능을 연결하고 외부 피해·사망 이벤트를 전투 흐름에 전달한다.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CrabBossCore : MonoBehaviour
    {
        [SerializeField] private BossStateFlow _stateFlow;
        [SerializeField] private BossLocomotionModule _locomotion;
        [SerializeField] private TargetDetectionModule _targetDetection;
        [SerializeField] private CrabBossAttackSelectionModule _attackSelection;
        [SerializeField] private CrabBossAttackModule _attack;
        [SerializeField] private CrabBossAnimationView _animation;
        [SerializeField] private CrabBossAttackAreaView _attackAreaView;
        [SerializeField] private CrabBossAttackEffectView _attackEffectView;
        [SerializeField] private CrabBossCollisionView _collisionView;
        [SerializeField] private Animator _animator;
        [SerializeField] private LivingModule _living;
        [SerializeField] private DamageReceiverModule _damageReceiver;

        [Tooltip("비어 있으면 보스 루트의 정면을 Attack1 판정 기준으로 사용한다.")]
        [SerializeField] private Transform _attack1Origin;

        private readonly HealthContext _healthContext = new();
        private bool _bound;

        private void Awake()
        {
            _stateFlow ??= GetComponentInChildren<BossStateFlow>(true);
            _locomotion ??= GetComponentInChildren<BossLocomotionModule>(true);
            _targetDetection ??= GetComponentInChildren<TargetDetectionModule>(true);
            _attackSelection ??= GetComponentInChildren<CrabBossAttackSelectionModule>(true);
            _attack ??= GetComponentInChildren<CrabBossAttackModule>(true);
            _animation ??= GetComponentInChildren<CrabBossAnimationView>(true);
            _attackAreaView ??= GetComponentInChildren<CrabBossAttackAreaView>(true);
            _attackEffectView ??= GetComponentInChildren<CrabBossAttackEffectView>(true);
            _collisionView ??= GetComponentInChildren<CrabBossCollisionView>(true);
            _animator ??= GetComponentInChildren<Animator>(true);
            _living ??= GetComponentInChildren<LivingModule>(true);
            _damageReceiver ??= GetComponent<DamageReceiverModule>();

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null || _stateFlow == null || _locomotion == null ||
                _targetDetection == null || _attackSelection == null ||
                _attack == null || _animation == null ||
                _attackAreaView == null || _attackEffectView == null ||
                _collisionView == null || !_collisionView.HasBodyColliders ||
                _animator == null || _living == null ||
                _damageReceiver == null)
            {
                Debug.LogError("CrabBossCore: 보스 전투 구성 요소가 누락되었습니다.", this);
                enabled = false;
                return;
            }

            _locomotion.Bind(body);
            _targetDetection.Bind(gameObject);
            _attackSelection.Bind(transform, _locomotion);
            _attack.Bind(transform, _attack1Origin != null ? _attack1Origin : transform);
            _animation.Bind(_animator);
            _attackEffectView.Bind(_animation, _attack, _targetDetection, transform);
            _collisionView.SetContactBlocking(true);

            _living.Bind(_healthContext);
            _damageReceiver.Bind(transform, _living.TryDecreaseHealth);
            _living.OnDeath += HandleDeath;

            _stateFlow.Bind(transform, _targetDetection, _locomotion,
                _attackSelection, _attack, _animation,
                _attackAreaView, _attackEffectView, _collisionView);
            _bound = true;
        }

        private void HandleDeath()
        {
            _stateFlow?.HandleDeath();
        }

        private void OnDestroy()
        {
            if (!_bound)
                return;

            _stateFlow.Unbind();
            _attackEffectView.Unbind();
            _living.OnDeath -= HandleDeath;
            _damageReceiver.Unbind();
        }
    }
}
