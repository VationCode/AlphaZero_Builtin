using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 패턴 선택과 발사·전장 생성·Rush 이동의 실행 시점, 종료·취소를 판단한다.
    [DisallowMultipleComponent]
    public sealed class BossCombatFlow : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _recoverySeconds = 0.5f;
        private BossCore _boss;
        private BossCombatModule _combat;
        private BossCombatContext _context;
        private BossPatternGroup[] _patternGroups;
        private readonly BossAttackPatternSelector _patternSelector = new();
        public event Action<BossPatternData, EBossRushEffectTiming> OnRushEffectRequested;
        public event Action OnAttackCancelled;
        public event Action OnAttackStarted;

        public void Bind(BossCore p_boss, BossCombatModule p_combat, BossCombatContext p_context,
            BossPatternGroup[] p_patternGroups)
        {
            CancelAttack();
            if (_boss != null)
                _boss.EncounterContext.OnStateChanged -= HandleEncounterState;
            _boss = p_boss;
            _combat = p_combat;
            _context = p_context;
            _patternGroups = p_patternGroups;
            _context.Clear();
            _patternSelector.History.Clear();
            _boss.EncounterContext.OnStateChanged += HandleEncounterState;
        }

        private bool CanAct => isActiveAndEnabled && _boss != null && _boss.isActiveAndEnabled &&
            !_boss.HealthContext.IsDead && _boss.EncounterContext.CurrentState == EBossEncounterState.Combat;

        private bool CanStartAttack(BossPatternData p_pattern) =>
            CanAct && _context != null && _context.State == EBossCombatState.Idle && _combat != null &&
            _combat.CanExecute(p_pattern, _boss.Rigidbody) && _boss.AnimationView != null &&
            (!RequiresTarget(p_pattern) || _boss.Target != null);

        // ActionFlow에는 패턴 선택·실행 가능 여부만 제공한다. 이동 판단은 소유하지 않는다.
        public bool TrySelectActionPattern(float p_distance, bool p_canReposition, out BossPatternData p_pattern) =>
            _patternSelector.TrySelectActionPattern(_patternGroups, p_distance, p_canReposition,
                CanPrepareActionPattern, out p_pattern);

        private bool CanPrepareActionPattern(BossPatternData p_pattern) =>
            CanStartAttack(p_pattern) && _boss.AnimationView.CanPlayAttack(p_pattern.AnimationKey);

        public bool CanKeepActionPattern(BossPatternData p_pattern, float p_distance, bool p_allowReposition)
        {
            if (!CanPrepareActionPattern(p_pattern) || p_pattern.Selection == null || _patternGroups == null)
                return false;
            bool inRange = p_pattern.Selection.CanSelectAtDistance(p_distance);
            if (!inRange && (!p_allowReposition || !p_pattern.Selection.CanRepositionFromDistance(p_distance)))
                return false;
            foreach (BossPatternGroup group in _patternGroups)
            {
                if (group == null || !group.isActiveAndEnabled)
                    continue;
                for (int i = 0; i < group.PatternCount; i++)
                    if (group.GetPattern(i) == p_pattern)
                        return true;
            }
            return false;
        }

        public bool TryStartRandomAttack(EBossPatternGroupType p_groupType, EBossAttackType p_attackType)
        {
            if (!CanAct || _boss.Target == null || _context == null || _context.State != EBossCombatState.Idle)
                return false;
            Vector3 offset = _boss.Target.position -
                (_boss.Rigidbody != null ? _boss.Rigidbody.position : _boss.transform.position);
            offset.y = 0f;
            return _patternSelector.TrySelectPattern(_patternGroups, p_groupType, p_attackType,
                offset.magnitude, CanStartAttack, out BossPatternData pattern) && TryStartAttack(pattern);
        }

        public bool TryStartAttack(BossPatternData p_pattern)
        {
            if (!CanStartAttack(p_pattern))
                return false;

            _context.Begin(p_pattern);
            if (_boss.AnimationView.TryPlayAttack(_context.AttackId, p_pattern.AnimationKey))
            {
                if (IsRush && !_boss.AnimationView.SetRushMotionControlled(_context.AttackId,
                        p_pattern.MovementAttack.UseRootMotion))
                {
                    CancelAttack();
                    return false;
                }
                // 선택·접근 실패는 소비하지 않고, 실제 시작에 성공한 공격만 기억한다.
                _patternSelector.History.RecordStarted(p_pattern);
                OnAttackStarted?.Invoke();
                return true;
            }
            _context.Clear();
            return false;
        }

        public void NotifyAnimationProgress(long p_attackId, float p_elapsedSeconds, float p_durationSeconds)
        {
            if (!MatchesAttack(p_attackId))
                return;
            if (!CanContinue())
            {
                CancelAttack();
                return;
            }

            BossPatternData pattern = _context.CurrentPattern;
            BossDirectHitSettings damage = pattern.Damage;
            bool animationDamage = IsDirectDamage && damage.Enabled && damage.UsesAnimationWindow(pattern.AttackType);
            if (!_context.AnimationStarted && animationDamage && damage.EndTimeSeconds > p_durationSeconds + 0.001f)
            {
                Debug.LogWarning($"[{name}] {pattern.PatternName}: Damage 종료 시간이 애니메이션 길이를 넘습니다.", this);
                CancelAttack();
                return;
            }
            float previous = _context.AnimationElapsedSeconds;
            _context.AnimationElapsedSeconds = Mathf.Max(previous, Mathf.Clamp(p_elapsedSeconds, 0f, p_durationSeconds));
            if (animationDamage && damage.OverlapsAnimationStep(previous, _context.AnimationElapsedSeconds))
            {
                ApplyDirectDamage(true);
                if (!MatchesAttack(p_attackId))
                    return;
            }
            else if (animationDamage)
                _context.Damage.HasPosition = false;
            if ((IsScheduledArea || IsRush) && _context.AnimationStarted)
                return;
            _context.AnimationStarted = true;
            _context.ElapsedSeconds = Mathf.Max(_context.ElapsedSeconds,
                Mathf.Clamp(p_elapsedSeconds, 0f, p_durationSeconds));
            if (IsRush)
            {
                if (_context.CurrentPattern.MovementAttack.UseRootMotion)
                    _context.Rush.RootMotionCurve = _boss.AnimationView.SampleRushRootMotion(
                        _context.CurrentPattern.MovementAttack.StartTimeSeconds,
                        _context.CurrentPattern.MovementAttack.DurationSeconds);
                OnRushEffectRequested?.Invoke(_context.CurrentPattern, EBossRushEffectTiming.AttackStart);
            }
            else
                ExecuteReadyGroups(p_attackId);
        }

        private void ExecuteReadyGroups(long p_attackId)
        {
            while (_context.NextGroup < _context.GroupCount && _context.NextFireTime <= _context.ElapsedSeconds)
            {
                // 먼저 소비하여 피해 이벤트가 재진입해도 같은 회차를 다시 실행하지 않는다.
                int groupIndex = _context.ConsumeGroup();
                if (!_combat.ExecuteGroup(_boss.transform, _boss.Target, _context.CurrentPattern, groupIndex))
                {
                    CancelAttack();
                    return;
                }
                if (!MatchesAttack(p_attackId))
                    return;
            }
            if (IsScheduledArea && _context.AnimationCompleted)
                TryBeginAreaRecovery();
        }

        public void NotifyAnimationCompleted(long p_attackId)
        {
            if (!MatchesAttack(p_attackId))
                return;
            if (!CanContinue())
            {
                CancelAttack();
                return;
            }
            _context.AnimationCompleted = true;
            if (IsRush)
            {
                TryBeginRushRecovery();
                return;
            }
            if (IsScheduledArea)
            {
                TryBeginAreaRecovery();
                return;
            }
            if (_context.NextGroup < _context.GroupCount)
                Debug.LogWarning($"[{name}] 애니메이션 길이를 넘는 발사 그룹은 실행하지 않았습니다.", this);
            _context.BeginRecovery(Mathf.Max(0f, _recoverySeconds));
        }

        public void NotifyAnimationInterrupted(long p_attackId)
        {
            if (MatchesAttack(p_attackId))
                CancelAttack();
        }

        public void CancelAttack()
        {
            if (_context == null)
                return;
            long id = _context.AttackId;
            _combat?.StopRush();
            _context.Clear();
            _boss?.AnimationView?.StopAttack(id);
            OnAttackCancelled?.Invoke();
        }

        private bool MatchesAttack(long p_id) => _context != null &&
            _context.State == EBossCombatState.Attack && _context.AttackId == p_id;

        private bool IsArea => _context.CurrentPattern?.AttackType == EBossAttackType.Area;
        private bool IsScheduledArea => IsArea || _context.CurrentPattern?.AttackType == EBossAttackType.Arena;
        private bool IsRush => _context.CurrentPattern?.AttackType == EBossAttackType.Rush;

        private static bool RequiresTarget(BossPatternData p_pattern) => p_pattern != null &&
            (p_pattern.AttackType == EBossAttackType.Area ||
             (p_pattern.AttackType == EBossAttackType.Range &&
              p_pattern.RangeAttack.DirectionType == EBossRangeDirectionType.Target) ||
             (p_pattern.AttackType == EBossAttackType.Rush &&
              p_pattern.MovementAttack.DirectionType == EBossMovementDirectionType.Target));

        private bool CanContinue() => CanAct &&
            (!RequiresTarget(_context.CurrentPattern) || _boss.Target != null ||
             (IsArea && _context.NextGroup >= _context.GroupCount) || (IsRush && _context.Rush.Started));

        private void TryBeginAreaRecovery()
        {
            if (_context.AnimationCompleted && _context.NextGroup >= _context.GroupCount)
                _context.BeginRecovery(Mathf.Max(0f, _recoverySeconds));
        }

        private void TryBeginRushRecovery()
        {
            if (_context.AnimationCompleted && _context.Rush.Completed)
            {
                _boss.AnimationView.ReleaseRushMotion(_context.AttackId);
                _context.BeginRecovery(Mathf.Max(0f, _recoverySeconds));
            }
        }

        private void FixedUpdate()
        {
            if (_context == null || _context.State != EBossCombatState.Attack ||
                !IsRush || !_context.AnimationStarted)
                return;
            if (!CanContinue())
            {
                CancelAttack();
                return;
            }
            if (_context.Rush.Completed || Time.fixedDeltaTime <= 0f)
                return;

            float stepSeconds = Time.fixedDeltaTime;
            long attackId = _context.AttackId;
            _context.ElapsedSeconds += stepSeconds;
            if (!_context.Rush.Started)
            {
                float startTime = _context.CurrentPattern.MovementAttack.StartTimeSeconds;
                if (_context.ElapsedSeconds < startTime)
                    return;
                if (!_combat.BeginRush(_boss.Rigidbody, _boss.Target, _context))
                {
                    CancelAttack();
                    return;
                }
                // 시작 시각이 물리 스텝 중간이면 남은 시간만큼만 이동한다.
                stepSeconds = Mathf.Min(stepSeconds, _context.ElapsedSeconds - startTime);
                OnRushEffectRequested?.Invoke(_context.CurrentPattern, EBossRushEffectTiming.MovementStart);
                if (!MatchesAttack(attackId))
                    return;
            }

            if (!_combat.TickRush(_context, stepSeconds, Time.fixedDeltaTime))
            {
                CancelAttack();
                return;
            }
            bool completed = _context.Rush.Completed;
            if (completed)
            {
                OnRushEffectRequested?.Invoke(_context.CurrentPattern, EBossRushEffectTiming.MovementEnd);
                if (!MatchesAttack(attackId))
                    return;
            }
            BossPatternData pattern = _context.CurrentPattern;
            EBossDamageTiming timing = ResolveDamageTiming(pattern);
            if (pattern.Damage.Enabled && (timing == EBossDamageTiming.DuringMovement ||
                (timing == EBossDamageTiming.MovementEnd && completed)))
            {
                ApplyDirectDamage(timing == EBossDamageTiming.DuringMovement);
                if (!MatchesAttack(attackId))
                    return;
            }
            TryBeginRushRecovery();
        }

        private void Update()
        {
            if (_context == null || _context.State == EBossCombatState.Idle)
                return;
            if (!CanContinue())
            {
                CancelAttack();
                return;
            }
            if (_context.State == EBossCombatState.Attack && IsScheduledArea &&
                _context.AnimationStarted && Time.deltaTime > 0f)
            {
                // Area·Arena는 애니메이션 길이와 독립된 게임 시간으로 남은 그룹을 끝까지 실행한다.
                _context.ElapsedSeconds += Time.deltaTime;
                ExecuteReadyGroups(_context.AttackId);
            }
            else if (_context.State == EBossCombatState.Recovery)
            {
                _context.RecoveryRemaining -= Time.deltaTime;
                if (_context.RecoveryRemaining <= 0f)
                    _context.Clear();
            }
        }

        private void OnDisable() => CancelAttack();

        private void HandleEncounterState(EBossEncounterState p_state)
        {
            if (p_state != EBossEncounterState.Combat)
                _patternSelector.History.Clear();
        }

        private void OnDestroy()
        {
            if (_boss != null)
                _boss.EncounterContext.OnStateChanged -= HandleEncounterState;
        }

        private bool IsDirectDamage => _context.CurrentPattern?.AttackType == EBossAttackType.Melee || IsRush;

        // 자동 판정의 의미는 공격 종류와 이동 높이를 보고 Flow가 결정한다.
        internal static EBossDamageTiming ResolveDamageTiming(BossPatternData p_pattern)
        {
            if (p_pattern.Damage.Timing != EBossDamageTiming.Automatic)
                return p_pattern.Damage.Timing;
            if (p_pattern.AttackType == EBossAttackType.Melee)
                return EBossDamageTiming.AnimationWindow;
            return p_pattern.MovementAttack.Height > 0f
                ? EBossDamageTiming.MovementEnd : EBossDamageTiming.DuringMovement;
        }

        private void ApplyDirectDamage(bool p_sweep)
        {
            Vector3 position = _boss.Rigidbody != null ? _boss.Rigidbody.position : _boss.transform.position;
            Vector3 forward = IsRush && _context.Rush.Started ? _context.Rush.Direction : _boss.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            _combat.ApplyDamage(_boss.transform, position, forward.normalized, _context, p_sweep);
        }
    }
}
