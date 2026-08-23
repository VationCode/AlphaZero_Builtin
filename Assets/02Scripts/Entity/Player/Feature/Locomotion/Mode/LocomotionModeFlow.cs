// 모드 전환 관리
using System;
using System.Collections.Generic;
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
    // Player의 이동 Mode와 하위 이동 State의 시작·갱신·종료 순서를 조정한다.
    // ActionFlow가 일반 이동을 막아도 환경 판정과 넉백처럼 필수 이동 처리는 계속 갱신한다.
    public class LocomotionModeFlow : MonoBehaviour
    {
        private PlayerCore _core;
        public ELocomotionMode CurrentMode { get; private set; }

        private readonly Dictionary<ELocomotionMode, StateFlowBase> _flowDict = new();
        public StateFlowBase CurrentFlow => _currentStateFlow;
        private StateFlowBase _currentStateFlow;

        public event Action<string> OnStateFlowChanged;
        // 이동 Mode별 StateFlow를 구성하고 지상 이동에서 시작한다.
        public void Bind(PlayerCore p_core)
        {
            _core = p_core;

            _flowDict.Add(ELocomotionMode.Ground, new GroundStateFlow(p_core));

            _flowDict.Add(ELocomotionMode.Flight, new FlightStateFlow(p_core));

            ChangeMode(ELocomotionMode.Ground, ELocoStateType.Move);
        }

        // 매 프레임 입력과 현재 상태를 갱신한다.
        private void Update()
        {
            if (_core == null) return;

            float gravityScale = CurrentMode == ELocomotionMode.Ground ? 1f : 0f;

            // State 실행 전에 환경 상태 갱신
            _core.LocomotionModule.UpdateEnvironment(gravityScale);

            // 넉백 이동이 진행 중인 프레임에는 현재 State 이동을 중복 적용하지 않는다.
            if (_core.LocomotionModule.TickKnockback(Time.deltaTime))
                return;

            // Mode 전환
            if (_core.CanUseLocomotion &&
                !_core.LocomotionModule.BlocksInput &&
                _currentStateFlow.CanChangeMode(
                    out ELocomotionMode nextMode,
                    out ELocoStateType entryState))
            {
                ChangeMode(nextMode, entryState);
            }

            // 해당 Mode의 상태를 Update
            _currentStateFlow?.TickFlow();
        }

        // ChangeMode 상태 전환을 수행하고 변경을 알린다.
        public void ChangeMode(ELocomotionMode p_nextMode, ELocoStateType p_entryState)
        {
            if (!_flowDict.TryGetValue(p_nextMode, out StateFlowBase nextFlow) ||
                ReferenceEquals(_currentStateFlow, nextFlow))
            {
                Debug.LogWarning($"[LocomotionMode] 등록되지 않은 Mode: {p_nextMode}");
                return;
            }

            string previousMode = _currentStateFlow == null ? "None" : CurrentMode.ToString();

            //Debug.Log($"[LocomotionMode] {previousMode} → {p_nextMode} " + $"(Entry: {p_entryState})");


            _currentStateFlow?.ExitFlow();

            _currentStateFlow = nextFlow;
            CurrentMode = p_nextMode;

            // Context에 현재 Mode 기록
            _core.LocomotionContext.SetCurrentMode(p_nextMode);

            // 지정한 State로 새 Flow 시작
            _currentStateFlow.EnterFlow(p_entryState);
        }
    }
}
