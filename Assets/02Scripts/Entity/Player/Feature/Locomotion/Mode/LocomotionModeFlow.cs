using UnityEngine;

// ELocomotionMode 관련 선택 값을 정의한다.
public enum ELocomotionMode
{
    Ground,
    Flight,
    Swim

    // 실제 구현할 때 추가
    // Climb,
    // RopeClimb,
    // RopeSwing,
    // Zipline
}

namespace Alpha.Player.Locomotion
{
    // Player의 이동 Mode와 하나의 공통 이동 State 흐름을 조정한다.
    // ActionFlow가 일반 이동을 막아도 환경 판정과 넉백처럼 필수 이동 처리는 계속 갱신한다.
    public class LocomotionModeFlow : MonoBehaviour
    {
        private PlayerCore _core;
        private LocomotionStateFlow _stateFlow;
        private readonly TransitionRule _rule = new();

        public ELocomotionMode CurrentMode { get; private set; }
        public LocomotionStateFlow CurrentFlow => _stateFlow;

        // Mode와 무관하게 공유하는 State 흐름을 구성하고 지상 이동에서 시작한다.
        public void Bind(PlayerCore p_core)
        {
            if (p_core == null)
            {
                Debug.LogError($"{nameof(LocomotionModeFlow)}에 PlayerCore가 없습니다.", this);
                return;
            }

            _core = p_core;
            _stateFlow = new LocomotionStateFlow(p_core);

            CurrentMode = ELocomotionMode.Ground;
            _core.LocomotionContext.SetCurrentMode(CurrentMode);
            _stateFlow.EnterFlow(ELocoStateType.Move);
        }

        // 매 프레임 입력과 현재 상태를 갱신한다.
        private void Update()
        {
            if (_core == null || _stateFlow == null)
                return;

            float gravityScale = CurrentMode == ELocomotionMode.Ground ? 1f : 0f;

            // State 실행 전에 환경 상태 갱신
            _core.LocomotionModule.UpdateEnvironment(gravityScale);

            // 넉백 이동이 진행 중인 프레임에는 현재 State 이동을 중복 적용하지 않는다.
            if (_core.LocomotionModule.TickKnockback(Time.deltaTime))
                return;

            // 상위 ActionFlow가 막은 동안 현재 State의 일반 행동도 갱신하지 않는다.
            if (!_core.CanUseLocomotion)
                return;

            // Mode만 변경하며 공통 StateFlow 인스턴스는 교체하지 않는다.
            if (!_core.LocomotionModule.BlocksInput &&
                TryResolveModeChange(
                    out ELocomotionMode nextMode,
                    out ELocoStateType entryState))
            {
                ChangeMode(nextMode, entryState);
            }

            _stateFlow.TickFlow();
        }

        // Mode를 갱신하고 새 Mode가 사용할 진입 State만 공통 Flow에 요청한다.
        public void ChangeMode(ELocomotionMode p_nextMode, ELocoStateType p_entryState)
        {
            if (_core == null || _stateFlow == null || !IsSupportedMode(p_nextMode))
            {
                Debug.LogWarning($"[LocomotionMode] 등록되지 않은 Mode: {p_nextMode}");
                return;
            }

            bool isModeChanged = CurrentMode != p_nextMode;
            CurrentMode = p_nextMode;
            _core.LocomotionContext.SetCurrentMode(p_nextMode);

            bool isStateChanged = _stateFlow.ChangeState(p_entryState);

            // Move → Move처럼 State가 유지되어도 Mode 변경은 View에 알려야 한다.
            if (isModeChanged && !isStateChanged)
                _core.LocomotionContext.NotifyCurrentState();
        }

        // Flight 입력을 Mode 토글로 해석하고 다음 State를 함께 결정한다.
        private bool TryResolveModeChange(
            out ELocomotionMode p_nextMode,
            out ELocoStateType p_entryState)
        {
            p_nextMode = default;
            p_entryState = default;

            if (!_core.Input.IsFlight)
                return false;

            if (CurrentMode == ELocomotionMode.Ground &&
                _rule.CanFlight(_core.LocomotionContext))
            {
                p_nextMode = ELocomotionMode.Flight;
                p_entryState = ELocoStateType.Move;
                return true;
            }

            if (CurrentMode == ELocomotionMode.Flight &&
                _rule.CanGround(_core.LocomotionContext))
            {
                p_nextMode = ELocomotionMode.Ground;
                p_entryState = _core.LocomotionModule.IsGrounded
                    ? ELocoStateType.Move
                    : ELocoStateType.Fall;
                return true;
            }

            return false;
        }

        private static bool IsSupportedMode(ELocomotionMode p_mode)
        {
            return p_mode == ELocomotionMode.Ground ||
                   p_mode == ELocomotionMode.Flight;
        }
    }
}
