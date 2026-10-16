using Alpha.Detection;
using Alpha.Player;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Alpha.Enemy.Boss
{
    // 공격 후보를 가중치로 선택하고 예고, 타격, 회복 순서를 진행한다.
    public sealed class BossStateFlow : MonoBehaviour
    {
        private const int Attack1AreaId = 1;
        private const int Attack2AreaId = 2;
        private const int Attack1FillAreaId = 3;
        private const int Attack2FillAreaId = 4;
        private const int Attack3AreaId = 5;
        private const int Attack3FillAreaId = 6;
        private const int Attack4AreaId = 7;
        private const int Attack4FillAreaId = 8;
        private const float StartDistanceTolerance = 0.05f;
        private const float FallbackContactClearanceSpeed = 5f;

        private enum State
        {
            Waiting,
            Approaching,
            Preparing,
            Attacking,
            Recovering,
            Dead
        }

        private enum AttackKind
        {
            None,
            Attack1,
            Attack2,
            Attack3,
            Attack4
        }

        [SerializeField, Min(0f)] private float _prepareDuration = 0.3f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 1.2f;

        private TargetDetectionModule _targetDetection;
        private Transform _boss;
        private BossLocomotionModule _locomotion;
        private CrabBossAttackSelectionModule _selection;
        private CrabBossAttackModule _attack;
        private CrabBossAnimationView _animation;
        private CrabBossAttackAreaView _attackAreaView;
        private CrabBossCollisionView _collisionView;

        private Transform _target;
        private Transform _distanceTarget;
        private Collider _distanceTargetCollider;
        private Vector3 _aimPosition;
        private State _state = State.Waiting;
        private float _timeRemaining;
        private float _nextAttack1SelectionTime;
        private float _nextAttack2SelectionTime;
        private float _nextAttack3SelectionTime;
        private float _nextAttack4SelectionTime;
        private bool _isFacingLockedPosition;
        private bool _attack1HitApplied;
        private bool _attack2HitApplied;
        private bool _attack3HitApplied;
        private bool _attack4HitApplied;
        private bool _contactClearanceActive;
        private bool _attack2TargetLocked;
        private bool _attack3TargetLocked;
        private bool _attack4TargetLocked;
        private Vector3 _attack4Destination;
        private DetectionAreaRequest _attack4Area;
        private bool _indicatorStarted;
        private bool _hitWindowActive;
        private bool _attackAnimationFinished;
        private float _activeHitDurationSeconds;
        private AttackKind _selectedAttack;
        private AttackKind _previousAttack;
        private int _remainingAttackCount;
        private bool _bound;

        public void Bind(Transform boss, TargetDetectionModule targetDetection,
            BossLocomotionModule locomotion, CrabBossAttackSelectionModule selection,
            CrabBossAttackModule attack,
            CrabBossAnimationView animation, CrabBossAttackAreaView attackAreaView,
            CrabBossCollisionView collisionView)
        {
            Unbind();

            _boss = boss;
            _targetDetection = targetDetection;
            _locomotion = locomotion;
            _selection = selection;
            _attack = attack;
            _animation = animation;
            _attackAreaView = attackAreaView;
            _collisionView = collisionView;
            _bound = _boss != null && _targetDetection != null && _locomotion != null &&
                     _selection != null &&
                     _attack != null && _animation != null && _attackAreaView != null &&
                     _collisionView != null;

            if (!_bound)
                return;

            _animation.Attack1Hit += HandleAttack1Hit;
            _animation.Attack1Finished += HandleAttack1Finished;
            _animation.Attack2Hit += HandleAttack2Hit;
            _animation.Attack2Finished += HandleAttack2Finished;
            _animation.Attack3Hit += HandleAttack3Hit;
            _animation.Attack3Finished += HandleAttack3Finished;
            _animation.Attack4Hit += HandleAttack4Hit;
            _animation.Attack4Finished += HandleAttack4Finished;
            _animation.IndicatorStarted += HandleIndicatorStarted;
            _state = State.Waiting;
            _nextAttack1SelectionTime = 0f;
            _nextAttack2SelectionTime = 0f;
            _nextAttack3SelectionTime = 0f;
            _nextAttack4SelectionTime = 0f;
            _selectedAttack = AttackKind.None;
            _previousAttack = AttackKind.None;
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
            _attack4TargetLocked = false;
            _attackAnimationFinished = false;
            _contactClearanceActive = false;
        }

        public void Unbind()
        {
            CloseHitWindow();
            _attackAreaView?.HideAll();
            _collisionView?.SetContactBlocking(true);

            if (_animation != null)
            {
                _animation.Attack1Hit -= HandleAttack1Hit;
                _animation.Attack1Finished -= HandleAttack1Finished;
                _animation.Attack2Hit -= HandleAttack2Hit;
                _animation.Attack2Finished -= HandleAttack2Finished;
                _animation.Attack3Hit -= HandleAttack3Hit;
                _animation.Attack3Finished -= HandleAttack3Finished;
                _animation.Attack4Hit -= HandleAttack4Hit;
                _animation.Attack4Finished -= HandleAttack4Finished;
                _animation.IndicatorStarted -= HandleIndicatorStarted;
                _animation.CancelAttack();
            }

            _locomotion?.CancelJump();
            _locomotion?.CancelDash();
            _boss = null;
            _targetDetection = null;
            _locomotion = null;
            _selection = null;
            _attack = null;
            _animation = null;
            _attackAreaView = null;
            _collisionView = null;
            _target = null;
            _distanceTarget = null;
            _distanceTargetCollider = null;
            _bound = false;
            _state = State.Waiting;
            _selectedAttack = AttackKind.None;
            _previousAttack = AttackKind.None;
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
            _attack4TargetLocked = false;
            _attackAnimationFinished = false;
            _contactClearanceActive = false;
        }

        public void HandleDeath()
        {
            if (!_bound || _state == State.Dead)
                return;

            _state = State.Dead;
            CloseHitWindow();
            _collisionView.SetContactBlocking(true);
            _target = null;
            _selectedAttack = AttackKind.None;
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
            _attack4TargetLocked = false;
            _locomotion.CancelJump();
            _locomotion.CancelDash();
            _attackAreaView.HideAll();
            _animation.SetMoving(false);
            _animation.PlayDeath();
        }

        private void Update()
        {
            if (!_bound || _state == State.Dead)
                return;

            if (_hitWindowActive &&
                (!_animation.TryGetHitElapsedClipSeconds(out float hitElapsed) ||
                 hitElapsed >= _activeHitDurationSeconds))
                CloseHitWindow();

            // 클립이 먼저 끝나도 Hit Duration이 남아 있으면 반복·회복을 기다린다.
            if (_state == State.Attacking && _attackAnimationFinished && !_hitWindowActive)
            {
                FinishSelectedAttack();
                return;
            }

            if (!_targetDetection.TryGetTarget(out Transform target))
            {
                // 시작된 휘두르기는 끝까지 보여주고, 다음 판단에서 타깃 상실을 처리한다.
                if (_state == State.Attacking)
                {
                    _target = null;
                    return;
                }

                ReturnToWaiting();
                return;
            }

            _target = target;

            switch (_state)
            {
                case State.Waiting:
                case State.Approaching:
                    AttackKind selected = SelectAttack(_target);
                    if (selected != AttackKind.None)
                        BeginSelectedAttack(selected);
                    else
                        _state = State.Approaching;
                    break;

                case State.Preparing:
                    if (_isFacingLockedPosition)
                    {
                        _timeRemaining -= Time.deltaTime;
                        if (_timeRemaining <= 0f)
                            BeginAttack();
                    }
                    break;

                case State.Recovering:
                    _timeRemaining -= Time.deltaTime;
                    if (_timeRemaining <= 0f)
                        _state = State.Waiting;
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (_bound && (_contactClearanceActive ||
                           _collisionView.IsContactRestorePending))
            {
                if (!ClearPlayerContact())
                    _contactClearanceActive = false;
                if (_collisionView.IsContactRestorePending)
                    _collisionView.TryRestoreContactBlocking();
            }

            if (!_bound || _state == State.Dead ||
                (_target == null && _state != State.Attacking && _state != State.Recovering))
                return;

            switch (_state)
            {
                case State.Approaching:
                {
                    Vector3 offset = GetTargetContactPoint(_target) - _locomotion.Position;
                    offset.y = 0f;
                    float approachDistance = GetApproachDistance();
                    float startDistance = approachDistance + StartDistanceTolerance;
                    if (offset.sqrMagnitude > startDistance * startDistance)
                    {
                        _locomotion.MoveTo(_target.position, approachDistance);
                        _animation.SetMoving(true);
                    }
                    else
                    {
                        _locomotion.Stop();
                        _animation.SetMoving(false);
                    }

                    _locomotion.RotateTo(_target.position);
                    break;
                }

                case State.Preparing:
                    _locomotion.Stop();
                    if (_locomotion.RotateTo(_aimPosition) && !_isFacingLockedPosition)
                    {
                        _isFacingLockedPosition = true;
                        _timeRemaining = _prepareDuration;
                    }
                    break;

                case State.Attacking:
                {
                    if (_selectedAttack == AttackKind.Attack3 && _attack3TargetLocked &&
                        !_attack3HitApplied &&
                        _animation.TryGetIndicatorProgress(out float jumpProgress))
                        _locomotion.AdvanceJump(jumpProgress);
                    else if (_selectedAttack == AttackKind.Attack4 && _attack4TargetLocked &&
                             _attack4HitApplied &&
                             _animation.TryGetHitElapsedClipSeconds(out float dashElapsed))
                    {
                        float dashProgress = dashElapsed /
                            _attack.Attack4DashDurationSeconds;
                        _locomotion.AdvanceDash(dashProgress);
                        if (dashProgress >= 1f)
                            _locomotion.CompleteDash();
                    }
                    else
                        _locomotion.Stop();

                    // 위치를 고정하기 전에는 움직이는 타깃을 향한다.
                    bool targeting = (_selectedAttack == AttackKind.Attack2 && !_attack2TargetLocked) ||
                                     (_selectedAttack == AttackKind.Attack3 && !_attack3TargetLocked) ||
                                     (_selectedAttack == AttackKind.Attack4 && !_attack4TargetLocked);
                    Vector3 aim = targeting && _target != null
                        ? _target.position : _aimPosition;
                    if (_selectedAttack != AttackKind.Attack4 || !_attack4TargetLocked)
                        _locomotion.RotateTo(aim);
                    if (_hitWindowActive)
                    {
                        if (!_animation.TryGetHitElapsedClipSeconds(out float elapsed) ||
                            elapsed >= _activeHitDurationSeconds)
                            CloseHitWindow();
                        else
                            ApplySelectedAttack();
                    }
                    break;
                }

                case State.Recovering:
                    _locomotion.Stop();
                    if (_target != null)
                        _locomotion.RotateTo(_target.position);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (!_bound || _state == State.Dead)
                return;

            bool isWarning = _state == State.Attacking && _indicatorStarted &&
                             !SelectedHitApplied();
            if (!isWarning)
            {
                _attackAreaView.Hide(Attack1FillAreaId);
                _attackAreaView.Hide(Attack2FillAreaId);
                _attackAreaView.Hide(Attack3FillAreaId);
                _attackAreaView.Hide(Attack4FillAreaId);
                _attackAreaView.Hide(Attack1AreaId);
                _attackAreaView.Hide(Attack2AreaId);
                _attackAreaView.Hide(Attack3AreaId);
                _attackAreaView.Hide(Attack4AreaId);
                return;
            }

            if (_selectedAttack != AttackKind.Attack1)
            {
                _attackAreaView.Hide(Attack1AreaId);
                _attackAreaView.Hide(Attack1FillAreaId);
            }
            if (_selectedAttack != AttackKind.Attack2)
            {
                _attackAreaView.Hide(Attack2AreaId);
                _attackAreaView.Hide(Attack2FillAreaId);
            }
            if (_selectedAttack != AttackKind.Attack3)
            {
                _attackAreaView.Hide(Attack3AreaId);
                _attackAreaView.Hide(Attack3FillAreaId);
            }
            if (_selectedAttack != AttackKind.Attack4)
            {
                _attackAreaView.Hide(Attack4AreaId);
                _attackAreaView.Hide(Attack4FillAreaId);
            }

            // 이벤트 사이의 애니메이션 진행도에 따라 진한 영역만 확장한다.
            float progress = _animation.TryGetIndicatorProgress(out float value) ? value : 0f;
            if (ShowSelectedArea(progress))
                return;

            Debug.LogError("BossStateFlow: 공격 예고 장판을 갱신할 수 없습니다.", this);
            ReturnToWaiting();
            enabled = false;
        }

        private bool ShowSelectedArea(float progress) => _selectedAttack switch
        {
            AttackKind.Attack1 => ShowAttack1Warning(progress),
            AttackKind.Attack2 => ShowAttack2Warning(progress),
            AttackKind.Attack3 => ShowAttack3Warning(progress),
            AttackKind.Attack4 => ShowAttack4Warning(progress),
            _ => false
        };

        private bool ShowAttack1Warning(float progress) =>
            _attack.TryGetAttack1Area(out var request) &&
            ShowWarningArea(Attack1AreaId, Attack1FillAreaId, request,
                _attack.Attack1WarningColor, null, progress);

        private bool ShowAttack2Warning(float progress)
        {
            if (!_attack2TargetLocked ||
                !_attack.TryGetAttack2Area(_aimPosition, out var request))
                return false;

            Color warningColor = _attack.Attack2WarningColor;
            Color baseColor = warningColor;
            baseColor.a *= 0.3f;
            return ShowWarningArea(Attack2AreaId, Attack2FillAreaId, request,
                baseColor, warningColor, progress);
        }

        private bool ShowAttack3Warning(float progress)
        {
            if (!_attack3TargetLocked ||
                !_attack.TryGetAttack3Area(_aimPosition, out var request))
                return false;

            Color warningColor = _attack.Attack3WarningColor;
            Color baseColor = warningColor;
            baseColor.a *= 0.3f;
            return ShowWarningArea(Attack3AreaId, Attack3FillAreaId, request,
                baseColor, warningColor, progress, centeredBox: true);
        }

        private bool ShowAttack4Warning(float progress)
        {
            if (!_attack4TargetLocked)
                return false;

            Color warningColor = _attack.Attack4WarningColor;
            Color baseColor = warningColor;
            baseColor.a *= 0.3f;
            return ShowWarningArea(Attack4AreaId, Attack4FillAreaId, _attack4Area,
                baseColor, warningColor, progress);
        }

        private bool ShowWarningArea(int baseId, int fillId,
            in DetectionAreaRequest request, Color baseColor, Color? fillColor,
            float progress, bool centeredBox = false)
        {
            if (!_attackAreaView.Show(baseId, request, color: baseColor))
                return false;

            if (progress <= 0f)
            {
                _attackAreaView.Hide(fillId);
                return true;
            }

            return _attackAreaView.Show(fillId, request, color: fillColor,
                revealProgress: progress, sortingOrder: 1,
                revealFromCenter: centeredBox);
        }

        private bool SelectedHitApplied() => _selectedAttack switch
        {
            AttackKind.Attack1 => _attack1HitApplied,
            AttackKind.Attack2 => _attack2HitApplied,
            AttackKind.Attack3 => _attack3HitApplied,
            AttackKind.Attack4 => _attack4HitApplied,
            _ => true
        };

        // 가능한 공격만 후보에 넣고 가중치 비율로 하나를 선택한다.
        private AttackKind SelectAttack(Transform target)
        {
            AttackSelectionSettings attack1 = _selection.Attack1;
            AttackSelectionSettings attack2 = _selection.Attack2;
            AttackSelectionSettings attack3 = _selection.Attack3;
            AttackSelectionSettings attack4 = _selection.Attack4;
            float attack1Weight = CanSelectAttack(target, attack1, _nextAttack1SelectionTime)
                ? AdjustWeight(AttackKind.Attack1, attack1.Weight) : 0f;
            float attack2Weight = CanSelectAttack(target, attack2, _nextAttack2SelectionTime)
                ? AdjustWeight(AttackKind.Attack2, attack2.Weight) : 0f;
            float attack3Weight = CanSelectAttack(target, attack3, _nextAttack3SelectionTime)
                ? AdjustWeight(AttackKind.Attack3, attack3.Weight) : 0f;
            float attack4Weight = CanSelectAttack(target, attack4, _nextAttack4SelectionTime)
                ? AdjustWeight(AttackKind.Attack4, attack4.Weight) : 0f;

            // 다른 공격도 거리·재사용 시간 조건을 만족하면 먼저 타깃을 향해 돈다.
            if (attack3Weight > 0f && attack1Weight + attack2Weight + attack4Weight <= 0f &&
                HasReadyAlternativeAtCurrentDistance(target))
                return AttackKind.None;

            float totalWeight = attack1Weight + attack2Weight + attack3Weight + attack4Weight;
            if (totalWeight <= 0f)
                return AttackKind.None;

            float selection = Random.value * totalWeight;
            if (selection < attack1Weight)
                return AttackKind.Attack1;
            if (selection < attack1Weight + attack2Weight)
                return AttackKind.Attack2;
            if (selection < attack1Weight + attack2Weight + attack3Weight)
                return AttackKind.Attack3;
            // Random.value가 1을 반환해도 비활성 공격을 선택하지 않는다.
            return attack4Weight > 0f ? AttackKind.Attack4 :
                attack3Weight > 0f ? AttackKind.Attack3 :
                attack2Weight > 0f ? AttackKind.Attack2 : AttackKind.Attack1;
        }

        private float AdjustWeight(AttackKind attack, float weight) =>
            attack == _previousAttack ? weight * _selection.PreviousAttackWeightMultiplier : weight;

        private bool HasReadyAlternativeAtCurrentDistance(Transform target)
        {
            AttackSelectionSettings attack1 = _selection.Attack1;
            AttackSelectionSettings attack2 = _selection.Attack2;
            AttackSelectionSettings attack4 = _selection.Attack4;
            return (attack1.Weight > 0f && Time.time >= _nextAttack1SelectionTime &&
                    IsWithinSelectionDistance(target, attack1.MinDistance, attack1.MaxDistance)) ||
                   (attack2.Weight > 0f && Time.time >= _nextAttack2SelectionTime &&
                    IsWithinSelectionDistance(target, attack2.MinDistance, attack2.MaxDistance)) ||
                   (attack4.Weight > 0f && Time.time >= _nextAttack4SelectionTime &&
                    IsWithinSelectionDistance(target, attack4.MinDistance, attack4.MaxDistance));
        }

        private float GetApproachDistance()
        {
            // 선택 가능한 공격이 없을 때는 가까운 공격도 닿는 거리까지 접근한다.
            float distance = float.PositiveInfinity;
            AttackSelectionSettings attack1 = _selection.Attack1;
            AttackSelectionSettings attack2 = _selection.Attack2;
            AttackSelectionSettings attack3 = _selection.Attack3;
            AttackSelectionSettings attack4 = _selection.Attack4;
            if (attack1.Weight > 0f)
                distance = Mathf.Min(distance, attack1.MaxDistance);
            if (attack2.Weight > 0f)
                distance = Mathf.Min(distance, attack2.MaxDistance);
            if (attack3.Weight > 0f)
                distance = Mathf.Min(distance, attack3.MaxDistance);
            if (attack4.Weight > 0f)
                distance = Mathf.Min(distance, attack4.MaxDistance);
            return float.IsPositiveInfinity(distance) ? 0f : distance;
        }

        // 선택은 위치와 시간만 검사한다. 실제 명중은 타격 순간 AttackModule이 판정한다.
        private bool CanSelectAttack1(Transform target) =>
            CanSelectAttack(target, _selection.Attack1, _nextAttack1SelectionTime);

        private bool CanSelectAttack2(Transform target) =>
            CanSelectAttack(target, _selection.Attack2, _nextAttack2SelectionTime);

        private bool CanSelectAttack3(Transform target) =>
            CanSelectAttack(target, _selection.Attack3, _nextAttack3SelectionTime);

        private bool CanSelectAttack4(Transform target) =>
            CanSelectAttack(target, _selection.Attack4, _nextAttack4SelectionTime);

        private bool CanSelectAttack(Transform target, AttackSelectionSettings settings,
            float nextSelectionTime) =>
            target != null && settings.Weight > 0f && Time.time >= nextSelectionTime &&
            IsWithinSelectionArea(target, settings.MinDistance,
                settings.MaxDistance, settings.MaxAngle);

        private bool IsWithinSelectionArea(Transform target, float minimumDistance,
            float maximumDistance, float maximumAngle)
        {
            if (!IsWithinSelectionDistance(target, minimumDistance, maximumDistance))
                return false;

            Vector3 toTarget = target.position - _locomotion.Position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                return true;

            Vector3 forward = _locomotion.Forward;
            forward.y = 0f;
            return Vector3.Angle(forward, toTarget) <= maximumAngle;
        }

        private bool IsWithinSelectionDistance(Transform target, float minimumDistance,
            float maximumDistance)
        {
            Vector3 toContact = GetTargetContactPoint(target) - _locomotion.Position;
            toContact.y = 0f;
            float distanceSqr = toContact.sqrMagnitude;
            float startDistance = maximumDistance + StartDistanceTolerance;
            return distanceSqr >= minimumDistance * minimumDistance &&
                   distanceSqr <= startDistance * startDistance;
        }

        // 시작 거리에는 타깃의 몸 가장자리를 사용하고, 실제 피해는 AttackModule에서 판정한다.
        private Vector3 GetTargetContactPoint(Transform target)
        {
            if (_distanceTarget != target)
            {
                _distanceTarget = target;
                _distanceTargetCollider = target.GetComponent<CharacterController>();
                if (_distanceTargetCollider == null)
                    _distanceTargetCollider = target.GetComponent<Collider>();
            }

            if (_distanceTargetCollider == null || !_distanceTargetCollider.enabled)
                return target.position;

            // 발 높이에서는 캡슐의 둥근 끝을 재므로 몸통 높이에서 수평 경계를 찾는다.
            Vector3 probe = _locomotion.Position;
            probe.y = _distanceTargetCollider.bounds.center.y;
            return _distanceTargetCollider.ClosestPoint(probe);
        }

        private void BeginSelectedAttack(AttackKind attack)
        {
            _selectedAttack = attack;
            AttackSelectionSettings settings = attack switch
            {
                AttackKind.Attack1 => _selection.Attack1,
                AttackKind.Attack2 => _selection.Attack2,
                AttackKind.Attack3 => _selection.Attack3,
                AttackKind.Attack4 => _selection.Attack4,
                _ => default
            };
            _remainingAttackCount = attack == AttackKind.None ? 0 :
                RollRepeatCount(settings.MinRepeats, settings.MaxRepeats);
            BeginPreparation();
        }

        private static int RollRepeatCount(int minimum, int maximum)
        {
            minimum = Mathf.Max(1, minimum);
            maximum = Mathf.Max(minimum, maximum);
            // 정수 Random.Range의 상한은 제외되므로 한 칸 낮춰 뽑고 되돌린다.
            return minimum == maximum ? minimum : Random.Range(minimum - 1, maximum) + 1;
        }

        private void BeginPreparation()
        {
            CloseHitWindow();
            _locomotion.CancelJump();
            _locomotion.CancelDash();
            _animation.SetMoving(false);
            // Attack1은 여기서 조준하고, 대상 위치 공격은 IndicatorStart에서 확정한다.
            _aimPosition = _target.position;
            _attack1HitApplied = false;
            _attack2HitApplied = false;
            _attack3HitApplied = false;
            _attack4HitApplied = false;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
            _attack4TargetLocked = false;
            _attackAnimationFinished = false;
            _indicatorStarted = false;
            _isFacingLockedPosition = false;
            _timeRemaining = 0f;
            _state = State.Preparing;
        }

        private void BeginAttack()
        {
            bool started = _selectedAttack switch
            {
                AttackKind.Attack1 => _animation.PlayAttack1(),
                AttackKind.Attack2 => _animation.PlayAttack2(),
                AttackKind.Attack3 => _animation.PlayAttack3(),
                AttackKind.Attack4 => _animation.PlayAttack4(),
                _ => false
            };
            if (!started)
            {
                Debug.LogError("BossStateFlow: 선택한 공격 애니메이션을 재생할 수 없습니다.", this);
                ReturnToWaiting();
                enabled = false;
                return;
            }

            // 점프 중에도 지면 충돌은 유지하고 플레이어와의 몸체 충돌만 푼다.
            if (_selectedAttack == AttackKind.Attack3)
                _collisionView.SetContactBlocking(false, _target);
            _state = State.Attacking;
        }

        private void HandleIndicatorStarted()
        {
            if (_state != State.Attacking || SelectedHitApplied())
                return;

            if (_selectedAttack == AttackKind.Attack2 ||
                _selectedAttack == AttackKind.Attack3)
            {
                if (_target == null)
                    return;

                _aimPosition = _target.position;
                if (_selectedAttack == AttackKind.Attack2)
                    _attack2TargetLocked = true;
                else
                {
                    _attack3TargetLocked = true;
                    _locomotion.BeginJumpTo(_aimPosition);
                }
                _animation.SetAttackTargetPosition(_aimPosition);
            }
            else if (_selectedAttack == AttackKind.Attack4)
            {
                if (_target == null)
                    return;

                Vector3 start = _locomotion.Position;
                _aimPosition = _target.position;
                Vector3 direction = _aimPosition - start;
                direction.y = 0f;
                if (direction.sqrMagnitude <= 0.0001f)
                    return;

                if (!_attack.TryGetAttack4Area(start, _aimPosition,
                        out _attack4Area))
                    return;

                _attack4Destination = start + direction.normalized *
                    _attack4Area.Settings.Length;
                _attack4TargetLocked = true;
            }

            _indicatorStarted = true;
        }

        private void HandleAttack1Hit()
        {
            if (_state != State.Attacking || _selectedAttack != AttackKind.Attack1 ||
                _attack1HitApplied)
                return;

            _attack1HitApplied = true;
            _indicatorStarted = false;
            _attackAreaView.Hide(Attack1AreaId);
            _attackAreaView.Hide(Attack1FillAreaId);
            OpenHitWindow(_attack.Attack1HitTiming);
        }

        private void HandleAttack2Hit()
        {
            if (_state != State.Attacking || _selectedAttack != AttackKind.Attack2 ||
                _attack2HitApplied)
                return;

            _attack2HitApplied = true;
            _indicatorStarted = false;
            _attackAreaView.Hide(Attack2AreaId);
            _attackAreaView.Hide(Attack2FillAreaId);
            if (_attack2TargetLocked)
                OpenHitWindow(_attack.Attack2HitTiming);
        }

        private void HandleAttack3Hit()
        {
            if (_state != State.Attacking || _selectedAttack != AttackKind.Attack3 ||
                _attack3HitApplied)
                return;

            _attack3HitApplied = true;
            _indicatorStarted = false;
            _attackAreaView.Hide(Attack3AreaId);
            _attackAreaView.Hide(Attack3FillAreaId);
            if (!_attack3TargetLocked)
                return;

            _locomotion.CompleteJump();
            OpenHitWindow(_attack.Attack3HitTiming);
            _contactClearanceActive = true;
            ClearPlayerContact();
        }

        private void HandleAttack4Hit()
        {
            if (_state != State.Attacking || _selectedAttack != AttackKind.Attack4 ||
                _attack4HitApplied)
                return;

            _attack4HitApplied = true;
            _indicatorStarted = false;
            _attackAreaView.Hide(Attack4AreaId);
            _attackAreaView.Hide(Attack4FillAreaId);
            if (!_attack4TargetLocked)
                return;

            // 예고 중에는 고정한 방향만 보관하고, HitStart부터 실제로 이동한다.
            _collisionView.SetContactBlocking(false, _target);
            _locomotion.BeginDashTo(_attack4Destination);
            OpenHitWindow(_attack.Attack4HitTiming);
            _contactClearanceActive = true;
            ClearPlayerContact();
        }

        private bool ClearPlayerContact()
        {
            if (_collisionView == null ||
                !_collisionView.TryGetPlayerClearance(out CharacterController controller,
                    out Vector3 displacement))
                return false;

            // 피격 무적이나 누운 상태여도 Player의 충돌 이동 경로로 몸 밖으로 보낸다.
            PlayerCore player = controller.GetComponentInParent<PlayerCore>();
            // 실제 넉백이 진행 중이면 이동량을 중복 적용하지 않는다.
            if (player == null || player.IsKnockbackActive)
                return true;

            float speed = _selectedAttack == AttackKind.Attack3 &&
                _attack.TryGetAttack3KnockbackSpeed(out float knockbackSpeed)
                ? knockbackSpeed : FallbackContactClearanceSpeed;
            player.TryMoveForContactClearance(Vector3.ClampMagnitude(displacement,
                speed * Time.fixedDeltaTime));
            return true;
        }

        private void OpenHitWindow(CrabBossHitTiming timing)
        {
            _attack.BeginHitWindow();
            _hitWindowActive = timing.Mode == CrabBossHitMode.Continuous;
            _activeHitDurationSeconds = Mathf.Max(0f, timing.DurationSeconds);
            ApplySelectedAttack(); // HitStart 시점의 대상도 놓치지 않는다.
            if (!_hitWindowActive || _activeHitDurationSeconds <= 0f)
                CloseHitWindow();
        }

        private void ApplySelectedAttack()
        {
            switch (_selectedAttack)
            {
                case AttackKind.Attack1:
                    _attack.ApplyAttack1();
                    break;
                case AttackKind.Attack2 when _attack2TargetLocked:
                    _attack.ApplyAttack2(_aimPosition);
                    break;
                case AttackKind.Attack3 when _attack3TargetLocked:
                    _attack.ApplyAttack3(_aimPosition);
                    break;
                case AttackKind.Attack4 when _attack4TargetLocked:
                    _attack.ApplyAttack4(_attack4Area);
                    break;
            }
        }

        private void CloseHitWindow()
        {
            _hitWindowActive = false;
            _activeHitDurationSeconds = 0f;
            _attack?.EndHitWindow();
        }

        private void HandleAttack1Finished()
        {
            if (_state != State.Attacking || _selectedAttack != AttackKind.Attack1)
                return;

            _attackAreaView.Hide(Attack1AreaId);
            _attackAreaView.Hide(Attack1FillAreaId);
            _indicatorStarted = false;
            _attackAnimationFinished = true;
            if (_hitWindowActive && !_animation.TryGetHitElapsedClipSeconds(out _))
                CloseHitWindow();
            if (!_hitWindowActive)
                FinishSelectedAttack();
        }

        private void HandleAttack2Finished()
        {
            if (_state != State.Attacking || _selectedAttack != AttackKind.Attack2)
                return;

            _attackAreaView.Hide(Attack2AreaId);
            _attackAreaView.Hide(Attack2FillAreaId);
            _indicatorStarted = false;
            _attackAnimationFinished = true;
            if (_hitWindowActive && !_animation.TryGetHitElapsedClipSeconds(out _))
                CloseHitWindow();
            if (!_hitWindowActive)
                FinishSelectedAttack();
        }

        private void HandleAttack3Finished()
        {
            if (_state != State.Attacking || _selectedAttack != AttackKind.Attack3)
                return;

            _attackAreaView.Hide(Attack3AreaId);
            _attackAreaView.Hide(Attack3FillAreaId);
            _indicatorStarted = false;
            _attackAnimationFinished = true;
            if (!_attack3HitApplied)
                _locomotion.CancelJump();
            if (_hitWindowActive && !_animation.TryGetHitElapsedClipSeconds(out _))
                CloseHitWindow();
            if (!_hitWindowActive)
                FinishSelectedAttack();
        }

        private void HandleAttack4Finished()
        {
            if (_state != State.Attacking || _selectedAttack != AttackKind.Attack4)
                return;

            _attackAreaView.Hide(Attack4AreaId);
            _attackAreaView.Hide(Attack4FillAreaId);
            _indicatorStarted = false;
            _attackAnimationFinished = true;
            if (_attack4HitApplied && _attack4TargetLocked)
                _locomotion.CompleteDash();
            else
                _locomotion.CancelDash();
            if (!_hitWindowActive)
                FinishSelectedAttack();
        }

        private void FinishSelectedAttack()
        {
            CloseHitWindow();
            if (_selectedAttack == AttackKind.Attack3 ||
                _selectedAttack == AttackKind.Attack4)
                ClearPlayerContact();
            _collisionView.SetContactBlocking(true);
            _attackAnimationFinished = false;
            _locomotion.CancelJump();
            _locomotion.CancelDash();
            if (--_remainingAttackCount > 0)
            {
                if (_target == null)
                    ReturnToWaiting();
                else
                    BeginPreparation();
                return;
            }

            if (_selectedAttack == AttackKind.Attack1)
                _nextAttack1SelectionTime = Time.time + _selection.Attack1.Cooldown;
            else if (_selectedAttack == AttackKind.Attack2)
                _nextAttack2SelectionTime = Time.time + _selection.Attack2.Cooldown;
            else if (_selectedAttack == AttackKind.Attack3)
                _nextAttack3SelectionTime = Time.time + _selection.Attack3.Cooldown;
            else if (_selectedAttack == AttackKind.Attack4)
                _nextAttack4SelectionTime = Time.time + _selection.Attack4.Cooldown;

            _previousAttack = _selectedAttack;
            _selectedAttack = AttackKind.None;
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
            _attack4TargetLocked = false;
            _timeRemaining = _recoveryDuration;
            _state = State.Recovering;
        }

        private void ReturnToWaiting()
        {
            CloseHitWindow();
            _collisionView?.SetContactBlocking(true);
            if (_state == State.Waiting)
                return;

            _attackAreaView.HideAll();
            _animation.CancelAttack();
            _animation.SetMoving(false);
            _locomotion.CancelJump();
            _locomotion.CancelDash();
            _target = null;
            _distanceTarget = null;
            _distanceTargetCollider = null;
            _selectedAttack = AttackKind.None;
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
            _attack4TargetLocked = false;
            _attackAnimationFinished = false;
            _indicatorStarted = false;
            _timeRemaining = 0f;
            _state = State.Waiting;
        }

        private void OnDisable()
        {
            _attackAreaView?.HideAll();
            _collisionView?.SetContactBlocking(true);

            if (_bound && _state != State.Dead)
                ReturnToWaiting();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnValidate()
        {
            _prepareDuration = Mathf.Max(0f, _prepareDuration);
            _recoveryDuration = Mathf.Max(0f, _recoveryDuration);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Camera.current == null || Camera.current.cameraType != CameraType.SceneView)
                return;

            Transform origin = _boss != null ? _boss :
                GetComponentInParent<CrabBossCore>()?.transform;
            if (origin == null)
                return;

            CrabBossAttackModule attack = _attack != null ? _attack :
                origin.GetComponentInChildren<CrabBossAttackModule>(true);
            CrabBossAttackSelectionModule selection = _selection != null ? _selection :
                origin.GetComponentInChildren<CrabBossAttackSelectionModule>(true);
            if (attack == null || selection == null)
                return;

            Vector3 position = Application.isPlaying && _locomotion != null
                ? _locomotion.Position : origin.position;
            Vector3 forward = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();

            if (Application.isPlaying && _target != null && _locomotion != null)
            {
                Vector3 contactPoint = GetTargetContactPoint(_target);
                Vector3 offset = contactPoint - position;
                offset.y = 0f;
                Handles.Label(_target.position + Vector3.up * 1.5f,
                    $"Body distance: {offset.magnitude:0.##}m ({_state})\n" +
                    $"Conditions  1:{CanSelectAttack1(_target)}  " +
                    $"2:{CanSelectAttack2(_target)}  " +
                    $"3:{CanSelectAttack3(_target)}  " +
                    $"4:{CanSelectAttack4(_target)}");
            }

            if (attack.ShowAttack1HitArea)
                DrawAttack1RangeGizmo(attack, selection.Attack1, position);
            if (attack.ShowAttack2HitArea &&
                attack.TryGetAttack2Area(GetPreviewTargetPosition(position,
                    AttackKind.Attack2, _attack2TargetLocked), out var attack2Area))
                DrawHitAreaGizmo(attack2Area, attack.Attack2HitAreaColor,
                    Application.isPlaying ? "Attack2 hit" : "Attack2 hit sample", 1,
                    selection.Attack2.MinDistance, selection.Attack2.MaxDistance,
                    attack.Attack2HitAreaFillOpacity);
            if (attack.ShowAttack3HitArea &&
                attack.TryGetAttack3Area(GetPreviewTargetPosition(position,
                    AttackKind.Attack3, _attack3TargetLocked), out var attack3Area))
                DrawHitAreaGizmo(attack3Area, attack.Attack3HitAreaColor,
                    Application.isPlaying ? "Attack3 hit" : "Attack3 hit sample", 2,
                    selection.Attack3.MinDistance, selection.Attack3.MaxDistance,
                    attack.Attack3HitAreaFillOpacity);
            if (attack.ShowAttack4HitArea)
            {
                Vector3 aimPosition = position + forward;
                if (Application.isPlaying && _target != null)
                {
                    Vector3 toTarget = _target.position - position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f)
                        aimPosition = _target.position;
                }

                DetectionAreaRequest attack4Area = _attack4Area;
                if ((_selectedAttack == AttackKind.Attack4 && _attack4TargetLocked) ||
                    attack.TryGetAttack4Area(position, aimPosition, out attack4Area))
                    DrawHitAreaGizmo(attack4Area, attack.Attack4HitAreaColor,
                        Application.isPlaying ? "Attack4 hit path" :
                            "Attack4 fixed hit path sample", 3,
                        selection.Attack4.MinDistance, selection.Attack4.MaxDistance,
                        attack.Attack4HitAreaFillOpacity);
            }
        }

        private Vector3 GetPreviewTargetPosition(Vector3 fallback, AttackKind attack,
            bool locked)
        {
            if (Application.isPlaying && _selectedAttack == attack && locked)
                return _aimPosition;
            return Application.isPlaying && _target != null
                ? _target.position : fallback;
        }

        private static void DrawHitAreaGizmo(in DetectionAreaRequest request,
            Color color, string label, int labelRow,
            float minimumStopDistance, float maximumStopDistance,
            float fillOpacity)
        {
            if (!request.IsValid)
                return;

            DetectionAreaSettings settings = request.Settings;
            Vector3 center = request.AreaOrigin;
            center.y = request.Origin.y + 0.12f;
            Vector3 forward = Vector3.ProjectOnPlane(request.Forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();

            Color previousColor = Handles.color;
            Color fill = color;
            fill.a = Mathf.Clamp01(fillOpacity);
            Color outline = color;
            outline.a = Mathf.Max(outline.a, 0.7f);
            Handles.color = fill;

            switch (settings.Shape)
            {
                case EDetectionAreaShape.Radial:
                    Handles.DrawSolidDisc(center, Vector3.up, settings.Radius);
                    Handles.color = outline;
                    Handles.DrawWireDisc(center, Vector3.up, settings.Radius);
                    break;

                case EDetectionAreaShape.ForwardSector:
                    Vector3 left = Quaternion.AngleAxis(-settings.Angle * 0.5f,
                        Vector3.up) * forward;
                    Handles.DrawSolidArc(center, Vector3.up, left,
                        settings.Angle, settings.Radius);
                    Handles.color = outline;
                    Handles.DrawWireArc(center, Vector3.up, left,
                        settings.Angle, settings.Radius);
                    Handles.DrawLine(center, center + left * settings.Radius);
                    Handles.DrawLine(center, center +
                        Quaternion.AngleAxis(settings.Angle * 0.5f, Vector3.up) *
                        forward * settings.Radius);
                    break;

                case EDetectionAreaShape.ForwardBox:
                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                    float halfWidth = settings.Width * 0.5f;
                    Vector3[] corners =
                    {
                        center - right * halfWidth,
                        center + right * halfWidth,
                        center + right * halfWidth + forward * settings.Length,
                        center - right * halfWidth + forward * settings.Length
                    };
                    Handles.DrawSolidRectangleWithOutline(corners, fill, outline);
                    break;
            }

            Handles.color = outline;
            float reach = settings.Shape == EDetectionAreaShape.ForwardBox
                ? settings.Length : settings.Radius;
            string size = settings.Shape == EDetectionAreaShape.ForwardBox
                ? $"{settings.Width:0.#}×{settings.Length:0.#}m"
                : $"r{settings.Radius:0.#}m";
            Handles.Label(center + forward * Mathf.Min(reach, 12f) +
                Vector3.up * (0.5f + labelRow * 0.35f),
                $"{label}: {size}\npre-attack stop start/end: " +
                $"{minimumStopDistance:0.#}/{maximumStopDistance:0.#}m");
            Handles.color = previousColor;
        }

        private void DrawAttack1RangeGizmo(CrabBossAttackModule attack,
            AttackSelectionSettings selection, Vector3 position)
        {
            if (attack == null || !attack.TryGetAttack1Area(out var request) ||
                request.Settings.Shape != EDetectionAreaShape.ForwardSector)
                return;

            Vector3 forward = Vector3.ProjectOnPlane(request.Forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                return;
            forward.Normalize();

            Vector3 center = request.AreaOrigin;
            center.y = position.y + 0.12f;
            float radius = request.Settings.Radius;
            float angle = request.Settings.Angle;
            Vector3 left = Quaternion.AngleAxis(-angle * 0.5f, Vector3.up) * forward;
            Vector3 right = Quaternion.AngleAxis(angle * 0.5f, Vector3.up) * forward;

            Color previousColor = Handles.color;
            Color fill = attack.Attack1HitAreaColor;
            fill.a = Mathf.Clamp01(attack.Attack1HitAreaFillOpacity);
            Handles.color = fill;
            Handles.DrawSolidArc(center, Vector3.up, left, angle, radius);
            Color outlineColor = attack.Attack1HitAreaColor;
            outlineColor.a = Mathf.Max(outlineColor.a, 0.7f);
            Handles.color = outlineColor;
            Handles.DrawWireArc(center, Vector3.up, left, angle, radius);
            Handles.DrawLine(center, center + left * radius);
            Handles.DrawLine(center, center + right * radius);
            Handles.Label(center + forward * radius + Vector3.up * 0.3f,
                $"Attack1 hit: {radius:0.##}m / {angle:0.#}°\n" +
                $"pre-attack stop start/end: {selection.MinDistance:0.#}/" +
                $"{selection.MaxDistance:0.#}m");
            Handles.color = previousColor;
        }
#endif
    }
}
