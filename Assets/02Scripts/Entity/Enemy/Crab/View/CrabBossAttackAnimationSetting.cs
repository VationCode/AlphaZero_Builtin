using System;
using Alpha.Enemy.CrabBoss.Combat;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    [Serializable]
    public sealed class CrabBossAttackAnimationSetting
    {
        [SerializeField]
        private EAttackPattern _pattern;

        [SerializeField]
        private string[] _stateNames = Array.Empty<string>();

        public EAttackPattern Pattern => _pattern;
        public int StateCount => _stateNames?.Length ?? 0;

        public bool TryGetStateName(
            int p_index,
            out string p_stateName)
        {
            p_stateName = null;

            if (_stateNames == null ||
                p_index < 0 ||
                p_index >= _stateNames.Length ||
                string.IsNullOrWhiteSpace(_stateNames[p_index]))
            {
                return false;
            }

            p_stateName = _stateNames[p_index];
            return true;
        }
    }
}
