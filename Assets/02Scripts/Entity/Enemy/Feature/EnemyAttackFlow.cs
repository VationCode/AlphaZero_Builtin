using UnityEngine;

namespace Alpha.Enemy
{
    // 사용 가능한 공격 패턴을 가중치로 선택하고 공격 실행 시점을 결정한다.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackFlow : MonoBehaviour
    {
        private EnemyCore _core;

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
                combat.TickAttack(
                    p_target,
                    _core.LocomotionModule,
                    p_deltaTime);
                return;
            }

            if (!p_isFacingTarget ||
                !TrySelectPattern(
                    combat,
                    p_target,
                    out int patternIndex))
            {
                return;
            }

            if (!combat.TryBeginAttack(
                    patternIndex,
                    p_target,
                    out EnemyAttackPatternSetting pattern))
            {
                return;
            }

            _core.AnimationView?.PlayAttack(pattern.AnimationIndex);

            combat.TickAttack(
                p_target,
                _core.LocomotionModule,
                p_deltaTime);
        }

        public void CancelAttack()
        {
            if (_core == null || _core.CombatModule == null)
                return;

            _core.CombatModule.CancelAttack(
                _core.LocomotionModule);
        }

        private static bool TrySelectPattern(
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

            float selection = Random.value * totalWeight;

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

        private void OnDisable()
        {
            CancelAttack();
        }
    }
}
