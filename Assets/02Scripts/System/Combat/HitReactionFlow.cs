using UnityEngine;

namespace Alpha.Combat
{
    // Entity가 실제로 진행 중인 공용 피격 행동 상태를 나타낸다.
    public enum EHitReactionState
    {
        None,
        LightHit,
        HeavyHit,
        Knockdown,
        LyingDown,
        StandUp
    }

    // Entity와 무관하게 피격 우선순위와 상태별 시간을 관리한다.
    public sealed class HitReactionFlow
    {
        private float _remainingTime;
        private float _downRecoveryDuration;
        private float _standupDuration;
        private float _nextLightHitTime = float.NegativeInfinity;

        public EHitReaction CurrentReaction { get; private set; } =
            EHitReaction.None;

        public EHitReactionState CurrentState { get; private set; } =
            EHitReactionState.None;

        public bool IsActive =>
            CurrentState != EHitReactionState.None;

        public void Reset()
        {
            Clear();
            _nextLightHitTime = float.NegativeInfinity;
        }

        // 현재 반응만 종료하고 연속 Light 피격 제한 시간은 유지한다.
        public void Clear()
        {
            CurrentReaction = EHitReaction.None;
            CurrentState = EHitReactionState.None;
            _remainingTime = 0f;
            _downRecoveryDuration = 0f;
            _standupDuration = 0f;
        }

        public bool TryBegin(
            in ImpactReactionResult p_result,
            float p_currentTime,
            float p_lightRepeatInterval,
            float p_knockdownFallDuration,
            float p_standupDuration)
        {
            if (!p_result.HasReaction ||
                p_result.Priority < (int)CurrentReaction)
            {
                return false;
            }

            if (p_result.Reaction == EHitReaction.Light &&
                p_currentTime < _nextLightHitTime)
            {
                return false;
            }

            if (p_result.Reaction == EHitReaction.Light)
            {
                _nextLightHitTime =
                    p_currentTime +
                    Mathf.Max(0f, p_lightRepeatInterval);
            }

            CurrentReaction = p_result.Reaction;
            _standupDuration = Mathf.Max(0f, p_standupDuration);

            if (CurrentReaction is EHitReaction.Knockdown or
                EHitReaction.Launch)
            {
                CurrentState = EHitReactionState.Knockdown;
                _remainingTime = Mathf.Max(
                    0f,
                    p_knockdownFallDuration);
                _downRecoveryDuration = p_result.RecoveryDuration;
                return true;
            }

            CurrentState = CurrentReaction == EHitReaction.Heavy
                ? EHitReactionState.HeavyHit
                : EHitReactionState.LightHit;
            _remainingTime = p_result.RecoveryDuration;
            _downRecoveryDuration = 0f;
            return true;
        }

        // 현재 상태의 시간을 갱신하고 반응이 유지되는지 반환한다.
        public bool Tick(
            float p_deltaTime,
            bool p_isKnockbackActive)
        {
            if (!IsActive)
                return false;

            switch (CurrentState)
            {
                case EHitReactionState.LightHit:
                case EHitReactionState.HeavyHit:
                    if (TickTimer(p_deltaTime))
                    {
                        Clear();
                        return false;
                    }
                    break;

                case EHitReactionState.Knockdown:
                    if (TickTimer(p_deltaTime))
                    {
                        CurrentState = EHitReactionState.LyingDown;
                        _remainingTime = _downRecoveryDuration;
                    }
                    break;

                case EHitReactionState.LyingDown:
                    // 물리 넉백이 끝난 뒤부터 LyingDown 회복 시간을 계산한다.
                    if (!p_isKnockbackActive &&
                        TickTimer(p_deltaTime))
                    {
                        CurrentState = EHitReactionState.StandUp;
                        _remainingTime = _standupDuration;
                    }
                    break;

                case EHitReactionState.StandUp:
                    if (TickTimer(p_deltaTime))
                    {
                        Clear();
                        return false;
                    }
                    break;
            }

            return true;
        }

        private bool TickTimer(float p_deltaTime)
        {
            _remainingTime = Mathf.Max(
                0f,
                _remainingTime - Mathf.Max(0f, p_deltaTime));

            return _remainingTime <= 0f;
        }
    }
}
