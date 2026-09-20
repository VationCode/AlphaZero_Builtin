using System;
using UnityEngine;

namespace Alpha.Boss
{
    // Boss의 기본 이동·사망 표현과 Cinematic의 Animator 제어권을 관리한다.
    public sealed class BossAnimationView : MonoBehaviour
    {
        private const int BaseLayer = 0;

        [SerializeField] private Animator _animator;

        [SerializeField]
        [Tooltip("Animator 기준 이동 뼈대 경로입니다. Crab은 root. 비우면 Animator Root Motion을 사용합니다. Use Root Motion인 Rush에서 이 뼈대의 수평 이동을 본체로 옮깁니다.")]
        private string _rootMotionBonePath = string.Empty;

        [Header("Locomotion States")]
        [SerializeField] private string _idleStatePath = "Base Layer.Idle";
        [SerializeField, HideInInspector] private string _idleStateId;
        [SerializeField] private string _chaseStatePath = "Base Layer.Chase";
        [SerializeField, HideInInspector] private string _chaseStateId;
        [SerializeField] private string _deathStatePath = "Base Layer.Death";
        [SerializeField, HideInInspector] private string _deathStateId;

        [Header("Debug")]
        [SerializeField] private bool _logCrossFadeRequests;

        private Rigidbody _rigidbody;
        private bool _isBound;
        private bool _isCombat;
        private bool _isDead;
        private bool _isCinematicPlaying;
        private int _currentBaseState;
        private bool _hasCurrentBaseState;
        private int _pendingBaseState;
        private string _pendingBaseStatePath;
        private float _pendingTransitionDuration;
        private bool _pendingRestart;
        private bool _hasPendingBaseState;
        private RuntimeAnimatorController _lastController;

        public event Action<long, float, float> OnAttackProgress;
        public event Action<long> OnAttackCompleted;
        public event Action<long> OnAttackInterrupted;
        private long _attackId;
        private int _attackStateHash;
        private int _attackRequestFrame;
        private bool _hasEnteredAttack;
        private float _attackStartWait;
        private RuntimeAnimatorController _attackController;
        private readonly BossRootMotionSampler _rootMotionSampler = new();
        private long _rushMotionId;
        private Transform _rootMotionBone;
        private Vector3 _rootMotionBoneOrigin;
        private bool _controlsRootMotionBone;
        private bool _releasingRootMotionBone;
        private int _rootMotionBoneStateHash;

        // Rush 전체 구간의 Transform 이동은 Rigidbody가 소유한다. 준비/종료 동작도 중복 이동하지 않는다.
        public bool SetRushMotionControlled(long p_attackId, bool p_useRootMotion = false)
        {
            if (!ResolveAnimator() || _animator.gameObject != gameObject)
            {
                Debug.LogWarning($"[{name}] Rush의 이동 제어에는 Animator와 같은 객체의 BossAnimationView가 필요합니다.", this);
                return false;
            }
            _rushMotionId = p_attackId;
            if (p_useRootMotion && !string.IsNullOrEmpty(_rootMotionBonePath))
            {
                if (_rootMotionBone == null)
                    ResolveRootMotionBone();
                if (_rootMotionBone == null)
                {
                    Debug.LogWarning($"[{name}] Root Motion Bone Path를 찾을 수 없습니다: {_rootMotionBonePath}", this);
                    return false;
                }
                _controlsRootMotionBone = true;
                _releasingRootMotionBone = false;
                _rootMotionBoneStateHash = _attackStateHash;
            }
            return true;
        }

        public void ReleaseRushMotion(long p_attackId)
        {
            if (_rushMotionId == p_attackId)
            {
                _rushMotionId = 0;
                _releasingRootMotionBone = _controlsRootMotionBone;
            }
        }

        public AnimationCurve SampleRushRootMotion(float p_startSeconds, float p_durationSeconds)
        {
            if (!ResolveAnimator() || _attackId == 0)
                return null;
            bool next = _animator.IsInTransition(BaseLayer) &&
                _animator.GetNextAnimatorStateInfo(BaseLayer).fullPathHash == _attackStateHash;
            AnimatorStateInfo state = next ? _animator.GetNextAnimatorStateInfo(BaseLayer) :
                _animator.GetCurrentAnimatorStateInfo(BaseLayer);
            AnimatorClipInfo[] clips = next ? _animator.GetNextAnimatorClipInfo(BaseLayer) :
                _animator.GetCurrentAnimatorClipInfo(BaseLayer);
            AnimationCurve curve = null;
            // 단일 Clip 상태만 전체 이동량을 확정할 수 있다. 동적으로 변하는 BlendTree는 기존 곡선을 사용한다.
            if (clips.Length == 1 && clips[0].clip != null && state.length > 0f)
            {
                float scale = clips[0].clip.length / state.length;
                curve = _rootMotionSampler.Sample(_animator, clips[0].clip,
                    p_startSeconds * scale, p_durationSeconds * scale,
                    _controlsRootMotionBone ? _rootMotionBonePath : string.Empty);
            }
            if (curve == null)
                Debug.LogWarning($"[{name}] Rush 구간의 수평 Root Motion을 읽을 수 없어 Movement Curve를 사용합니다. Clip의 Root Motion과 이동 시간 구간을 확인하세요.", this);
            return curve;
        }

        private void OnAnimatorMove()
        {
            if (_animator != null && _animator.gameObject == gameObject && _rushMotionId == 0 &&
                _animator.applyRootMotion)
                _animator.ApplyBuiltinRootMotion();
            CorrectRootMotionBone();
        }

        private void ResolveRootMotionBone()
        {
            _rootMotionBone = ResolveAnimator() && !string.IsNullOrEmpty(_rootMotionBonePath)
                ? _animator.transform.Find(_rootMotionBonePath) : null;
            if (_rootMotionBone != null)
                _rootMotionBoneOrigin = _animator.transform.InverseTransformPoint(_rootMotionBone.position);
        }

        // 뼈대의 수평 이동만 제거한다. Y와 회전, 하위 관절의 박치기 동작은 그대로 표현한다.
        private void CorrectRootMotionBone()
        {
            if (!_controlsRootMotionBone || _rootMotionBone == null || _animator == null)
                return;
            if (_releasingRootMotionBone &&
                _animator.GetCurrentAnimatorStateInfo(BaseLayer).fullPathHash != _rootMotionBoneStateHash &&
                (!_animator.IsInTransition(BaseLayer) ||
                 _animator.GetNextAnimatorStateInfo(BaseLayer).fullPathHash != _rootMotionBoneStateHash))
            {
                _controlsRootMotionBone = false;
                _releasingRootMotionBone = false;
                return;
            }
            Vector3 position = _animator.transform.InverseTransformPoint(_rootMotionBone.position);
            position.x = _rootMotionBoneOrigin.x;
            position.z = _rootMotionBoneOrigin.z;
            _rootMotionBone.position = _animator.transform.TransformPoint(position);
        }

        public Animator Animator
        {
            get
            {
                ResolveAnimator();
                return _animator;
            }
        }

        private void Awake() => ResolveAnimator();

        public void Bind(Rigidbody p_rigidbody)
        {
            ResolveRootMotionBone();
            _rigidbody = p_rigidbody;
            _isBound = true;
            _isCombat = false;
            _isDead = false;
        }

        // 연출 전에는 Animator의 기본 재생을 유지하고 전투 중에만 이동 표현을 갱신한다.
        public void SetEncounterState(EBossEncounterState p_state)
        {
            _isCombat = p_state == EBossEncounterState.Combat;
            _hasCurrentBaseState = false;
            if (!_isCombat && !_isDead)
                _hasPendingBaseState = false;
        }

        public void Unbind()
        {
            InterruptAttack();
            _controlsRootMotionBone = false;
            _rigidbody = null;
            _isBound = false;
            _isCombat = false;
            _hasPendingBaseState = false;
        }

        public bool PlayDeath()
        {
            InterruptAttack();
            _isDead = true;
            _isCinematicPlaying = false;
            return CrossFadeBase(_deathStatePath, 0.05f, true);
        }

        // Cinematic 재생 중에는 기본 이동 표현이 Timeline을 덮어쓰지 않는다.
        public bool BeginCinematic()
        {
            if (_isCinematicPlaying || _isDead || !ResolveAnimator())
                return false;

            InterruptAttack();
            _hasPendingBaseState = false;
            _isCinematicPlaying = true;
            _controlsRootMotionBone = false;
            return true;
        }

        public void EndCinematic()
        {
            _isCinematicPlaying = false;
            _hasCurrentBaseState = false;
        }

        public bool CanPlayAttack(string p_statePath) =>
            _isBound && isActiveAndEnabled && !_isDead && !_isCinematicPlaying && _attackId == 0 &&
            ResolveAnimator() && _animator.isActiveAndEnabled && _animator.isInitialized &&
            !string.IsNullOrWhiteSpace(p_statePath) && _animator.layerCount > BaseLayer &&
            _animator.HasState(BaseLayer, Animator.StringToHash(p_statePath));

        public bool TryPlayAttack(long p_attackId, string p_statePath)
        {
            if (p_attackId <= 0 || !CanPlayAttack(p_statePath))
                return false;
            if (!CrossFadeBase(p_statePath, 0.05f, true))
                return false;
            _attackId = p_attackId;
            _attackStateHash = Animator.StringToHash(p_statePath);
            _attackController = _animator.runtimeAnimatorController;
            _attackRequestFrame = Time.frameCount;
            _hasEnteredAttack = false;
            _attackStartWait = 0f;
            return true;
        }

        public void StopAttack(long p_attackId)
        {
            ReleaseRushMotion(p_attackId);
            if (_attackId != p_attackId || _attackId == 0)
                return;
            _attackId = 0;
            _hasCurrentBaseState = false;
            if (_isBound && _isCombat && !_isDead && !_isCinematicPlaying && isActiveAndEnabled)
                CrossFadeBase(_idleStatePath);
        }

        private void InterruptAttack()
        {
            ReleaseRushMotion(_rushMotionId);
            long id = _attackId;
            _attackId = 0;
            _hasCurrentBaseState = false;
            if (id != 0)
                OnAttackInterrupted?.Invoke(id);
        }

        // Animator評価後の攻撃状態を読む。古い攻撃の通知は実行IDで区別する。
        private void UpdateAttackProgress()
        {
            if (_attackId == 0 || Time.deltaTime <= 0f || Time.frameCount <= _attackRequestFrame)
                return;
            if (!_animator.isActiveAndEnabled || _animator.runtimeAnimatorController != _attackController)
            {
                InterruptAttack();
                return;
            }
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(BaseLayer);
            if (_animator.IsInTransition(BaseLayer))
            {
                AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(BaseLayer);
                if (next.fullPathHash == _attackStateHash)
                    state = next;
                else if (_hasEnteredAttack)
                {
                    InterruptAttack();
                    return;
                }
            }
            if (state.fullPathHash != _attackStateHash)
            {
                _attackStartWait += Time.deltaTime;
                if (_hasEnteredAttack || _attackStartWait > 1f)
                    InterruptAttack();
                return;
            }
            _hasEnteredAttack = true;
            long id = _attackId;
            float duration = state.length;
            if (duration <= 0f || float.IsInfinity(duration) || float.IsNaN(duration))
            {
                InterruptAttack();
                return;
            }
            OnAttackProgress?.Invoke(id, Mathf.Clamp01(state.normalizedTime) * duration, duration);
            if (_attackId == id && state.normalizedTime >= 1f)
            {
                _attackId = 0;
                _hasCurrentBaseState = false;
                OnAttackCompleted?.Invoke(id);
            }
        }

        private void UpdateMovementState()
        {
            Vector3 velocity = _rigidbody != null
                ? Vector3.ProjectOnPlane(_rigidbody.linearVelocity, Vector3.up)
                : Vector3.zero;

            CrossFadeBase(velocity.sqrMagnitude > 0.01f ? _chaseStatePath : _idleStatePath);
        }

        private bool CrossFadeBase(
            string p_statePath,
            float p_transitionDuration = 0.15f,
            bool p_restart = false)
        {
            if (string.IsNullOrWhiteSpace(p_statePath))
                return false;

            return CrossFadeBase(
                Animator.StringToHash(p_statePath),
                p_statePath,
                p_transitionDuration,
                p_restart);
        }

        private bool CrossFadeBase(
            int p_stateHash,
            string p_statePath,
            float p_transitionDuration,
            bool p_restart)
        {
            if (!ResolveAnimator() ||
                _animator.runtimeAnimatorController == null)
            {
                _hasPendingBaseState = false;
                return false;
            }

            // 실행 중 Controller가 교체되면 이전 상태의 캐시를 사용하지 않는다.
            if (_lastController != _animator.runtimeAnimatorController)
            {
                _lastController = _animator.runtimeAnimatorController;
                _hasCurrentBaseState = false;
            }

            if (!_animator.isInitialized)
            {
                QueueBaseState(
                    p_stateHash,
                    p_statePath,
                    p_transitionDuration,
                    p_restart);
                return false;
            }

            if (_animator.layerCount <= BaseLayer ||
                !_animator.HasState(BaseLayer, p_stateHash))
            {
                _hasPendingBaseState = false;
                if (_logCrossFadeRequests)
                {
                    Debug.LogError(
                        $"[{name}] Boss Animation 상태가 없습니다: " +
                        p_statePath,
                        this);
                }

                return false;
            }

            _hasPendingBaseState = false;

            if (!p_restart &&
                _hasCurrentBaseState &&
                _currentBaseState == p_stateHash)
            {
                return true;
            }

            _animator.CrossFadeInFixedTime(
                p_stateHash,
                p_transitionDuration,
                BaseLayer,
                0f);
            _currentBaseState = p_stateHash;
            _hasCurrentBaseState = true;
            return true;
        }

        private void QueueBaseState(
            int p_stateHash,
            string p_statePath,
            float p_transitionDuration,
            bool p_restart)
        {
            _pendingBaseState = p_stateHash;
            _pendingBaseStatePath = p_statePath;
            _pendingTransitionDuration = p_transitionDuration;
            _pendingRestart = p_restart;
            _hasPendingBaseState = true;
        }

        private bool ResolveAnimator()
        {
            _animator ??= GetComponent<Animator>();
            return _animator != null;
        }



        private void Update()
        {
            if (_isBound && _isCombat && !_isCinematicPlaying && !_isDead && _attackId == 0)
                UpdateMovementState();
        }

        private void LateUpdate()
        {
            CorrectRootMotionBone();
            UpdateAttackProgress();
            if (_hasPendingBaseState)
                CrossFadeBase(_pendingBaseState, _pendingBaseStatePath, _pendingTransitionDuration, _pendingRestart);
        }

        private void OnDisable()
        {
            InterruptAttack();
            _controlsRootMotionBone = false;
            _isCinematicPlaying = false;
            _currentBaseState = 0;
            _hasCurrentBaseState = false;
            _hasPendingBaseState = false;
        }

        private void OnValidate() => ResolveAnimator();
    }
}
