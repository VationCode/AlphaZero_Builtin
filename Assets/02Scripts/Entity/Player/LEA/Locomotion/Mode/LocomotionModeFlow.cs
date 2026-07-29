// 모드 전환 관리
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
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
    public class LocomotionModeFlow : MonoBehaviour
    {
        private PlayerCore _core;
        public ELocomotionMode CurrentMode { get; private set; }

        private readonly Dictionary<ELocomotionMode, StateFlowBase> _flowDict = new();
        public StateFlowBase CurrentFlow => _currentStateFlow;
        private StateFlowBase _currentStateFlow;

        public void Bind(PlayerCore p_core)
        {
            _core = p_core;

            _flowDict.Add(ELocomotionMode.Ground, new GroundStateFlow(p_core));

            _flowDict.Add(ELocomotionMode.Flight, new FlightStateFlow(p_core));

            ChangeMode(ELocomotionMode.Ground, ELocoStateType.Move);
        }

        private void Update()
        {
            if (_core == null) return;

            float gravityScale = CurrentMode == ELocomotionMode.Ground ? 1f : 0f;

            // State 실행 전에 환경 상태 갱신
            _core.LocomotionModule.UpdateEnvironment(gravityScale);

            // Mode 전환
            if (_currentStateFlow.CanChangeMode(out ELocomotionMode nextMode, out ELocoStateType entryState))
            {
                ChangeMode(nextMode, entryState);
            }

            // 해당 Mode의 상태 Update
            _currentStateFlow?.TickFlow();
        }

        public void ChangeMode(ELocomotionMode p_nextMode, ELocoStateType p_entryState)
        {
            if (!_flowDict.TryGetValue(p_nextMode, out StateFlowBase nextFlow) ||
                ReferenceEquals(_currentStateFlow, nextFlow))
            {
                Debug.LogWarning($"[LocomotionMode] 등록되지 않은 Mode: {p_nextMode}");
                return;
            }

            string previousMode = _currentStateFlow == null ? "None" : CurrentMode.ToString();

            Debug.Log($"[LocomotionMode] {previousMode} → {p_nextMode} " + $"(Entry: {p_entryState})");


            _currentStateFlow?.ExitFlow();

            _currentStateFlow = nextFlow;
            CurrentMode = p_nextMode;

            // Context에 현재 Mode 기록
            _core.LocomotionContext.CurrentMode = p_nextMode;

            // 지정한 State로 새 Flow 시작
            _currentStateFlow.EnterFlow(p_entryState);
        }
    }
}