using System;
using UnityEngine;

namespace Alpha.Enemy
{
    // 사용 가능한 공격 패턴을 가중치로 선택하고 공격 실행 시점을 결정한다.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackFlow : MonoBehaviour
    {
        private EnemyCore _core;
        private int _preparedPatternIndex = -1;

        public event Action<EEnemyAttackType> OnAttackWaitStarted;
        public event Action<EEnemyAttackType> OnAttackStarted;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            CancelAttack();
        }

        public bool CanEnterAttack(Transform p_target)
        {
            return _core != null &&
                   _core.CombatModule != null &&
                   _core.CombatModule.CanEngageTarget(p_target);
        }

        public void TickAttack(
            Transform p_target,
            bool p_isFacingTarget,
            float p_deltaTime)
        {
            EnemyCombatModule combat =
                _core != null ? _core.CombatModule : null;

            if (combat == null || p_target == null)
                return;

            if (combat.IsAttacking)
            {
                bool isCompleted = combat.TickAttack(
                    p_target,
                    _core.LocomotionModule,
                    p_deltaTime);

                if (isCompleted)
                {
                    ClearPreparedPattern();
                    TryPreparePattern(
                        combat,
                        p_target);
                }

                return;
            }

            if (!TryPreparePattern(
                    combat,
                    p_target) ||
                !p_isFacingTarget ||
                !combat.CanStartPattern(
                    _preparedPatternIndex,
                    p_target))
            {
                return;
            }

            if (!combat.TryBeginAttack(
                    _preparedPatternIndex,
                    p_target,
                    out EnemyAttackPatternSetting pattern))
            {
                return;
            }

            _core.AnimationView?.PlayAttack(pattern.AttackType);
            OnAttackStarted?.Invoke(pattern.AttackType);

            combat.TickAttack(
                p_target,
                _core.LocomotionModule,
                p_deltaTime);
        }

        public void CancelAttack()
        {
            ClearPreparedPattern();

            if (_core == null || _core.CombatModule == null)
                return;

            _core.CombatModule.CancelAttack(
                _core.LocomotionModule);
        }

        // 유효한 패턴을 예약하고 타입에 맞는 공격 대기 애니메이션을 재생한다.
        private bool TryPreparePattern(
            EnemyCombatModule p_combat,
            Transform p_target)
        {
            if (_preparedPatternIndex >= 0)
            {
                if (p_combat.CanStartPattern(
                        _preparedPatternIndex,
                        p_target))
                {
                    return true;
                }

                // 대기 중 다른 패턴이 먼저 준비되면 해당 패턴으로 교체한다.
                if (TrySelectReadyPattern(
                        p_combat,
                        p_target,
                        out int readyPatternIndex))
                {
                    return PreparePattern(
                        p_combat,
                        readyPatternIndex);
                }

                if (p_combat.CanPreparePattern(
                        _preparedPatternIndex,
                        p_target))
                {
                    return true;
                }
            }

            ClearPreparedPattern();

            if (!TrySelectPreparedPattern(
                    p_combat,
                    p_target,
                    out int patternIndex))
            {
                return false;
            }

            return PreparePattern(
                p_combat,
                patternIndex);
        }

        private bool PreparePattern(
            EnemyCombatModule p_combat,
            int p_patternIndex)
        {
            EnemyAttackPatternSetting pattern =
                p_combat.GetPattern(p_patternIndex);

            if (pattern == null)
                return false;

            _preparedPatternIndex = p_patternIndex;
            _core.AnimationView?.PlayAttackWait(pattern.AttackType);
            OnAttackWaitStarted?.Invoke(pattern.AttackType);
            return true;
        }

        // 준비된 패턴을 우선 선택하고, 모두 쿨타임이면 가장 빨리 준비될 패턴을 고른다.
        private static bool TrySelectPreparedPattern(
            EnemyCombatModule p_combat,
            Transform p_target,
            out int p_patternIndex)
        {
            if (TrySelectReadyPattern(
                    p_combat,
                    p_target,
                    out p_patternIndex))
            {
                return true;
            }

            p_patternIndex = -1;
            float shortestCooldown = float.PositiveInfinity;

            for (int index = 0; index < p_combat.PatternCount; index++)
            {
                if (!p_combat.CanPreparePattern(index, p_target))
                    continue;

                float cooldown =
                    p_combat.GetCooldownRemaining(index);

                if (cooldown >= shortestCooldown)
                    continue;

                shortestCooldown = cooldown;
                p_patternIndex = index;
            }

            return p_patternIndex >= 0;
        }

        private static bool TrySelectReadyPattern(
            EnemyCombatModule p_combat,
            Transform p_target,
            out int p_patternIndex)
        {
            p_patternIndex = -1;
            float totalWeight = 0f;

            for (int index = 0; index < p_combat.PatternCount; index++)
            {
                if (!p_combat.CanStartPattern(index, p_target))
                    continue;

                EnemyAttackPatternSetting pattern =
                    p_combat.GetPattern(index);

                totalWeight += pattern.SelectionWeight;
            }

            if (totalWeight <= 0f)
                return false;

            float selection = UnityEngine.Random.value * totalWeight;

            for (int index = 0; index < p_combat.PatternCount; index++)
            {
                if (!p_combat.CanStartPattern(index, p_target))
                    continue;

                EnemyAttackPatternSetting pattern =
                    p_combat.GetPattern(index);

                selection -= pattern.SelectionWeight;

                if (selection > 0f)
                    continue;

                p_patternIndex = index;
                return true;
            }

            return false;
        }

        private void ClearPreparedPattern()
        {
            _preparedPatternIndex = -1;
        }

        private void OnDisable()
        {
            CancelAttack();
        }
    }
}
