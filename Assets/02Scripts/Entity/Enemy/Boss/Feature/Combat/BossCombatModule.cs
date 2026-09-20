using UnityEngine;

namespace Alpha.Boss
{
    // 보스 공격 실행의 대표 진입점이다. 공격 종류에 맞는 세부 Module에 한 회차 실행을 전달한다.
    public sealed class BossCombatModule
    {
        private readonly BossRangeAttackModule _rangeAttack = new();
        private readonly BossArenaAttackModule _arenaAttack = new();
        private readonly BossAreaAttackModule _areaAttack = new();
        private readonly BossRushAttackModule _rushAttack = new();
        private readonly BossDamageModule _damage = new();

        public bool CanExecute(BossPatternData p_pattern, Rigidbody p_body = null)
        {
            if (p_pattern == null || string.IsNullOrWhiteSpace(p_pattern.AnimationKey))
                return false;
            bool direct = p_pattern.AttackType == EBossAttackType.Melee || p_pattern.AttackType == EBossAttackType.Rush;
            if (direct && (p_pattern.Damage == null || !p_pattern.Damage.IsConfigured(p_pattern.AttackType)))
                return false;
            // 피해값은 모든 방식의 공통 설정이며, 직접 타격을 끈 이동 패턴만 예외다.
            if ((!direct || p_pattern.Damage.Enabled) &&
                (p_pattern.DamageProfile == null || !p_pattern.DamageProfile.IsValid ||
                 float.IsNaN(p_pattern.DamageProfile.Damage) || float.IsInfinity(p_pattern.DamageProfile.Damage)))
                return false;
            return p_pattern.AttackType switch
            {
                EBossAttackType.Melee => true,
                EBossAttackType.Range => _rangeAttack.IsConfigured(p_pattern.RangeAttack),
                EBossAttackType.Arena => _arenaAttack.IsConfigured(p_pattern.ArenaAttack),
                EBossAttackType.Area => _areaAttack.IsConfigured(p_pattern.AreaAttack),
                EBossAttackType.Rush => p_body != null && !p_body.isKinematic &&
                    _rushAttack.IsConfigured(p_pattern.MovementAttack) && p_pattern.RushAttack != null,
                _ => false
            };
        }

        public bool BeginRush(Rigidbody p_body, Transform p_target, BossCombatContext p_context) =>
            _rushAttack.Begin(p_body, p_target, p_context.CurrentPattern.MovementAttack, p_context.Rush,
                p_context.Rush.RootMotionCurve);

        public bool TickRush(BossCombatContext p_context, float p_stepSeconds, float p_fixedDeltaTime) =>
            _rushAttack.Tick(p_context.CurrentPattern.MovementAttack, p_context.Rush,
                p_stepSeconds, p_fixedDeltaTime);

        public void StopRush() => _rushAttack.Stop();

        public void ApplyDamage(Transform p_owner, Vector3 p_position, Vector3 p_forward,
            BossCombatContext p_context, bool p_sweep) =>
            _damage.Apply(p_owner, p_position, p_forward, p_context, p_sweep);

        public bool ExecuteGroup(Transform p_owner, Transform p_target,
            BossPatternData p_pattern, int p_groupIndex)
        {
            if (p_pattern == null)
                return false;
            return p_pattern.AttackType switch
            {
                EBossAttackType.Range => _rangeAttack.Execute(p_owner, p_target, p_pattern, p_groupIndex),
                EBossAttackType.Area => _areaAttack.Execute(p_owner, p_target, p_pattern),
                EBossAttackType.Arena => _arenaAttack.Execute(p_owner, p_pattern, p_groupIndex),
                _ => false
            };
        }
    }
}
