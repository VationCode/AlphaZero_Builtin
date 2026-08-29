using System;
using Alpha.Enemy.View;
using Alpha.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alpha.Enemy
{
    public enum ECrabBossEncounterState
    {
        Dormant,
        Intro,
        Battle,
        Resetting,
        Defeated
    }

    // CrabBoss의 Intro 진입, 전투 전환, 구역 이탈 재설정을 관리한다.
    [DisallowMultipleComponent]
    public sealed class CrabBossEncounterFlow : MonoBehaviour
    {
        [SerializeField]
        private EnemyCore _boss;

        [SerializeField]
        private CrabBossIntroView _intro;

        private PlayerCore _player;
        private AlphaInputSystem _input;
        private Rigidbody _bossRigidbody;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _hasInitialPose;
        private bool _isIntroTriggerArmed;
        private bool _isResetRequested;
        private bool _isPlayerBlocked;
        private bool _isBossBlocked;
        private bool _previousBossTargetSearchEnabled;
        private bool _hasBossTargetSearchState;
        private bool _ownsBossInvulnerability;
        private bool _ownsGameplayInputBlock;
        private bool _isHudHidden;
        private InputAction _introSkipAction;

        public ECrabBossEncounterState CurrentState { get; private set; } =
            ECrabBossEncounterState.Dormant;
        public bool IsIntroTriggerArmed => _isIntroTriggerArmed;

        public event Action<ECrabBossEncounterState> OnStateChanged;
        public event Action<bool> OnIntroTriggerArmedChanged;
        public event Action<bool> OnGameplayHudVisibilityRequested;

        private void Awake()
        {
            _boss ??= GetComponentInChildren<EnemyCore>(true);
            _intro ??= GetComponentInChildren<CrabBossIntroView>(true);
            CreateIntroSkipAction();
        }

        // Installer가 Scene 공용 의존성만 Encounter에 전달한다.
        public void Bind(
            PlayerCore p_player,
            AlphaInputSystem p_input)
        {
            _player = p_player;
            _input = p_input;
        }

        public void BindCamera(CinemachineBrain p_brain)
        {
            _intro?.BindCamera(p_brain);
        }

        private void Start()
        {
            CaptureInitialPose();

            if (_boss?.HealthModule != null)
            {
                _boss.HealthModule.OnDeath -= HandleBossDefeated;
                _boss.HealthModule.OnDeath += HandleBossDefeated;
            }

            AcquireBossDormantState();
            ChangeState(ECrabBossEncounterState.Dormant);
            SetIntroTriggerArmed(true);
        }

        public bool RequestStart(PlayerCore p_player)
        {
            if (CurrentState != ECrabBossEncounterState.Dormant ||
                !_isIntroTriggerArmed ||
                !ReferenceEquals(_player, p_player) ||
                _boss == null ||
                _intro == null ||
                !_intro.IsConfigured ||
                _input == null)
            {
                return false;
            }

            SetIntroTriggerArmed(false);
            AcquireBossDormantState();
            AcquireIntroContext();
            ChangeState(ECrabBossEncounterState.Intro);

            if (_intro.TryPlay(HandleIntroFinished))
            {
                _introSkipAction?.Enable();
                return true;
            }

            ReleaseIntroContext();
            ChangeState(ECrabBossEncounterState.Dormant);
            SetIntroTriggerArmed(true);
            return false;
        }

        // Player가 외부 경계를 벗어나면 Intro 또는 전투를 초기 상태로 되돌린다.
        public bool RequestReset(PlayerCore p_player)
        {
            if (!ReferenceEquals(_player, p_player) ||
                CurrentState is ECrabBossEncounterState.Dormant or
                    ECrabBossEncounterState.Resetting or
                    ECrabBossEncounterState.Defeated)
            {
                return false;
            }

            _isResetRequested = true;
            SetIntroTriggerArmed(false);

            if (CurrentState == ECrabBossEncounterState.Intro &&
                _intro?.IsPlaying == true)
            {
                _intro.Cancel();
            }
            else
            {
                ResetEncounter();
            }

            return true;
        }

        private void AcquireIntroContext()
        {
            DisableBossTargetSearch();

            _isPlayerBlocked =
                _player?.ActionFlow?.BeginExternalBlock(this) == true;
            _ownsGameplayInputBlock =
                _input?.BeginGameplayInputBlock(this) == true;

            if (!_isHudHidden)
            {
                _isHudHidden = true;
                OnGameplayHudVisibilityRequested?.Invoke(false);
            }
        }

        private void ReleaseIntroContext()
        {
            RestoreBossTargetSearch();
            ReleasePlayerBlock();

            if (_ownsGameplayInputBlock)
            {
                _input?.EndGameplayInputBlock(this);
                _ownsGameplayInputBlock = false;
            }

            if (_isHudHidden)
            {
                _isHudHidden = false;
                OnGameplayHudVisibilityRequested?.Invoke(true);
            }
        }

        private void DisableBossTargetSearch()
        {
            if (_hasBossTargetSearchState ||
                _boss?.TargetDetectionModule == null)
            {
                return;
            }

            _previousBossTargetSearchEnabled =
                _boss.TargetDetectionModule.enabled;
            _hasBossTargetSearchState = true;

            _boss.TargetingFlow.ClearTarget();
            _boss.TargetDetectionModule.enabled = false;
        }

        private void RestoreBossTargetSearch()
        {
            if (!_hasBossTargetSearchState ||
                _boss?.TargetDetectionModule == null)
            {
                return;
            }

            _boss.TargetDetectionModule.enabled =
                _previousBossTargetSearchEnabled;
            _hasBossTargetSearchState = false;
        }

        private void AcquireBossDormantState()
        {
            if (!_isBossBlocked)
            {
                _isBossBlocked =
                    _boss?.ActionFlow?.BeginExternalBlock(this) == true;
            }

            if (!_ownsBossInvulnerability)
            {
                _ownsBossInvulnerability =
                    _boss?.DamageReceiver?.BeginInvulnerability(this) == true;
            }
        }

        private void HandleIntroFinished(bool p_wasCancelled)
        {
            _introSkipAction?.Disable();
            ReleaseIntroContext();

            if (_isResetRequested || p_wasCancelled)
            {
                ResetEncounter();
                return;
            }

            ReleaseBossDormantState();
            ChangeState(ECrabBossEncounterState.Battle);
        }

        private void ResetEncounter()
        {
            if (CurrentState == ECrabBossEncounterState.Defeated ||
                _boss == null)
            {
                return;
            }

            ChangeState(ECrabBossEncounterState.Resetting);
            ReleaseIntroContext();

            _boss.ActionFlow?.ResetForEncounter();
            _boss.LocomotionModule?.CancelKnockback();
            _boss.LocomotionModule?.Stop();
            RestoreInitialPose();

            if (_boss.HealthModule?.IsDead == false)
                _boss.HealthModule.ResetHealth();

            AcquireBossDormantState();
            _isResetRequested = false;
            ChangeState(ECrabBossEncounterState.Dormant);
            SetIntroTriggerArmed(true);
        }

        private void CaptureInitialPose()
        {
            if (_boss == null || _hasInitialPose)
                return;

            _bossRigidbody = _boss.GetComponent<Rigidbody>();
            _initialPosition = _boss.transform.position;
            _initialRotation = _boss.transform.rotation;
            _hasInitialPose = true;
        }

        private void RestoreInitialPose()
        {
            if (!_hasInitialPose || _boss == null)
                return;

            if (_bossRigidbody != null)
            {
                _bossRigidbody.linearVelocity = Vector3.zero;
                _bossRigidbody.angularVelocity = Vector3.zero;
                _bossRigidbody.position = _initialPosition;
                _bossRigidbody.rotation = _initialRotation;
                return;
            }

            _boss.transform.SetPositionAndRotation(
                _initialPosition,
                _initialRotation);
        }

        private void HandleBossDefeated()
        {
            _isResetRequested = false;
            SetIntroTriggerArmed(false);
            ReleaseIntroContext();
            ReleaseBossDormantState();
            ChangeState(ECrabBossEncounterState.Defeated);
        }

        private void ReleasePlayerBlock()
        {
            if (!_isPlayerBlocked)
                return;

            _player?.ActionFlow?.EndExternalBlock(this);
            _isPlayerBlocked = false;
        }

        private void ReleaseBossDormantState()
        {
            if (_ownsBossInvulnerability)
            {
                _boss?.DamageReceiver?.EndInvulnerability(this);
                _ownsBossInvulnerability = false;
            }

            if (_isBossBlocked)
            {
                _boss?.ActionFlow?.EndExternalBlock(this);
                _isBossBlocked = false;
            }
        }

        private void ChangeState(ECrabBossEncounterState p_nextState)
        {
            if (CurrentState == p_nextState)
                return;

            CurrentState = p_nextState;
            OnStateChanged?.Invoke(CurrentState);
        }

        private void SetIntroTriggerArmed(bool p_isArmed)
        {
            if (_isIntroTriggerArmed == p_isArmed)
                return;

            _isIntroTriggerArmed = p_isArmed;
            OnIntroTriggerArmedChanged?.Invoke(_isIntroTriggerArmed);
        }

        private void CreateIntroSkipAction()
        {
            if (_introSkipAction != null)
                return;

            // Gameplay Action Map이 차단된 동안에도 Intro Skip만 독립적으로 받는다.
            _introSkipAction = new InputAction(
                "SkipCrabBossIntro",
                InputActionType.Button);
            _introSkipAction.AddBinding("<Keyboard>/escape");
            _introSkipAction.AddBinding("<Gamepad>/start");
            _introSkipAction.performed += HandleIntroSkip;
        }

        private void HandleIntroSkip(InputAction.CallbackContext _)
        {
            if (CurrentState == ECrabBossEncounterState.Intro &&
                _intro?.IsPlaying == true)
            {
                _intro.Skip();
            }
        }

        private void OnDisable()
        {
            _introSkipAction?.Disable();

            if (CurrentState == ECrabBossEncounterState.Intro)
            {
                _isResetRequested = true;
                _intro?.Cancel();
            }

            ReleaseIntroContext();
        }

        private void OnDestroy()
        {
            if (_boss?.HealthModule != null)
                _boss.HealthModule.OnDeath -= HandleBossDefeated;

            if (_introSkipAction != null)
            {
                _introSkipAction.performed -= HandleIntroSkip;
                _introSkipAction.Dispose();
                _introSkipAction = null;
            }
        }
    }
}
