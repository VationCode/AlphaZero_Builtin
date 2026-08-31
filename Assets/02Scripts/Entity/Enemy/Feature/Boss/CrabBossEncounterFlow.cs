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
        Battle
    }

    // Intro를 시작하고 종료 시 Player를 고정 Target으로 전달하는 최소 Encounter Flow다.
    [DisallowMultipleComponent]
    public sealed class CrabBossEncounterFlow : MonoBehaviour
    {
        [SerializeField]
        private EnemyCore _boss;

        [SerializeField]
        private CrabBossIntroView _intro;

        private PlayerCore _player;
        private AlphaInputSystem _input;
        private InputAction _introSkipAction;
        private bool _isIntroTriggerArmed;
        private bool _isPlayerBlocked;
        private bool _isBossBlocked;
        private bool _ownsBossInvulnerability;
        private bool _ownsGameplayInputBlock;
        private bool _isHudHidden;
        private bool _isShuttingDown;

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

        private void OnEnable()
        {
            _isShuttingDown = false;
        }

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

        private void HandleIntroFinished(bool p_wasCancelled)
        {
            _introSkipAction?.Disable();
            ReleaseIntroContext();

            if (_isShuttingDown)
                return;

            if (p_wasCancelled || !TryBeginBattle())
            {
                AcquireBossDormantState();
                ChangeState(ECrabBossEncounterState.Dormant);
                SetIntroTriggerArmed(true);
                return;
            }

            ReleaseBossDormantState();
            ChangeState(ECrabBossEncounterState.Battle);
        }

        private bool TryBeginBattle()
        {
            Transform playerTarget =
                _player != null ? _player.transform : null;

            return _boss?.BeginTargetLock(this, playerTarget) == true;
        }

        private void AcquireIntroContext()
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

        private void ReleaseIntroContext()
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
            _isShuttingDown = true;
            _introSkipAction?.Disable();
            _intro?.Cancel();
            ReleaseIntroContext();
            _boss?.EndTargetLock(this);
            ReleaseBossDormantState();
        }

        private void OnDestroy()
        {
            if (_introSkipAction == null)
                return;

            _introSkipAction.performed -= HandleIntroSkip;
            _introSkipAction.Dispose();
            _introSkipAction = null;
        }
    }
}
