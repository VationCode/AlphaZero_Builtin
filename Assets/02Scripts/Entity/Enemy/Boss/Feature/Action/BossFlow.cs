using System;
using Alpha.Living;
using UnityEngine;

namespace Alpha.Boss
{
    // 보스 전체 행동 판단을 소유한다. 개별 패턴의 절차는 BossPatternFlow에 위임한다.
    [Serializable]
    public sealed class BossFlow
    {
        [SerializeField, Min(0.1f), Tooltip("살아 있는 Player를 새로 감지할 반경입니다.")]
        private float _detectionRadius = 20f;
        [SerializeField, Min(0.02f), Tooltip("타겟이 없을 때 감지하는 주기(초)입니다.")]
        private float _searchInterval = 0.25f;
        [SerializeField, Tooltip("Player Collider가 속한 레이어입니다.")]
        private LayerMask _targetLayers = 64;

        private BossContext _context;
        private HealthContext _health;
        private BossMovementModule _movement;
        private BossCinematicContext _cinematic;
        private readonly BossTargetDetectionModule _detection = new();
        private bool _enabled;
        private float _searchRemaining;
        private BossPatternFlow _patterns;
        private BossPatternSelector _selector;
        private bool _selectingPattern;
        private bool _startingPattern;
        private float _staggerRemaining;
        private BossPattern _selectedPattern;
        private Transform _selectedTarget;
        public string SelectedPatternId => _selectedPattern?.Settings.Id;
        public bool CanAct => _enabled && _context != null && !_health.IsDead &&
            (_cinematic == null || _cinematic.CurrentState == EBossCinematicState.Completed);

        public void Bind(BossContext p_context, HealthContext p_health, BossMovementModule p_movement,
            BossCinematicContext p_cinematic, BossPatternFlow p_patterns, BossPatternSelector p_selector)
        {
            Unbind();
            _context = p_context;
            _health = p_health;
            _movement = p_movement;
            _cinematic = p_cinematic;
            _patterns = p_patterns;
            _selector = p_selector;
            _health.OnDied += HandleDeath;
            if (_cinematic != null) _cinematic.OnStateChanged += HandleCinematic;
            if (_health.IsDead) HandleDeath();
        }

        public void SetEnabled(bool p_enabled)
        {
            _enabled = p_enabled;
            if (!CanAct) Suspend();
        }

        public void Tick(float p_deltaTime)
        {
            if (_context == null || p_deltaTime <= 0f || float.IsNaN(p_deltaTime) || float.IsInfinity(p_deltaTime)) return;
            if (_enabled) _context.AdvanceTime(p_deltaTime);
            if (!CanAct) { Suspend(); return; }
            if (_context.State == EBossState.Dead) return;
            if (_context.State == EBossState.Stagger)
            {
                _staggerRemaining = Mathf.Max(0f, _staggerRemaining - p_deltaTime);
                if (_staggerRemaining <= 0f) RestoreLocomotion();
                return;
            }
            if (_context.State == EBossState.ExecutePattern)
            {
                if (_detection.ResolvePlayer(_context.Target) == null) CancelPattern();
                else
                {
                    _patterns.Tick(p_deltaTime);
                    RestoreAfterPattern();
                }
                return;
            }
            if (_context.State == EBossState.Idle) TickIdle(p_deltaTime);
            // 감지 직후에도 추적할 수 있지만, 상태 알림에서 행동이 바뀌면 이동하지 않는다.
            if (CanAct && _context.State == EBossState.Chase) TickChase(p_deltaTime);
        }

        // 대기 중에는 정지하고 주기적으로 감지한다. 발견한 타겟을 저장한 뒤 추적으로 전환한다.
        private void TickIdle(float p_deltaTime)
        {
            StopMovement();
            _searchRemaining -= p_deltaTime;
            if (_searchRemaining > 0f) return;
            _searchRemaining = _searchInterval;
            _detection.TryFindClosest(_movement.Position, _detectionRadius, _targetLayers, out Transform target);
            _context.SetTarget(target);
            if (target != null) _context.ChangeState(EBossState.Chase);
        }

        // 선택 범위에서 패턴을 고른 뒤 공격 시작 거리까지 선택을 유지하며 접근한다.
        private void TickChase(float p_deltaTime)
        {
            if (_selectingPattern) return;
            Transform target = _context.Target;
            if (_detection.ResolvePlayer(target) == null)
            {
                ClearSelection();
                StopMovement();
                _context.SetTarget(null);
                _context.ChangeState(EBossState.Idle);
                return;
            }
            float distance = GetTargetDistance(target);
            BossPattern selected = _selectedTarget == target ? _selectedPattern : null;
            bool ready;
            _selectingPattern = true;
            try
            {
                ready = selected != null && _selector.CanSelect(selected, _context, distance);
                if (!CanContinueChase(target)) return;
                if (!ready)
                {
                    // 쿨다운 대기보다 실행 가능한 다른 패턴을 우선한다.
                    ready = _selector.TrySelect(_context, distance, out BossPattern available);
                    if (!CanContinueChase(target)) return;
                    if (ready) selected = available;
                    else if (selected == null || !_selector.CanWaitFor(selected, _context, distance))
                    {
                        if (!CanContinueChase(target)) return;
                        _selector.TrySelectWaiting(_context, distance, out selected);
                    }
                }
            }
            finally { _selectingPattern = false; }
            // 조건 조회 중 사망·경직·타겟 변경이 발생하면 선택 결과와 이동을 적용하지 않는다.
            if (!CanContinueChase(target)) return;
            _selectedPattern = selected;
            _selectedTarget = selected != null ? target : null;
            if (selected != null && selected.IsInAttackRange(distance))
            {
                if (ready)
                {
                    if (TryStartPattern(selected)) return;
                    // 실행 직전 조건이 바뀌었으면 다음 갱신에서 새 후보를 찾는다.
                    ClearSelection();
                }
                // 공격과 동일한 도착 오차를 사용한다. 쿨다운 대기 중에도 걷기를 끝낸다.
                if (CanContinueChase(target)) StopMovement();
                return;
            }
            if (!CanContinueChase(target)) return;
            // 쿨다운 중에도 선택한 패턴의 거리에서 멈추고, 바깥에 있을 때만 접근한다.
            _movement.Chase(target.position, selected?.AttackStartDistance ?? 0f, p_deltaTime);
            _context.SetMoving(_movement.IsMoving);
        }

        private void ClearSelection() { _selectedPattern = null; _selectedTarget = null; }

        private bool CanContinueChase(Transform p_target) => CanAct && _context.State == EBossState.Chase &&
            _context.Target == p_target && _detection.ResolvePlayer(p_target) != null;

        private float GetTargetDistance(Transform p_target)
        {
            Vector3 offset = p_target.position - _movement.Position;
            offset.y = 0f;
            return offset.magnitude;
        }

        // 조건 판단은 패턴에 맡기고, 추적 제어권과 보스 상태만 조정한다.
        public bool TryStartPattern(BossPattern p_pattern)
        {
            if (!CanAct || _startingPattern || _selectingPattern || p_pattern == null ||
                (_context.State != EBossState.Idle && _context.State != EBossState.Chase) || _patterns.IsRunning ||
                _detection.ResolvePlayer(_context.Target) == null) return false;
            if (!p_pattern.IsInAttackRange(GetTargetDistance(_context.Target))) return false;
            _startingPattern = true;
            try
            {
                ClearSelection();
                StopMovement();
                _context.ChangeState(EBossState.ExecutePattern);
                if (!CanAct || _context.State != EBossState.ExecutePattern) return false;
                return _patterns.TryStart(p_pattern);
            }
            finally
            {
                RestoreAfterPattern();
                _startingPattern = false;
            }
        }

        public void CancelPattern()
        {
            ClearSelection();
            _patterns?.Cancel();
            RestoreAfterPattern();
        }

        // 경직 발생 조건은 요청자가 판단한다. 재요청은 남은 시간을 줄이지 않고 연장한다.
        public bool TryStagger(float p_duration)
        {
            if (!CanAct || _context.State == EBossState.Dead || p_duration <= 0f ||
                float.IsNaN(p_duration) || float.IsInfinity(p_duration)) return false;
            ClearSelection();
            StopMovement();
            _staggerRemaining = Mathf.Max(_staggerRemaining, p_duration);
            // 취소 콜백이 패턴을 다시 시작하거나 추적 상태로 복귀하지 못하도록 먼저 전환한다.
            _context.ChangeState(EBossState.Stagger);
            _patterns.Cancel();
            return true;
        }

        private void RestoreAfterPattern()
        {
            if (_context == null || _context.State != EBossState.ExecutePattern || _patterns.IsRunning) return;
            RestoreLocomotion();
        }

        private void RestoreLocomotion()
        {
            if (_detection.ResolvePlayer(_context.Target) == null) _context.SetTarget(null);
            _context.ChangeState(CanAct && _context.Target != null ? EBossState.Chase : EBossState.Idle);
        }

        private void Suspend()
        {
            ClearSelection();
            _staggerRemaining = 0f;
            _patterns?.Cancel();
            StopMovement();
            _context?.SetTarget(null);
            _searchRemaining = 0f;
            if (_context != null && _context.State != EBossState.Dead) _context.ChangeState(EBossState.Idle);
        }
        private void StopMovement() { _movement?.Stop(); _context?.SetMoving(false); }
        private void HandleDeath()
        {
            ClearSelection();
            _staggerRemaining = 0f;
            _patterns?.Cancel();
            StopMovement();
            _context.SetTarget(null);
            _context.ChangeState(EBossState.Dead);
        }
        private void HandleCinematic(EBossCinematicState p_state)
        {
            if (p_state != EBossCinematicState.Completed) Suspend();
            else _searchRemaining = 0f;
        }
        public void Unbind()
        {
            if (_health != null) _health.OnDied -= HandleDeath;
            if (_cinematic != null) _cinematic.OnStateChanged -= HandleCinematic;
            _enabled = false;
            Suspend();
            _context = null; _health = null; _movement = null; _cinematic = null;
        }
        internal void Validate()
        {
            _detectionRadius = Mathf.Max(0.1f, Valid(_detectionRadius, 20f));
            _searchInterval = Mathf.Max(0.02f, Valid(_searchInterval, 0.25f));
        }
        private static float Valid(float p_value, float p_default) =>
            float.IsNaN(p_value) || float.IsInfinity(p_value) ? p_default : Mathf.Max(0f, p_value);
    }
}
