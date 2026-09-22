using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 네 공격 타입이 공유하는 시간 기반 절차다. 실제 공격은 단계 알림을 받은 Module이 수행한다.
    public abstract class BossSequencePattern : BossPattern
    {
        private readonly BossPatternDefinition _definition;
        private readonly Action<BossPatternExecution> _publish;
        private readonly float _prepareDuration, _activeDuration, _recoveryDuration;
        private float _remaining;
        private float _phaseDuration;
        private Vector3 _targetPosition;
        private bool _finished, _cancelled, _exited;

        protected BossSequencePattern(BossPatternDefinition p_definition, Action<BossPatternExecution> p_publish)
            : base(p_definition != null ? p_definition.Common : throw new ArgumentNullException(nameof(p_definition)))
        {
            _definition = p_definition;
            _publish = p_publish;
            _prepareDuration = BossPatternSettings.NonNegative(p_definition.PrepareDuration);
            _activeDuration = BossPatternSettings.NonNegative(p_definition.ActiveDuration);
            _recoveryDuration = BossPatternSettings.NonNegative(p_definition.RecoveryDuration);
        }

        public override bool CanExecute(BossContext p_context) => p_context != null && p_context.Target != null;
        public override bool IsFinished => _finished;

        public override void Enter(BossContext p_context)
        {
            _finished = _cancelled = _exited = false;
            _targetPosition = Runtime.Target.position;
            EnterPhase(EBossPatternPhase.Prepare, _prepareDuration);
            AdvanceSequence(0f);
        }

        public override void Update(float p_deltaTime) => AdvanceSequence(p_deltaTime);

        private void AdvanceSequence(float p_deltaTime)
        {
            // 큰 deltaTime의 초과 시간을 다음 단계에 전달한다. 전환은 최대 세 단계다.
            float remainingDelta = p_deltaTime;
            while (!_finished && !CancellationRequested)
            {
                if (remainingDelta < _remaining)
                {
                    _remaining -= remainingDelta;
                    Runtime.SetPhaseElapsedTime(_phaseDuration - _remaining);
                    return;
                }
                remainingDelta -= _remaining;
                switch (Runtime.Phase)
                {
                    case EBossPatternPhase.Prepare:
                        // 준비가 끝나는 시점의 목표 위치를 고정한다.
                        if (Runtime.Target != null) _targetPosition = Runtime.Target.position;
                        EnterPhase(EBossPatternPhase.Active, _activeDuration);
                        break;
                    case EBossPatternPhase.Active:
                        EnterPhase(EBossPatternPhase.Recovery, _recoveryDuration);
                        break;
                    case EBossPatternPhase.Recovery:
                        _finished = true;
                        break;
                    default:
                        _finished = true;
                        break;
                }
            }
        }

        private void EnterPhase(EBossPatternPhase p_phase, float p_duration)
        {
            _remaining = p_duration;
            _phaseDuration = p_duration;
            ChangePhase(p_phase);
            Publish(p_phase);
        }

        public override void Cancel() { _cancelled = true; _finished = true; }

        // 중단 단계와 무관하게 최종 정리 알림은 한 번 전달한다.
        public override void Exit()
        {
            if (_exited) return;
            _exited = _finished = true;
            EBossPatternPhase phase = _cancelled ? EBossPatternPhase.Cancelled : EBossPatternPhase.Completed;
            ChangePhase(phase);
            Publish(phase);
        }

        private void Publish(EBossPatternPhase p_phase) => _publish?.Invoke(new BossPatternExecution(
            _definition, Settings.Id, p_phase, Runtime.Target, _targetPosition));
    }
}
