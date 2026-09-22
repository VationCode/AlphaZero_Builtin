using System;
using UnityEngine;

namespace Alpha.Boss
{
    public enum EBossPatternPhase { None, Prepare, Active, Recovery, Completed, Cancelled }

    // 보스별 패턴 인스턴스가 소유하는 실행 상태다. 설정 에셋과 공유하지 않는다.
    public sealed class BossPatternContext
    {
        public EBossPatternPhase Phase { get; private set; }
        public float ElapsedTime { get; private set; }
        public float PhaseElapsedTime { get; private set; }
        public double CooldownUntil { get; private set; }
        public Transform Target { get; private set; }

        public double GetCooldownRemaining(double p_time) => Math.Max(0d, CooldownUntil - p_time);

        internal void Begin(Transform p_target, double p_time, float p_cooldown)
        {
            Target = p_target;
            ElapsedTime = 0f;
            CooldownUntil = p_time + p_cooldown;
            SetPhase(EBossPatternPhase.Prepare);
        }

        internal void Advance(float p_deltaTime)
        {
            ElapsedTime += p_deltaTime;
            PhaseElapsedTime += p_deltaTime;
        }

        internal void SetPhase(EBossPatternPhase p_phase)
        {
            Phase = p_phase;
            PhaseElapsedTime = 0f;
        }

        internal void SetPhaseElapsedTime(float p_elapsedTime) => PhaseElapsedTime = p_elapsedTime;

        internal void Finish(bool p_cancelled)
        {
            SetPhase(p_cancelled ? EBossPatternPhase.Cancelled : EBossPatternPhase.Completed);
            Target = null;
        }
    }
}
