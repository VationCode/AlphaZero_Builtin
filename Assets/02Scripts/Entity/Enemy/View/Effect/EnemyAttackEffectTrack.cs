using System;
using UnityEngine;

namespace Alpha.Enemy.Effect
{
    // 공격 타입과 AnimationIndex별로 복수 Effect 구간을 묶는다.
    [Serializable]
    public sealed class EnemyAttackEffectTrack
    {
        [SerializeField]
        private EEnemyAttackType _attackType;

        [Tooltip("-1은 해당 공격 타입의 기본 Effect Track입니다.")]
        [SerializeField, Min(-1)]
        private int _animationIndex = -1;

        [Tooltip("한 공격 애니메이션에서 순서와 구간을 달리해 재생할 Effect 목록입니다.")]
        [SerializeField]
        private EnemyAttackEffectTimingSetting[] _effectTimings =
            Array.Empty<EnemyAttackEffectTimingSetting>();

        public EEnemyAttackType AttackType => _attackType;
        public int AnimationIndex => _animationIndex;
        public int TimingCount => _effectTimings?.Length ?? 0;

        public EnemyAttackEffectTimingSetting GetTiming(int p_index)
        {
            return p_index >= 0 && p_index < TimingCount
                ? _effectTimings[p_index]
                : null;
        }

        public void Validate()
        {
            _animationIndex = Mathf.Max(-1, _animationIndex);
            _effectTimings ??=
                Array.Empty<EnemyAttackEffectTimingSetting>();

            for (int index = 0;
                 index < _effectTimings.Length;
                 index++)
            {
                _effectTimings[index] ??=
                    new EnemyAttackEffectTimingSetting();
                _effectTimings[index].Validate();
            }
        }
    }
}
