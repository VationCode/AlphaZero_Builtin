using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 몸체로 타격하는 공격의 설정이다. 돌진·제자리 점프도 이동 설정으로 구성한다.
    [Serializable]
    public sealed class BossMeleeAttackSettings : BossAttackSettings
    {
        [SerializeField, HideInInspector] private bool _useMovement;
        [SerializeField] private BossDirectHitSettings _directHit = new();
        [SerializeField] private BossMovementAttackSettings _movementAttack = new();
        [SerializeField] private BossRushAttackSettings _rushAttack = new();

        public override EBossAttackType AttackType => _useMovement ? EBossAttackType.Rush : EBossAttackType.Melee;
        public BossDirectHitSettings DirectHit => _directHit;
        public BossMovementAttackSettings MovementAttack => _movementAttack;
        public BossRushAttackSettings RushAttack => _rushAttack;

        public BossMeleeAttackSettings(bool p_useMovement = false) => _useMovement = p_useMovement;

        internal void CopyDetailsFrom(BossMeleeAttackSettings p_source)
        {
            _directHit = p_source.DirectHit;
            _movementAttack = p_source.MovementAttack;
            _rushAttack = p_source.RushAttack;
        }

        internal void Restore(BossDamageSettings p_damage, BossMovementAttackSettings p_movement,
            BossRushAttackSettings p_rush)
        {
            _directHit = BossDirectHitSettings.FromLegacy(p_damage);
            _movementAttack = p_movement ?? new BossMovementAttackSettings();
            _rushAttack = p_rush ?? new BossRushAttackSettings();
        }

        public override void Validate()
        {
            base.Validate();
            _directHit ??= new BossDirectHitSettings();
            _directHit.Validate();
            _movementAttack ??= new BossMovementAttackSettings();
            _movementAttack.Validate();
            _rushAttack ??= new BossRushAttackSettings();
            _rushAttack.Validate();
        }
    }
}
