using System;
using Alpha.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alpha.Boss
{
    // Cinematic 실행 조건과 Player 잠금, 완료 후 Combat 전환을 결정한다.
    [DisallowMultipleComponent]
    public sealed class BossCinematicFlow : MonoBehaviour
    {
        private BossCore _boss;
        private BossCinematicModule _cinematicModule;
        private BossCinematicView _cinematicView;
        private AlphaInputSystem _input;
        private PlayerCore _player;
        private InputAction _skipAction;

        private bool _ownsPlayback;
        private bool _isWaitingPrepared;
        private bool _isPlayerBlocked;
        private bool _ownsGameplayInputBlock;
        private bool _isHudHidden;
        private bool _hasStarted;
        private bool _isShuttingDown;
        private string _reportedStartFailure;

        public BossEncounterContext Context => _boss?.EncounterContext;
        public bool IsCinematicTriggerArmed =>
            !_isShuttingDown &&
            _isWaitingPrepared &&
            Context?.CurrentState ==
                EBossEncounterState.WaitingForCinematic &&
            !_ownsPlayback &&
            _input != null &&
            _cinematicView != null;

        public event Action<bool> OnCinematicTriggerArmedChanged;
        public event Action<bool> OnGameplayHudVisibilityRequested;

        private void Awake()
        {
            CreateSkipAction();
        }

        private void OnEnable()
        {
            _isShuttingDown = false;

            if (_hasStarted)
                PrepareWaitingState();
        }

        private void Start()
        {
            _hasStarted = true;
            PrepareWaitingState();
        }

        // Root가 Boss가 소유한 Cinematic 구성 요소를 연결한다.
        public bool Bind(
            BossCore p_boss,
            BossCinematicModule p_cinematicModule,
            BossCinematicView p_cinematicView)
        {
            if (p_boss == null ||
                p_cinematicModule == null ||
                p_cinematicView == null)
            {
                return false;
            }

            _boss = p_boss;
            _cinematicModule = p_cinematicModule;
            _cinematicView = p_cinematicView;
            NotifyTriggerArmedChanged();
            return true;
        }

        // Installer가 Scene 공용 입력을 Entity 경계로 전달한다.
        public bool BindInput(AlphaInputSystem p_input)
        {
            _input = p_input;
            NotifyTriggerArmedChanged();
            return _input != null;
        }

        public void RefreshCinematicTrigger()
        {
            NotifyTriggerArmedChanged();
        }

        public bool RequestStart(PlayerCore p_player)
        {
            if (!IsCinematicTriggerArmed)
                return false;

            if (p_player == null)
                return ReportStartFailure("PlayerCore를 찾지 못했습니다.");

            if (!_cinematicView.IsConfigured)
                return ReportStartFailure(
                    _cinematicView.ConfigurationIssue);

            if (!_cinematicModule.PrepareCinematic())
                return ReportStartFailure(
                    "Boss Cinematic 대기 상태를 준비하지 못했습니다.");

            _reportedStartFailure = null;

            _player = p_player;

            if (Context?.TryBeginCinematic() != true)
            {
                RollbackCinematic();
                return false;
            }

            AcquireCinematicContext();
            _ownsPlayback = true;
            NotifyTriggerArmedChanged();

            if (_cinematicView.TryPlay(HandleCinematicCompleted))
            {
                _skipAction?.Enable();
                Debug.Log("Boss Cinematic 재생을 시작했습니다.", this);
                return true;
            }

            _ownsPlayback = false;
            RollbackCinematic();
            return ReportStartFailure(
                "PlayableDirector가 재생을 시작하지 못했습니다.");
        }

        private bool ReportStartFailure(string p_reason)
        {
            string reason = string.IsNullOrWhiteSpace(p_reason)
                ? "알 수 없는 구성 오류입니다."
                : p_reason;

            if (!string.Equals(
                    _reportedStartFailure,
                    reason,
                    StringComparison.Ordinal))
            {
                _reportedStartFailure = reason;
                Debug.LogWarning(
                    $"Boss Cinematic을 시작하지 못했습니다: {reason}",
                    this);
            }

            return false;
        }

        public bool SkipCinematic()
        {
            return _ownsPlayback &&
                   _cinematicView?.Skip() == true;
        }

        public bool CancelCinematic()
        {
            return _ownsPlayback &&
                   _cinematicView?.Cancel() == true;
        }

        private void HandleCinematicCompleted(
            EBossCinematicPlayResult p_result)
        {
            if (!_ownsPlayback)
                return;

            _ownsPlayback = false;
            _skipAction?.Disable();
            ReleaseCinematicContext();

            if (p_result is EBossCinematicPlayResult.Completed or
                EBossCinematicPlayResult.Skipped)
            {
                Transform playerTarget =
                    _player != null ? _player.transform : null;

                if (playerTarget != null &&
                    Context?.TryBeginCombat() == true)
                {
                    _cinematicModule.BeginCombat();
                    _boss.SetTarget(playerTarget);
                    _player = null;
                    _isWaitingPrepared = false;
                    NotifyTriggerArmedChanged();
                    return;
                }
            }

            RollbackCinematic();
        }

        private void PrepareWaitingState()
        {
            _isWaitingPrepared =
                _cinematicModule?.PrepareWaitingForCinematic() == true;
            Context?.ReturnToWaitingForCinematic();
            NotifyTriggerArmedChanged();
        }

        private void RollbackCinematic()
        {
            _skipAction?.Disable();
            ReleaseCinematicContext();
            _isWaitingPrepared =
                _cinematicModule?.ReturnToWaitingForCinematic() == true;
            Context?.ReturnToWaitingForCinematic();
            _player = null;
            NotifyTriggerArmedChanged();
        }

        private void AcquireCinematicContext()
        {
            _isPlayerBlocked =
                _player?.ActionFlow?.BeginExternalBlock(this) == true;
            _ownsGameplayInputBlock =
                _input?.BeginGameplayInputBlock(this) == true;

            if (_isHudHidden)
                return;

            _isHudHidden = true;
            OnGameplayHudVisibilityRequested?.Invoke(false);
        }

        private void ReleaseCinematicContext()
        {
            if (_isPlayerBlocked)
            {
                _player?.ActionFlow?.EndExternalBlock(this);
                _isPlayerBlocked = false;
            }

            if (_ownsGameplayInputBlock)
            {
                _input?.EndGameplayInputBlock(this);
                _ownsGameplayInputBlock = false;
            }

            if (!_isHudHidden)
                return;

            _isHudHidden = false;
            OnGameplayHudVisibilityRequested?.Invoke(true);
        }

        private void CreateSkipAction()
        {
            if (_skipAction != null)
                return;

            _skipAction = new InputAction(
                "SkipBossCinematic",
                InputActionType.Button);
            _skipAction.AddBinding("<Keyboard>/escape");
            _skipAction.AddBinding("<Gamepad>/start");
            _skipAction.performed += HandleCinematicSkip;
        }

        private void HandleCinematicSkip(InputAction.CallbackContext _)
        {
            SkipCinematic();
        }

        private void NotifyTriggerArmedChanged()
        {
            OnCinematicTriggerArmedChanged?.Invoke(
                IsCinematicTriggerArmed);
        }

        private void OnDisable()
        {
            _isShuttingDown = true;
            _skipAction?.Disable();

            if (_ownsPlayback)
            {
                _ownsPlayback = false;
                _cinematicView?.Cancel();
            }

            ReleaseCinematicContext();
            _cinematicModule?.Release();
            _isWaitingPrepared = false;
            _player = null;
            Context?.ReturnToWaitingForCinematic();
            NotifyTriggerArmedChanged();
        }

        private void OnDestroy()
        {
            if (_skipAction == null)
                return;

            _skipAction.performed -= HandleCinematicSkip;
            _skipAction.Dispose();
            _skipAction = null;
        }
    }
}
