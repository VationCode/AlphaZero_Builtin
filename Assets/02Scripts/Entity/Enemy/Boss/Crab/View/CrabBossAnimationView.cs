using System;
using UnityEngine;

namespace Alpha.Enemy.Boss
{
    public class CrabBossAnimationView : MonoBehaviour
    {
        private static readonly int Attack1State = Animator.StringToHash("Base Layer.Attack1");
        private static readonly int Attack1Name = Animator.StringToHash("Attack1");
        private static readonly int Attack2State = Animator.StringToHash("Base Layer.Attack2");
        private static readonly int Attack2Name = Animator.StringToHash("Attack2");
        private static readonly int ChaseState = Animator.StringToHash("Base Layer.Chase");
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");

        [SerializeField] private Animator _animator;

        private bool _attackPlaying;
        private bool _attackEntered;
        private bool _attackHitSent;
        private bool _indicatorStartSent;
        private bool _isMoving;
        private int _framesWaitingForEntry;
        private int _activeAttackNameHash;
        private int _activeAttackStateHash;
        private float _hitStartNormalizedTime;
        private float _hitClipLengthSeconds;
        private float _hitElapsedAtAnimationEnd;
        private float _hitAnimationEndTime = float.NegativeInfinity;
        private float _hitStateSpeed;
        private bool _hitClockAvailable;
        private float _indicatorStartNormalizedTime;
        private float _indicatorHitNormalizedTime;
        private bool _indicatorClockAvailable;

        public event Action Attack1Hit;
        public event Action Attack2Hit;
        public event Action IndicatorStarted;
        public event Action<string> EffectStarted;
        public event Action<int> AttackAnimationStarted;
        public event Action<int> AttackAnimationEnded;
        public event Action Attack1Finished;
        public event Action Attack2Finished;
        public Vector3? AttackTargetPosition { get; private set; }

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }

        public void Bind(Animator animator)
        {
            CancelAttack();
            _animator = animator;
            _isMoving = false;
        }

        public void SetMoving(bool moving)
        {
            if (_attackPlaying || _isMoving == moving || _animator == null ||
                !_animator.isActiveAndEnabled)
                return;

            int state = moving ? ChaseState : IdleState;
            if (!_animator.HasState(0, state))
                return;

            _isMoving = moving;
            _animator.CrossFadeInFixedTime(state, 0.1f, 0);
        }

        public bool PlayAttack1() => PlayAttack(Attack1State, Attack1Name, null);

        public bool PlayAttack2() => PlayAttack(Attack2State, Attack2Name, null);

        // IndicatorStart에서 확정된 Attack2 위치를 타깃 위치 이펙트에 전달한다.
        public void SetAttack2TargetPosition(Vector3 targetPosition)
        {
            if (_attackPlaying && _activeAttackNameHash == Attack2Name)
                AttackTargetPosition = targetPosition;
        }

        private bool PlayAttack(int stateHash, int nameHash, Vector3? targetPosition)
        {
            if (_attackPlaying || _animator == null || !_animator.isActiveAndEnabled ||
                _animator.runtimeAnimatorController == null || !_animator.HasState(0, stateHash))
                return false;

            _attackPlaying = true;
            _isMoving = false;
            _attackEntered = false;
            _attackHitSent = false;
            _indicatorStartSent = false;
            ResetHitClock();
            ResetIndicatorClock();
            _framesWaitingForEntry = 0;
            _activeAttackStateHash = stateHash;
            _activeAttackNameHash = nameHash;
            AttackTargetPosition = targetPosition;
            _animator.Play(stateHash, 0, 0f);
            AttackAnimationStarted?.Invoke(nameHash);
            return true;
        }

        // 클립의 IndicatorStart 키를 현재 공격의 표시 시작 신호로 전달한다.
        public void IndicatorStart(AnimationEvent animationEvent)
        {
            if (!_attackPlaying || _indicatorStartSent)
                return;

            AnimationClip clip = animationEvent?.animatorClipInfo.clip;
            if (clip == null && _animator != null)
            {
                AnimatorClipInfo[] clips = _animator.GetCurrentAnimatorClipInfo(0);
                if (clips.Length > 0)
                    clip = clips[0].clip;
            }

            if (clip != null && animationEvent != null && clip.length > 0f)
            {
                float startTime = animationEvent.time;
                float hitTime = float.PositiveInfinity;
                foreach (AnimationEvent clipEvent in clip.events)
                {
                    if (clipEvent.functionName == nameof(HitStart) &&
                        clipEvent.time > startTime && clipEvent.time < hitTime)
                        hitTime = clipEvent.time;
                }

                if (!float.IsPositiveInfinity(hitTime))
                {
                    _indicatorStartNormalizedTime = startTime / clip.length;
                    _indicatorHitNormalizedTime = hitTime / clip.length;
                    _indicatorClockAvailable = true;
                }
            }

            _indicatorStartSent = true;
            IndicatorStarted?.Invoke();
        }

        // 이름을 IndicatorStar로 입력한 클립도 같은 신호로 처리한다.
        public void IndicatorStar(AnimationEvent animationEvent) => IndicatorStart(animationEvent);

        // 상태의 normalizedTime을 쓰므로 Animator Speed와 상태 Speed를 별도로 환산하지 않는다.
        public bool TryGetIndicatorProgress(out float progress)
        {
            progress = 0f;
            if (!_attackPlaying || !_indicatorStartSent || !_indicatorClockAvailable ||
                _animator == null || !_animator.isActiveAndEnabled)
                return false;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != _activeAttackStateHash)
                return false;

            progress = Mathf.Clamp01((state.normalizedTime - _indicatorStartNormalizedTime) /
                (_indicatorHitNormalizedTime - _indicatorStartNormalizedTime));
            return true;
        }

        // HitStart 키는 공격 판정 시점이다.
        public void HitStart(AnimationEvent animationEvent)
        {
            if (!_attackPlaying || _attackHitSent)
                return;

            AnimationClip clip = animationEvent?.animatorClipInfo.clip;
            if (clip == null && _animator != null)
            {
                AnimatorClipInfo[] clips = _animator.GetCurrentAnimatorClipInfo(0);
                if (clips.Length > 0)
                    clip = clips[0].clip;
            }

            _hitClipLengthSeconds = clip != null ? clip.length : 0f;
            _hitStartNormalizedTime = _hitClipLengthSeconds > 0f && animationEvent != null
                ? animationEvent.time / _hitClipLengthSeconds
                : _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            _hitClockAvailable = _hitClipLengthSeconds > 0f;
            _attackHitSent = true;
            if (_activeAttackNameHash == Attack1Name)
                Attack1Hit?.Invoke();
            else if (_activeAttackNameHash == Attack2Name)
                Attack2Hit?.Invoke();
        }

        // 클립 종료 후에도 남은 Hit Duration은 마지막 상태 속도로 진행한다.
        public bool TryGetHitElapsedClipSeconds(out float elapsedSeconds)
        {
            elapsedSeconds = 0f;
            if (!_hitClockAvailable || _animator == null || !_animator.isActiveAndEnabled)
                return false;

            if (!_attackPlaying)
            {
                if (float.IsNegativeInfinity(_hitAnimationEndTime))
                    return false;

                elapsedSeconds = _hitElapsedAtAnimationEnd +
                    Mathf.Max(0f, Time.time - _hitAnimationEndTime) *
                    _hitStateSpeed * Mathf.Abs(_animator.speed);
                return true;
            }

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != _activeAttackStateHash)
                return false;

            elapsedSeconds = Mathf.Max(0f,
                (state.normalizedTime - _hitStartNormalizedTime) * _hitClipLengthSeconds);
            _hitElapsedAtAnimationEnd = elapsedSeconds;
            _hitStateSpeed = Mathf.Abs(state.speed * state.speedMultiplier);
            return true;
        }

        // Unity 애니메이션 이벤트는 Function 이름으로 메서드를 찾는다.
        public void Effect1Start() => RaiseEffectStart(nameof(Effect1Start));
        public void Effect2Start() => RaiseEffectStart(nameof(Effect2Start));
        public void Effect3Start() => RaiseEffectStart(nameof(Effect3Start));
        public void Effect4Start() => RaiseEffectStart(nameof(Effect4Start));
        public void Effect5Start() => RaiseEffectStart(nameof(Effect5Start));
        public void Effect6Start() => RaiseEffectStart(nameof(Effect6Start));
        public void Effect7Start() => RaiseEffectStart(nameof(Effect7Start));
        public void Effect8Start() => RaiseEffectStart(nameof(Effect8Start));
        public void Effect9Start() => RaiseEffectStart(nameof(Effect9Start));
        public void Effect10Start() => RaiseEffectStart(nameof(Effect10Start));
        public void Effect11Start() => RaiseEffectStart(nameof(Effect11Start));
        public void Effect12Start() => RaiseEffectStart(nameof(Effect12Start));
        public void Effect13Start() => RaiseEffectStart(nameof(Effect13Start));
        public void Effect14Start() => RaiseEffectStart(nameof(Effect14Start));
        public void Effect15Start() => RaiseEffectStart(nameof(Effect15Start));
        public void Effect16Start() => RaiseEffectStart(nameof(Effect16Start));

        private void RaiseEffectStart(string eventName)
        {
            if (_attackPlaying)
                EffectStarted?.Invoke(eventName);
        }

        public void CancelAttack()
        {
            if (!_attackPlaying)
            {
                ResetHitClock();
                return;
            }

            EndAttackAnimation();
            ClearAttackTracking();

            if (_animator != null && _animator.isActiveAndEnabled && _animator.HasState(0, IdleState))
                _animator.Play(IdleState, 0, 0f);
        }

        public bool PlayDeath()
        {
            // 사망 시 공격을 중단하고 Idle을 거치지 않고 Death로 전환한다.
            EndAttackAnimation();
            ClearAttackTracking();
            _isMoving = false;

            if (_animator == null || !_animator.isActiveAndEnabled ||
                _animator.runtimeAnimatorController == null || !_animator.HasState(0, DeathState))
                return false;

            _animator.Play(DeathState, 0, 0f);
            return true;
        }

        private void LateUpdate()
        {
            if (!_attackPlaying)
                return;

            if (_animator == null || !_animator.isActiveAndEnabled)
            {
                FinishAttack(false);
                return;
            }

            AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.fullPathHash != _activeAttackStateHash)
            {
                // Play 직후 첫 프레임에는 이전 상태가 보일 수 있다.
                if (_attackEntered || ++_framesWaitingForEntry >= 2)
                    FinishAttack(false);
                return;
            }

            _attackEntered = true;

            // 다른 상태로 전환되면 남은 타격 판정을 실행하지 않는다.
            if (_animator.IsInTransition(0) &&
                _animator.GetNextAnimatorStateInfo(0).fullPathHash != _activeAttackStateHash)
            {
                FinishAttack(false);
                return;
            }

            if (_attackPlaying && currentState.normalizedTime >= 1f)
            {
                if (_animator.HasState(0, IdleState))
                    _animator.CrossFadeInFixedTime(IdleState, 0.1f, 0);

                FinishAttack(true);
            }
        }

        private void OnDisable()
        {
            CancelAttack();
        }

        private void FinishAttack(bool completedNaturally)
        {
            int finishedNameHash = _activeAttackNameHash;
            bool preserveHitClock = completedNaturally && _hitClockAvailable &&
                                    TryGetHitElapsedClipSeconds(out _);
            if (preserveHitClock)
                _hitAnimationEndTime = Time.time;
            EndAttackAnimation();
            ClearAttackTracking(preserveHitClock);
            if (finishedNameHash == Attack1Name)
                Attack1Finished?.Invoke();
            else if (finishedNameHash == Attack2Name)
                Attack2Finished?.Invoke();
        }

        private void EndAttackAnimation()
        {
            if (_activeAttackNameHash == 0)
                return;

            int attackNameHash = _activeAttackNameHash;
            _activeAttackNameHash = 0;
            AttackAnimationEnded?.Invoke(attackNameHash);
        }

        private void ClearAttackTracking(bool preserveHitClock = false)
        {
            _attackPlaying = false;
            _attackEntered = false;
            _attackHitSent = false;
            _indicatorStartSent = false;
            ResetIndicatorClock();
            if (!preserveHitClock)
                ResetHitClock();
            _framesWaitingForEntry = 0;
            _activeAttackNameHash = 0;
            _activeAttackStateHash = 0;
            AttackTargetPosition = null;
        }

        private void ResetHitClock()
        {
            _hitClockAvailable = false;
            _hitStartNormalizedTime = 0f;
            _hitClipLengthSeconds = 0f;
            _hitElapsedAtAnimationEnd = 0f;
            _hitAnimationEndTime = float.NegativeInfinity;
            _hitStateSpeed = 0f;
        }

        private void ResetIndicatorClock()
        {
            _indicatorClockAvailable = false;
            _indicatorStartNormalizedTime = 0f;
            _indicatorHitNormalizedTime = 0f;
        }
    }
}
