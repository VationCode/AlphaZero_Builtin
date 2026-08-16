using System;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    [Serializable]
    public sealed class CrabBossAttackAnimationSetting
    {
        [SerializeField] private AnimationClip _clip;
        [SerializeField] private bool _useRootMotion;

        public AnimationClip Clip => _clip;
        public bool UseRootMotion => _useRootMotion;
    }
}
