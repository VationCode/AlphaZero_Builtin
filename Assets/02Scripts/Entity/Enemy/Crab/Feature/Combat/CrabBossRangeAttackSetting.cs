using System;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss.Combat
{
    // 원거리 공격이 실행할 기능을 구분한다.
    public enum ECrabRangeAttackType
    {
        StraightProjectile
    }

    // 직선형 투사체가 이동을 종료할 기준을 구분한다.
    public enum ECrabProjectileEndMode
    {
        MaxDistance,
        LockedTargetPoint
    }

    [Serializable]
    public sealed class CrabBossRangeAttackSetting
    {
        [SerializeField]
        private string _stateName;

        [SerializeField]
        private ECrabRangeAttackType _attackType;

        [SerializeField]
        private ECrabProjectileEndMode _endMode;

        public string StateName => _stateName;
        public ECrabRangeAttackType AttackType => _attackType;
        public ECrabProjectileEndMode EndMode => _endMode;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(_stateName);
    }
}
