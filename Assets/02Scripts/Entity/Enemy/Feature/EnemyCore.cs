using Alpha.Living;
using Alpha.Enemy.Animation;
using UnityEngine;

namespace Alpha.Enemy
{
    public class EnemyCore : MonoBehaviour
    {
        [SerializeField]
        private EnemyHealthModule _healthModule;

        [SerializeField]
        private EnemyLocomotionModule _locomotionModule;

        [SerializeField]
        private EnemyTargetModule _targetModule;

        [SerializeField]
        private EnemyCombatModule _combatModule;

        [SerializeField]
        private EnemyAttackFlow _attackFlow;

        [SerializeField]
        private EnemyActionFlow _actionFlow;

        [SerializeField]
        private EnemyAnimationView _animationView;

        public HealthContext HealthContext { get; } = new();
        public Transform Target { get; private set; }

        public EnemyHealthModule HealthModule => _healthModule;
        public EnemyLocomotionModule LocomotionModule => _locomotionModule;
        public EnemyTargetModule TargetModule => _targetModule;
        public EnemyCombatModule CombatModule => _combatModule;
        public EnemyAttackFlow AttackFlow => _attackFlow;
        public EnemyActionFlow ActionFlow => _actionFlow;
        public EnemyAnimationView AnimationView => _animationView;

        private void Awake()
        {
            _healthModule ??= GetComponentInChildren<EnemyHealthModule>(true);
            _locomotionModule ??= GetComponentInChildren<EnemyLocomotionModule>(true);
            _targetModule ??= GetComponentInChildren<EnemyTargetModule>(true);
            _combatModule ??= GetComponentInChildren<EnemyCombatModule>(true);
            _attackFlow ??= GetComponentInChildren<EnemyAttackFlow>(true);
            _actionFlow ??= GetComponentInChildren<EnemyActionFlow>(true);
            _animationView ??=
                GetComponentInChildren<EnemyAnimationView>(true);

            _healthModule?.Bind(HealthContext);
            _locomotionModule?.Bind(transform);
            _targetModule?.Bind(transform);
            _combatModule?.Bind(transform);
            _attackFlow?.Bind(this);
            _actionFlow?.Bind(this);
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
