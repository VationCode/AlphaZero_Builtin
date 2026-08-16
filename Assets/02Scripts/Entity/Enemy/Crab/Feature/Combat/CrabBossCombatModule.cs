using UnityEngine;

namespace Alpha.Enemy.CrabBoss.Combat
{
    public class CrabBossCombatModule : MonoBehaviour
    {
        [Header("Combat Distance")]
        [SerializeField, Min(0f)]
        private float _meleeAttackDistance = 8f;

        [SerializeField, Min(0f)]
        private float _moveMaxDistance = 15f;

        [Header("Attack Strategies")]
        [SerializeField] private CrabBossMeleeAttackStrategy _meleeAttack = new();
        [SerializeField] private CrabBossRangeAttackStrategy _rangeAttack = new();
        [SerializeField] private CrabBossRushAttackStrategy _rushAttack = new();
        [SerializeField] private CrabBossAreaAttackStrategy _areaAttack = new();
        [SerializeField] private CrabBossArenaAttackStrategy _arenaAttack = new();

        private CrabBossAttackStrategy _currentStrategy;

        public float MeleeAttackDistance => _meleeAttackDistance;
        public float MoveMaxDistance => _moveMaxDistance;
        public ECrabAttackPattern CurrentPattern =>
            _currentStrategy?.Pattern ?? ECrabAttackPattern.None;
        public bool IsAttacking => _currentStrategy != null;
        public bool IsAttackComplete =>
            _currentStrategy != null && _currentStrategy.IsComplete;

        private void OnValidate()
        {
            _moveMaxDistance = Mathf.Max(
                _meleeAttackDistance,
                _moveMaxDistance);
        }

        public bool BeginAttack(
            ECrabAttackPattern p_pattern,
            CrabBossContext p_context,
            CrabBossLocomotionModule p_locomotion)
        {
            CancelAttack();

            CrabBossAttackStrategy strategy = GetStrategy(p_pattern);

            if (strategy == null ||
                !strategy.Begin(p_context, p_locomotion))
            {
                strategy?.Cancel();
                return false;
            }

            _currentStrategy = strategy;
            return true;
        }

        public void TickAttack(float p_deltaTime)
        {
            _currentStrategy?.Tick(p_deltaTime);
        }

        public void CancelAttack()
        {
            _currentStrategy?.Cancel();
            _currentStrategy = null;
        }

        private CrabBossAttackStrategy GetStrategy(
            ECrabAttackPattern p_pattern)
        {
            return p_pattern switch
            {
                ECrabAttackPattern.MeleeAttack => _meleeAttack,
                ECrabAttackPattern.RangeAttack => _rangeAttack,
                ECrabAttackPattern.RushAttack => _rushAttack,
                ECrabAttackPattern.AreaAttack => _areaAttack,
                ECrabAttackPattern.ArenaAttack => _arenaAttack,
                _ => null
            };
        }
    }
}
