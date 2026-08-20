using System;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss.Combat
{
    [Serializable]
    public sealed class CrabBossRangeAttackStrategy : BossAttackStrategy
    {
        [SerializeField]
        private CrabBossRangeAttackSetting[] _attackSettings =
            Array.Empty<CrabBossRangeAttackSetting>();

        public override EAttackPattern Pattern =>
            EAttackPattern.RangeAttack;

        public int AttackCount => _attackSettings?.Length ?? 0;

        public override bool Begin(
            CrabBossContext p_context,
            CrabBossLocomotionModule p_locomotion)
        {
            // 실행 로직은 이후 단계에서 설정을 기준으로 다시 구성한다.
            Cancel();
            return false;
        }

        public override void Tick(float p_deltaTime) { }

        // 애니메이션 이름과 실행 설정을 같은 인덱스에서 조회한다.
        public bool TryGetSetting(
            int p_index,
            out CrabBossRangeAttackSetting p_setting)
        {
            p_setting = null;

            if (_attackSettings == null ||
                p_index < 0 ||
                p_index >= _attackSettings.Length)
            {
                return false;
            }

            p_setting = _attackSettings[p_index];
            return p_setting != null && p_setting.IsValid;
        }
    }
}
