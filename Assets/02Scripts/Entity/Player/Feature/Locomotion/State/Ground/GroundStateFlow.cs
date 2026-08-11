
using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Player.Locomotion
{
    // GroundStateFlow 요청의 조건과 실행 순서를 결정한다.
    public class GroundStateFlow : StateFlowBase
    {

        private Dictionary<ELocoStateType, StateBase> _stateDict;

        // 전달받은 값으로 초기 상태를 구성한다.
        public GroundStateFlow(PlayerCore p_core) : base(p_core)
        {
            _stateDict = new Dictionary<ELocoStateType, StateBase>
            {
                { ELocoStateType.Move, new GroundMoveState(p_core, this) },
                { ELocoStateType.Jump, new GroundJumpState(p_core, this) },
                { ELocoStateType.Fall, new GroundFallState(p_core, this) },
                { ELocoStateType.Land, new GroundLandState(p_core, this) },
                { ELocoStateType.Dash, new GroundDashState(p_core, this) }
            };
        }

        // GetState 결과를 현재 상태에서 계산해 반환한다.
        protected override StateBase GetState(ELocoStateType p_stateType)
        {
            return _stateDict[p_stateType];
        }

        // CanChangeMode 실행 가능 조건을 검사한다.
        internal override bool CanChangeMode(out ELocomotionMode p_nextMode, out ELocoStateType p_entryState)
        {
            if (_Core.Input.IsFlight &&
                _Rule.CanFlight(_Core.LocomotionContext))
            {
                p_nextMode = ELocomotionMode.Flight;
                p_entryState = ELocoStateType.Rising;
                return true;
            }

            p_nextMode = default;
            p_entryState = default;
            return false;
        }
    }
}
