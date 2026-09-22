using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Boss
{
    // 상태는 읽기만 하고 Animator 재생만 소유한다. 시네마틱 대기·재생 중에는 제어권을 넘긴다.
    [DisallowMultipleComponent]
    public sealed class BossAnimationView : MonoBehaviour
    {
        [SerializeField, Tooltip("보스 모델의 Animator입니다. Rigidbody 이동과 별도로 표현만 담당합니다.")]
        private Animator _animator;
        [SerializeField, Tooltip("공격 상태 경로의 접두사입니다. 뒤에 설정 클립 이름을 붙입니다.")]
        private string _attackStatePrefix = "Base Layer.";
        [SerializeField, Tooltip("대기 상태의 Animator 경로입니다.")]
        private string _idleState = "Base Layer.Idle";
        [SerializeField, Tooltip("추적 이동 중 재생할 Animator 경로입니다.")]
        private string _chaseState = "Base Layer.Chase";
        [SerializeField, Tooltip("경직·그로기 상태의 Animator 경로입니다.")]
        private string _staggerState = "Base Layer.KnockDown";
        [SerializeField, Tooltip("사망 상태의 Animator 경로입니다.")]
        private string _deathState = "Base Layer.Death";
        [SerializeField, Min(0f), Tooltip("일반 상태로 전환할 때의 혼합 시간(초)입니다.")]
        private float _transitionDuration = 0.05f;

        private BossCore _boss;
        private BossCinematicContext _cinematic;
        private readonly HashSet<int> _missingStates = new();
        private bool _subscribed, _hasState, _playingPattern, _ownsSpeed;
        private int _stateHash;
        private float _previousSpeed;

        public void Bind(BossCore p_boss, BossCinematicContext p_cinematic)
        {
            Unbind();
            _boss = p_boss;
            _cinematic = p_cinematic;
            _animator ??= GetComponentInChildren<Animator>(true);
            if (isActiveAndEnabled) Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            ReleasePlayback();
            _boss = null;
            _cinematic = null;
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() { Unsubscribe(); ReleasePlayback(); }
        private void OnDestroy() => Unbind();
        // 초기화가 늦은 Animator와 View만 재활성화된 경우도 현재 상태로 복원한다.
        private void Update() => Refresh();

        private void Subscribe()
        {
            if (_subscribed || _boss == null) return;
            _boss.Context.OnStateChanged += HandleState;
            _boss.OnPatternPhaseChanged += HandlePattern;
            if (_cinematic != null) _cinematic.OnStateChanged += HandleCinematic;
            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_boss != null)
            {
                _boss.Context.OnStateChanged -= HandleState;
                _boss.OnPatternPhaseChanged -= HandlePattern;
            }
            if (_cinematic != null) _cinematic.OnStateChanged -= HandleCinematic;
            _subscribed = false;
        }

        private void HandleState(EBossState p_state) => Refresh();
        private void HandleCinematic(EBossCinematicState p_state) => Refresh();
        private void HandlePattern(BossPatternExecution p_execution)
        {
            if (p_execution.Phase == EBossPatternPhase.Prepare ||
                (_playingPattern && p_execution.Phase is EBossPatternPhase.Completed or EBossPatternPhase.Cancelled))
                ReleasePlayback();
            Refresh();
        }

        private void Refresh()
        {
            if (!isActiveAndEnabled || _boss == null || !_boss.isActiveAndEnabled ||
                (_cinematic != null && _cinematic.CurrentState != EBossCinematicState.Completed))
            {
                ReleasePlayback();
                return;
            }
            if (_animator == null || !_animator.isActiveAndEnabled || !_animator.isInitialized ||
                _animator.runtimeAnimatorController == null) return;

            if (_boss.Context.State == EBossState.ExecutePattern && _boss.ActivePattern.HasValue)
            {
                BossPatternExecution execution = _boss.ActivePattern.Value;
                if (execution.Phase == EBossPatternPhase.Active && execution.Definition != null &&
                    execution.Definition.ActiveDuration > 0f)
                {
                    PlayPattern(execution);
                    return;
                }
            }
            // 준비·후딜레이는 대기 모션으로 표현하며 공격 재생 속도를 유지하지 않는다.
            RestoreSpeed();
            _playingPattern = false;
            string state = _boss.Context.State switch
            {
                EBossState.Chase when _boss.Context.IsMoving => _chaseState,
                EBossState.Stagger => _staggerState,
                EBossState.Dead => _deathState,
                _ => _idleState
            };
            if (!TryGetState(state, out int hash) || (_hasState && _stateHash == hash)) return;
            _animator.CrossFadeInFixedTime(hash, _transitionDuration, 0, 0f);
            _stateHash = hash;
            _hasState = true;
        }

        private void PlayPattern(BossPatternExecution p_execution)
        {
            BossPatternDefinition definition = p_execution.Definition;
            if (definition == null || definition.AnimationClip == null) return;
            if (!TryGetState(_attackStatePrefix + definition.AnimationClip.name, out int hash)) return;
            if (_playingPattern && _hasState && _stateHash == hash) return;

            float duration = definition.ActiveDuration;
            float speed = definition.AnimationClip.length / duration;
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0f) speed = 1f;
            if (!_ownsSpeed) { _previousSpeed = _animator.speed; _ownsSpeed = true; }
            _animator.speed = speed;
            float activeElapsedTime = _boss.ActivePatternElapsedTime - definition.PrepareDuration;
            float normalizedTime = Mathf.Clamp01(activeElapsedTime / duration);
            // Active에서 한 번 시작한다. 복원 시에도 준비 시간을 제외한 실행 진행률을 사용한다.
            _animator.Play(hash, 0, normalizedTime);
            _stateHash = hash;
            _hasState = _playingPattern = true;
        }

        private bool TryGetState(string p_state, out int p_hash)
        {
            p_hash = Animator.StringToHash(p_state ?? string.Empty);
            if (!string.IsNullOrEmpty(p_state) && _animator.HasState(0, p_hash)) return true;
            if (_missingStates.Add(p_hash)) Debug.LogWarning($"Boss Animator 상태를 찾을 수 없습니다: {p_state}", this);
            return false;
        }

        private void RestoreSpeed()
        {
            if (_ownsSpeed && _animator != null) _animator.speed = _previousSpeed;
            _ownsSpeed = false;
        }

        private void ReleasePlayback()
        {
            RestoreSpeed();
            _hasState = _playingPattern = false;
        }

        private void OnValidate() => _transitionDuration = BossPatternSettings.NonNegative(_transitionDuration);
    }
}
