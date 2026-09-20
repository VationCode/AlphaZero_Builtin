using System;
using UnityEngine;

namespace Alpha.Boss
{
    // Rush의 시점별 연출 설정을 보관한다. 피해 설정은 패턴의 Damage가 소유한다.
    [Serializable]
    public sealed class BossRushAttackSettings
    {
        [SerializeField] private BossRushEffectSettings[] _effects = Array.Empty<BossRushEffectSettings>();

        public int EffectCount => _effects?.Length ?? 0;
        public BossRushEffectSettings GetEffect(int p_index) => _effects[p_index];

        public void Validate()
        {
            _effects ??= Array.Empty<BossRushEffectSettings>();
            for (int i = 0; i < _effects.Length; i++)
            {
                _effects[i] ??= new BossRushEffectSettings();
                _effects[i].Validate();
            }
        }
    }
}
