using System;

namespace Alpha.Boss
{
    // GlobalAoE 설정을 사용하는 패턴이다. 현재 단계 시간은 공통 절차를 재사용한다.
    public sealed class BossGlobalAoEPattern : BossSequencePattern
    {
        public BossGlobalAoEPattern(BossGlobalAoEPatternDefinition p_definition,
            Action<BossPatternExecution> p_publish = null) : base(p_definition, p_publish) { }
    }
}

