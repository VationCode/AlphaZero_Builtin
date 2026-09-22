using UnityEngine;

namespace Alpha.Boss
{
    // Module·View에 전달하는 실행 시점 정보다. 실행 중인 패턴 객체는 노출하지 않는다.
    public readonly struct BossPatternExecution
    {
        public BossPatternDefinition Definition { get; }
        public string PatternId { get; }
        public EBossPatternPhase Phase { get; }
        public Transform Target { get; }
        public Vector3 TargetPosition { get; }

        public BossPatternExecution(BossPatternDefinition p_definition, string p_patternId,
            EBossPatternPhase p_phase, Transform p_target, Vector3 p_targetPosition)
        {
            Definition = p_definition;
            PatternId = p_patternId;
            Phase = p_phase;
            Target = p_target;
            TargetPosition = p_targetPosition;
        }
    }
}
