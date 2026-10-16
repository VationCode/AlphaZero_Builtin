using Alpha.Detection;
using Alpha.Player;
using UnityEngine;
using UnityEngine.Serialization;

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
            Attack3
        }

        private enum HitMode
        {
            Once,
            Continuous
        }

        [SerializeField, Min(0f)] private float _prepareDuration = 0.3f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 1.2f;

        [Header("Attack1 Selection")]
        [Tooltip("타깃이 이 거리보다 가까우면 Attack1을 선택하지 않습니다.")]
        [SerializeField, Min(0f)] private float _attack1MinSelectionDistance;
        [Tooltip("보스 중심에서 타깃 충돌체 표면까지의 수평 거리입니다. 이 거리에서 추적을 멈추고, 다른 조건도 맞으면 공격을 시작합니다.")]
        [FormerlySerializedAs("_attack1MaxSelectionDistance")]
        [SerializeField, Min(0.01f)] private float _attack1StartDistance = 23f;
        [Tooltip("보스 정면에서 타깃까지 허용하는 최대 각도입니다.")]
        [SerializeField, Range(0f, 180f)] private float _attack1MaxSelectionAngle = 55f;
        [Tooltip("0이면 Attack1을 선택하지 않습니다. 후보가 하나인 동안 양수 값은 같은 결과입니다.")]
        [SerializeField, Min(0f)] private float _attack1SelectionWeight = 1f;
        [Tooltip("Attack1 종료 후 다시 선택하기까지의 시간입니다.")]
        [SerializeField, Min(0f)] private float _attack1Cooldown;
        [Tooltip("Attack1을 선택했을 때 연속 실행할 최소 횟수입니다.")]
        [SerializeField, Min(1)] private int _attack1MinRepeatCount = 1;
        [Tooltip("Attack1을 선택했을 때 연속 실행할 최대 횟수입니다. 최소 횟수와 같으면 고정됩니다.")]
        [SerializeField, Min(1)] private int _attack1MaxRepeatCount = 1;
        [Tooltip("Once는 HitStart에서 한 번, Continuous는 HitStart부터 Hit Duration 동안 판정합니다.")]
        [SerializeField] private HitMode _attack1HitMode = HitMode.Once;
        [Tooltip("HitStart 이후 판정을 유지할 클립 시간(초)입니다. Animator 재생 속도가 반영됩니다.")]
        [SerializeField, Min(0f)] private float _attack1HitDurationSeconds = 0.2f;
        [Tooltip("Attack1 시작 거리를 Scene 뷰 Gizmo로 표시합니다. Game 뷰에는 표시하지 않습니다.")]
        [FormerlySerializedAs("_showAttack1SelectionArea")]
        [SerializeField] private bool _showAttack1StartDistance = true;
        [FormerlySerializedAs("_attack1SelectionAreaColor")]
        [SerializeField] private Color _attack1StartDistanceColor = new(0.05f, 0.65f, 1f, 0.7f);

        [Tooltip("Attack1 피해 범위를 Scene 뷰 Gizmo로 표시합니다. Game 뷰에는 Indicator 예고만 표시합니다.")]
        [SerializeField] private bool _showAttack1Range = true;
        [SerializeField] private Color _attack1RangeColor = new(1f, 0.3f, 0.08f, 0.12f);

        [Header("Attack2 Selection")]
        [SerializeField, Min(0f)] private float _attack2MinSelectionDistance;
        [SerializeField, Min(0.01f)] private float _attack2MaxSelectionDistance = 17f;
        [SerializeField, Range(0f, 180f)] private float _attack2MaxSelectionAngle = 55f;
        [SerializeField, Min(0f)] private float _attack2SelectionWeight = 1f;
        [SerializeField, Min(0f)] private float _attack2Cooldown;
        [Tooltip("Attack2를 선택했을 때 연속 실행할 최소 횟수입니다.")]
        [SerializeField, Min(1)] private int _attack2MinRepeatCount = 1;
        [Tooltip("Attack2를 선택했을 때 연속 실행할 최대 횟수입니다. 최소 횟수와 같으면 고정됩니다.")]
        [SerializeField, Min(1)] private int _attack2MaxRepeatCount = 1;
        [Tooltip("Once는 HitStart에서 한 번, Continuous는 HitStart부터 Hit Duration 동안 판정합니다.")]
        [SerializeField] private HitMode _attack2HitMode = HitMode.Once;
        [Tooltip("HitStart 이후 판정을 유지할 클립 시간(초)입니다. Animator 재생 속도가 반영됩니다.")]
        [SerializeField, Min(0f)] private float _attack2HitDurationSeconds = 0.2f;
        [SerializeField] private Color _attack2WarningColor = new(0.35f, 0.45f, 1f, 0.5f);

        [Header("Attack3 Selection")]
        [SerializeField, Min(0f)] private float _attack3MinSelectionDistance = 4f;
        [SerializeField, Min(0.01f)] private float _attack3MaxSelectionDistance = 20f;
        [SerializeField, Range(0f, 180f)] private float _attack3MaxSelectionAngle = 180f;
        [SerializeField, Min(0f)] private float _attack3SelectionWeight = 1f;
        [SerializeField, Min(0f)] private float _attack3Cooldown = 2f;
        [SerializeField, Min(1)] private int _attack3MinRepeatCount = 1;
        [SerializeField, Min(1)] private int _attack3MaxRepeatCount = 1;
        [SerializeField] private HitMode _attack3HitMode = HitMode.Once;
        [SerializeField, Min(0f)] private float _attack3HitDurationSeconds = 0.2f;
        [SerializeField] private Color _attack3WarningColor = new(1f, 0.35f, 0.05f, 0.6f);

        private TargetDetectionModule _targetDetection;
        private Transform _boss;
        private BossLocomotionModule _locomotion;
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
        private bool _isFacingLockedPosition;
        private bool _attack1HitApplied;
        private bool _attack2HitApplied;
        private bool _attack3HitApplied;
        private bool _attack3ContactClearanceActive;
        private bool _attack2TargetLocked;
        private bool _attack3TargetLocked;
        private bool _indicatorStarted;
        private bool _hitWindowActive;
        private bool _attackAnimationFinished;
        private float _activeHitDurationSeconds;
        private AttackKind _selectedAttack;
        private int _remainingAttackCount;
        private bool _bound;

        public void Bind(Transform boss, TargetDetectionModule targetDetection,
            BossLocomotionModule locomotion, CrabBossAttackModule attack,
            CrabBossAnimationView animation, CrabBossAttackAreaView attackAreaView,
            CrabBossCollisionView collisionView)
        {
            Unbind();

            _boss = boss;
            _targetDetection = targetDetection;
            _locomotion = locomotion;
            _attack = attack;
            _animation = animation;
            _attackAreaView = attackAreaView;
            _collisionView = collisionView;
            _bound = _boss != null && _targetDetection != null && _locomotion != null &&
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
            _animation.IndicatorStarted += HandleIndicatorStarted;
            _state = State.Waiting;
            _nextAttack1SelectionTime = 0f;
            _nextAttack2SelectionTime = 0f;
            _nextAttack3SelectionTime = 0f;
            _selectedAttack = AttackKind.None;
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
            _attackAnimationFinished = false;
            _attack3ContactClearanceActive = false;
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
                _animation.IndicatorStarted -= HandleIndicatorStarted;
                _animation.CancelAttack();
            }

            _locomotion?.CancelJump();
            _boss = null;
            _targetDetection = null;
            _locomotion = null;
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
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
            _attackAnimationFinished = false;
            _attack3ContactClearanceActive = false;
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
            _locomotion.CancelJump();
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
            if (_bound && (_attack3ContactClearanceActive ||
                           _collisionView.IsContactRestorePending))
            {
                if (!ClearPlayerContact())
                    _attack3ContactClearanceActive = false;
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
                    else
                        _locomotion.Stop();

                    // 위치를 고정하기 전에는 움직이는 타깃을 향한다.
                    bool targeting = (_selectedAttack == AttackKind.Attack2 && !_attack2TargetLocked) ||
                                     (_selectedAttack == AttackKind.Attack3 && !_attack3TargetLocked);
                    Vector3 aim = targeting && _target != null
                        ? _target.position : _aimPosition;
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
                _attackAreaView.Hide(Attack1AreaId);
                _attackAreaView.Hide(Attack2AreaId);
                _attackAreaView.Hide(Attack3AreaId);
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
            _ => false
        };

        private bool ShowAttack1Warning(float progress) =>
            _attack.TryGetAttack1Area(out var request) &&
            ShowWarningArea(Attack1AreaId, Attack1FillAreaId, request,
                _attack1RangeColor, null, progress);

        private bool ShowAttack2Warning(float progress)
        {
            if (!_attack2TargetLocked ||
                !_attack.TryGetAttack2Area(_aimPosition, out var request))
                return false;

            Color baseColor = _attack2WarningColor;
            baseColor.a *= 0.3f;
            return ShowWarningArea(Attack2AreaId, Attack2FillAreaId, request,
                baseColor, _attack2WarningColor, progress);
        }

        private bool ShowAttack3Warning(float progress)
        {
            if (!_attack3TargetLocked ||
                !_attack.TryGetAttack3Area(_aimPosition, out var request))
                return false;

            Color baseColor = _attack3WarningColor;
            baseColor.a *= 0.3f;
            return ShowWarningArea(Attack3AreaId, Attack3FillAreaId, request,
                baseColor, _attack3WarningColor, progress, centeredBox: true);
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
            _ => true
        };

        // 가능한 공격만 후보에 넣고 가중치 비율로 하나를 선택한다.
        private AttackKind SelectAttack(Transform target)
        {
            float attack1Weight = CanSelectAttack1(target) ? _attack1SelectionWeight : 0f;
            float attack2Weight = CanSelectAttack2(target) ? _attack2SelectionWeight : 0f;
            float attack3Weight = CanSelectAttack3(target) ? _attack3SelectionWeight : 0f;
            float totalWeight = attack1Weight + attack2Weight + attack3Weight;
            if (totalWeight <= 0f)
                return AttackKind.None;

            float selection = Random.value * totalWeight;
            if (selection < attack1Weight)
                return AttackKind.Attack1;
            if (selection < attack1Weight + attack2Weight)
                return AttackKind.Attack2;
            // Random.value가 1을 반환해도 비활성 공격을 선택하지 않는다.
            return attack3Weight > 0f ? AttackKind.Attack3 :
                attack2Weight > 0f ? AttackKind.Attack2 : AttackKind.Attack1;
        }

        private float GetApproachDistance()
        {
            float attack1Distance = _attack1SelectionWeight > 0f
                ? _attack1StartDistance : 0f;
            float attack2Distance = _attack2SelectionWeight > 0f
                ? _attack2MaxSelectionDistance : 0f;
            float attack3Distance = _attack3SelectionWeight > 0f
                ? _attack3MaxSelectionDistance : 0f;
            return Mathf.Max(attack1Distance, attack2Distance, attack3Distance);
        }

        // 선택은 위치와 시간만 검사한다. 실제 명중은 타격 순간 AttackModule이 판정한다.
        private bool CanSelectAttack1(Transform target)
        {
            if (target == null || _attack1SelectionWeight <= 0f ||
                Time.time < _nextAttack1SelectionTime)
                return false;

            return IsWithinSelectionArea(target, _attack1MinSelectionDistance,
                _attack1StartDistance, _attack1MaxSelectionAngle);
        }

        private bool CanSelectAttack2(Transform target)
        {
            if (target == null || _attack2SelectionWeight <= 0f ||
                Time.time < _nextAttack2SelectionTime)
                return false;

            return IsWithinSelectionArea(target, _attack2MinSelectionDistance,
                _attack2MaxSelectionDistance, _attack2MaxSelectionAngle);
        }

        private bool CanSelectAttack3(Transform target)
        {
            if (target == null || _attack3SelectionWeight <= 0f ||
                Time.time < _nextAttack3SelectionTime)
                return false;

            return IsWithinSelectionArea(target, _attack3MinSelectionDistance,
                _attack3MaxSelectionDistance, _attack3MaxSelectionAngle);
        }

        private bool IsWithinSelectionArea(Transform target, float minimumDistance,
            float maximumDistance, float maximumAngle)
        {
            Vector3 toContact = GetTargetContactPoint(target) - _locomotion.Position;
            toContact.y = 0f;
            float distanceSqr = toContact.sqrMagnitude;
            float startDistance = maximumDistance + StartDistanceTolerance;
            if (distanceSqr < minimumDistance * minimumDistance ||
                distanceSqr > startDistance * startDistance)
                return false;

            Vector3 toTarget = target.position - _locomotion.Position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                return true;

            Vector3 forward = _locomotion.Forward;
            forward.y = 0f;
            return Vector3.Angle(forward, toTarget) <= maximumAngle;
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
            _remainingAttackCount = attack switch
            {
                AttackKind.Attack1 => RollRepeatCount(_attack1MinRepeatCount, _attack1MaxRepeatCount),
                AttackKind.Attack2 => RollRepeatCount(_attack2MinRepeatCount, _attack2MaxRepeatCount),
                AttackKind.Attack3 => RollRepeatCount(_attack3MinRepeatCount, _attack3MaxRepeatCount),
                _ => 0
            };
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
            _animation.SetMoving(false);
            // Attack1은 여기서 조준하고, 대상 위치 공격은 IndicatorStart에서 확정한다.
            _aimPosition = _target.position;
            _attack1HitApplied = false;
            _attack2HitApplied = false;
            _attack3HitApplied = false;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
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
                _ => false
            };
            if (!started)
            {
                Debug.LogError("BossStateFlow: 선택한 공격 애니메이션을 재생할 수 없습니다.", this);
                ReturnToWaiting();
                enabled = false;
                return;
            }

            // 점프 공격 중에는 대상이 보스의 접촉 구체 안으로 들어올 수 있다.
            if (_selectedAttack == AttackKind.Attack3)
                _collisionView.SetContactBlocking(false);
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
            OpenHitWindow(_attack1HitMode, _attack1HitDurationSeconds);
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
                OpenHitWindow(_attack2HitMode, _attack2HitDurationSeconds);
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
            OpenHitWindow(_attack3HitMode, _attack3HitDurationSeconds);
            _attack3ContactClearanceActive = true;
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

            float speed = _attack.TryGetAttack3KnockbackSpeed(out float knockbackSpeed)
                ? knockbackSpeed : FallbackContactClearanceSpeed;
            player.TryMoveForContactClearance(Vector3.ClampMagnitude(displacement,
                speed * Time.fixedDeltaTime));
            return true;
        }

        private void OpenHitWindow(HitMode mode, float durationSeconds)
        {
            _attack.BeginHitWindow();
            _hitWindowActive = mode == HitMode.Continuous;
            _activeHitDurationSeconds = Mathf.Max(0f, durationSeconds);
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

        private void FinishSelectedAttack()
        {
            CloseHitWindow();
            if (_selectedAttack == AttackKind.Attack3)
                ClearPlayerContact();
            _collisionView.SetContactBlocking(true);
            _attackAnimationFinished = false;
            _locomotion.CancelJump();
            if (--_remainingAttackCount > 0)
            {
                if (_target == null)
                    ReturnToWaiting();
                else
                    BeginPreparation();
                return;
            }

            if (_selectedAttack == AttackKind.Attack1)
                _nextAttack1SelectionTime = Time.time + _attack1Cooldown;
            else if (_selectedAttack == AttackKind.Attack2)
                _nextAttack2SelectionTime = Time.time + _attack2Cooldown;
            else if (_selectedAttack == AttackKind.Attack3)
                _nextAttack3SelectionTime = Time.time + _attack3Cooldown;

            _selectedAttack = AttackKind.None;
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
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
            _target = null;
            _distanceTarget = null;
            _distanceTargetCollider = null;
            _selectedAttack = AttackKind.None;
            _remainingAttackCount = 0;
            _attack2TargetLocked = false;
            _attack3TargetLocked = false;
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
            _attack1MinSelectionDistance = Mathf.Max(0f, _attack1MinSelectionDistance);
            _attack1StartDistance = Mathf.Max(_attack1MinSelectionDistance + 0.01f,
                _attack1StartDistance);
            _attack1MaxSelectionAngle = Mathf.Clamp(_attack1MaxSelectionAngle, 0f, 180f);
            _attack1SelectionWeight = Mathf.Max(0f, _attack1SelectionWeight);
            _attack1Cooldown = Mathf.Max(0f, _attack1Cooldown);
            _attack1MinRepeatCount = Mathf.Max(1, _attack1MinRepeatCount);
            _attack1MaxRepeatCount = Mathf.Max(_attack1MinRepeatCount, _attack1MaxRepeatCount);
            _attack1HitDurationSeconds = Mathf.Max(0f, _attack1HitDurationSeconds);
            _attack2MinSelectionDistance = Mathf.Max(0f, _attack2MinSelectionDistance);
            _attack2MaxSelectionDistance = Mathf.Max(
                _attack2MinSelectionDistance + 0.01f, _attack2MaxSelectionDistance);
            _attack2MaxSelectionAngle = Mathf.Clamp(_attack2MaxSelectionAngle, 0f, 180f);
            _attack2SelectionWeight = Mathf.Max(0f, _attack2SelectionWeight);
            _attack2Cooldown = Mathf.Max(0f, _attack2Cooldown);
            _attack2MinRepeatCount = Mathf.Max(1, _attack2MinRepeatCount);
            _attack2MaxRepeatCount = Mathf.Max(_attack2MinRepeatCount, _attack2MaxRepeatCount);
            _attack2HitDurationSeconds = Mathf.Max(0f, _attack2HitDurationSeconds);
            _attack3MinSelectionDistance = Mathf.Max(0f, _attack3MinSelectionDistance);
            _attack3MaxSelectionDistance = Mathf.Max(
                _attack3MinSelectionDistance + 0.01f, _attack3MaxSelectionDistance);
            _attack3MaxSelectionAngle = Mathf.Clamp(_attack3MaxSelectionAngle, 0f, 180f);
            _attack3SelectionWeight = Mathf.Max(0f, _attack3SelectionWeight);
            _attack3Cooldown = Mathf.Max(0f, _attack3Cooldown);
            _attack3MinRepeatCount = Mathf.Max(1, _attack3MinRepeatCount);
            _attack3MaxRepeatCount = Mathf.Max(_attack3MinRepeatCount, _attack3MaxRepeatCount);
            _attack3HitDurationSeconds = Mathf.Max(0f, _attack3HitDurationSeconds);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Camera.current == null || Camera.current.cameraType != CameraType.SceneView)
                return;

            if ((!_showAttack1StartDistance || _attack1StartDistance <= 0f) &&
                !_showAttack1Range)
                return;

            Transform origin = _boss != null ? _boss :
                GetComponentInParent<CrabBossCore>()?.transform;
            if (origin == null)
                return;

            Vector3 position = Application.isPlaying && _locomotion != null
                ? _locomotion.Position : origin.position;
            if (_showAttack1StartDistance && _attack1StartDistance > 0f)
            {
                Vector3 center = position + Vector3.up * 0.1f;
                Color previousColor = Handles.color;
                Handles.color = _attack1StartDistanceColor;
                Handles.DrawWireDisc(center, Vector3.up, _attack1StartDistance);
                Handles.Label(center + Vector3.forward * _attack1StartDistance + Vector3.up * 0.3f,
                    $"Attack1 start: {_attack1StartDistance:0.##}m");
                if (Application.isPlaying && _target != null)
                {
                    Vector3 contactPoint = GetTargetContactPoint(_target);
                    Vector3 offset = contactPoint - position;
                    offset.y = 0f;
                    Handles.Label(_target.position + Vector3.up * 1.5f,
                        $"Body distance: {offset.magnitude:0.##}m ({_state})");
                }
                Handles.color = previousColor;
            }

            if (_showAttack1Range)
                DrawAttack1RangeGizmo(origin, position);
        }

        private void DrawAttack1RangeGizmo(Transform origin, Vector3 position)
        {
            CrabBossAttackModule attack = _attack != null ? _attack :
                origin.GetComponentInChildren<CrabBossAttackModule>(true);
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
            Handles.color = _attack1RangeColor;
            Handles.DrawSolidArc(center, Vector3.up, left, angle, radius);
            Color outlineColor = _attack1RangeColor;
            outlineColor.a = Mathf.Max(outlineColor.a, 0.7f);
            Handles.color = outlineColor;
            Handles.DrawWireArc(center, Vector3.up, left, angle, radius);
            Handles.DrawLine(center, center + left * radius);
            Handles.DrawLine(center, center + right * radius);
            Handles.Label(center + forward * radius + Vector3.up * 0.3f,
                $"Attack1 hit: {radius:0.##}m / {angle:0.#}°");
            Handles.color = previousColor;
        }
#endif
    }
}
