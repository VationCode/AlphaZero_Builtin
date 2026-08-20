using UnityEngine;

namespace Alpha.Enemy.CrabBoss.Combat
{
    public class CrabBossCombatModule : MonoBehaviour
    {
        [Header("Attack Strategies")]
        [SerializeField] private BossMeleeAttackStrategy _meleeAttack = new();
        [SerializeField] private CrabBossRangeAttackStrategy _rangeAttack = new();
        [SerializeField] private CrabBossRushAttackStrategy _rushAttack = new();
        [SerializeField] private CrabBossAreaAttackStrategy _areaAttack = new();
        [SerializeField] private CrabBossArenaAttackStrategy _arenaAttack = new();

        private BossAttackStrategy _currentStrategy;

        public EAttackPattern CurrentPattern =>
            _currentStrategy?.Pattern ?? EAttackPattern.None;
        public bool IsAttacking => _currentStrategy != null;
        public bool IsAttackComplete =>
            _currentStrategy != null && _currentStrategy.IsComplete;

        public bool BeginAttack(
            EAttackPattern p_pattern,
            CrabBossContext p_context,
            CrabBossLocomotionModule p_locomotion)
        {
            CancelAttack();

            BossAttackStrategy strategy = GetStrategy(p_pattern);

            if (strategy == null ||
                !strategy.Begin(
                    p_context,
                    p_locomotion))
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

        public void CompleteAttack()
        {
            _currentStrategy?.Complete();
        }

        public void CancelAttack()
        {
            _currentStrategy?.Cancel();
            _currentStrategy = null;
        }

        private BossAttackStrategy GetStrategy(
            EAttackPattern p_pattern)
        {
            return p_pattern switch
            {
                EAttackPattern.MeleeAttack => _meleeAttack,
                EAttackPattern.RangeAttack => _rangeAttack,
                EAttackPattern.RushAttack => _rushAttack,
                EAttackPattern.AreaAttack => _areaAttack,
                EAttackPattern.ArenaAttack => _arenaAttack,
                _ => null
            };
        }
    }
}
