using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 타겟 주변 범위 공격과 지정 지점의 전장 공격 설정을 소유한다.
    [Serializable]
    public sealed class BossAoEAttackSettings : BossAttackSettings
    {
        [SerializeField, HideInInspector] private bool _useArena;
        [SerializeField] private BossAreaAttackSettings _areaAttack = new();
        [SerializeField] private BossArenaAttackSettings _arenaAttack = new();

        public override EBossAttackType AttackType => _useArena ? EBossAttackType.Arena : EBossAttackType.Area;
        public BossAreaAttackSettings AreaAttack => _areaAttack;
        public BossArenaAttackSettings ArenaAttack => _arenaAttack;

        public BossAoEAttackSettings(bool p_useArena = false) => _useArena = p_useArena;

        internal void Restore(BossAreaAttackSettings p_area, BossArenaAttackSettings p_arena)
        {
            _areaAttack = p_area ?? new BossAreaAttackSettings();
            _arenaAttack = p_arena ?? new BossArenaAttackSettings();
        }

        public override void Validate()
        {
            base.Validate();
            _areaAttack ??= new BossAreaAttackSettings();
            _areaAttack.Validate();
            _arenaAttack ??= new BossArenaAttackSettings();
            _arenaAttack.Validate();
        }
    }
}
