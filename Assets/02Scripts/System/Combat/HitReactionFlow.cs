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
        private const int ReactionTypeCount =
            (int)EHitReaction.Launch + 1;

        private readonly float[] _immunityEndTimes =
            new float[ReactionTypeCount];

        private float _remainingTime;
        private float _downRecoveryDuration;
        private float _standupDuration;
        private float _currentImmunityDuration;

        public EHitReaction CurrentReaction { get; private set; } =
            EHitReaction.None;

        public EHitReactionState CurrentState { get; private set; } =
            EHitReactionState.None;

        public bool IsActive =>
            CurrentState != EHitReactionState.None;

        public void Reset()
        {
            Clear();

            for (int index = 0;
                 index < _immunityEndTimes.Length;
                 index++)
            {
                _immunityEndTimes[index] = float.NegativeInfinity;
            }
        }

        // 현재 반응만 종료하고 타입별 피격 면역 시간은 유지한다.
        public void Clear()
        {
            CurrentReaction = EHitReaction.None;
            CurrentState = EHitReactionState.None;
            _remainingTime = 0f;
            _downRecoveryDuration = 0f;
            _standupDuration = 0f;
            _currentImmunityDuration = 0f;
        }

        public bool TryBegin(
            in ImpactReactionResult p_result,
            float p_currentTime,
            HitReactionImmunitySettings p_immunitySettings,
            float p_knockdownFallDuration,
            float p_standupDuration)
        {
            if (!p_result.HasReaction ||
                (IsActive &&
                 p_result.Priority <= (int)CurrentReaction) ||
                IsImmune(
                    p_result.Reaction,
                    p_currentTime))
            {
                return false;
            }

            // 더 강한 반응이 현재 반응을 중단하면 중단된 타입의 면역을 즉시 시작한다.
            if (IsActive)
                BeginCurrentImmunity(p_currentTime);

            CurrentReaction = p_result.Reaction;
            _standupDuration = Mathf.Max(0f, p_standupDuration);
            _currentImmunityDuration =
                p_immunitySettings?.GetDuration(CurrentReaction) ?? 0f;

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
            bool p_isKnockbackActive,
            float p_currentTime)
        {
            if (!IsActive)
                return false;

            switch (CurrentState)
            {
                case EHitReactionState.LightHit:
                case EHitReactionState.HeavyHit:
                    if (TickTimer(p_deltaTime))
                    {
                        BeginCurrentImmunity(p_currentTime);
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
                        BeginCurrentImmunity(p_currentTime);
                        Clear();
                        return false;
                    }
                    break;
            }

            return true;
        }

        private bool IsImmune(
            EHitReaction p_reaction,
            float p_currentTime)
        {
            int index = (int)p_reaction;

            return index > (int)EHitReaction.None &&
                   index < _immunityEndTimes.Length &&
                   p_currentTime < _immunityEndTimes[index];
        }

        private void BeginCurrentImmunity(float p_currentTime)
        {
            int index = (int)CurrentReaction;

            if (index <= (int)EHitReaction.None ||
                index >= _immunityEndTimes.Length)
            {
                return;
            }

            _immunityEndTimes[index] = Mathf.Max(
                _immunityEndTimes[index],
                p_currentTime +
                Mathf.Max(0f, _currentImmunityDuration));
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
