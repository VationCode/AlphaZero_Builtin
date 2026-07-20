/*
클래스 책임
Mode를 등록한다.
현재 Mode를 선택한다.
Mode 변경 결과를 Context에 기록한다.
 */

using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Player.Locomotion
{
    public class LocomotionModeFlow : MonoBehaviour
    {
        private readonly Dictionary<ELocomotionMode, ILocomotionMode> _modes 
                                                                      = new Dictionary<ELocomotionMode, ILocomotionMode>();

        private LocomotionContext _context;

        public ILocomotionMode CurrentMode { get; private set; }

        public void Bind(LocomotionContext p_context, ILocomotionMode[] p_modes)
        {
            _context = p_context;

            foreach (ILocomotionMode mode in p_modes)
            {
                _modes.Add(mode.Type, mode);
            }

            ChangeMode(ELocomotionMode.Ground);
        }

        // 현재 이동 정책을 변경
        public bool ChangeMode(ELocomotionMode p_nextMode)
        {
            if (CurrentMode != null && CurrentMode.Type == p_nextMode)
                return false;

            if (!_modes.TryGetValue(p_nextMode, out ILocomotionMode nextMode))
                return false;

            CurrentMode = nextMode;
            _context.CurrentMode = nextMode.Type;

            return true;
        }

        // 비행 전환 입력을 현재 Mode에 맞게 해석
        public void ToggleFlightMode()
        {
            ELocomotionMode nextMode = 
                CurrentMode.Type == ELocomotionMode.Ground? ELocomotionMode.Flight : ELocomotionMode.Ground;

            ChangeMode(nextMode);
        }
    }
}
