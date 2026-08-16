using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Camera 입력과 ViewMode, Rig 실행 순서를 제어한다.
    public class CameraViewFlow : MonoBehaviour
    {
        private CameraCore _core;
        private AlphaInputSystem _input;
        private CameraContext _context;

        private CameraViewModule _viewModule;
        private CameraObstructionModule _obstructionModule;

        public CameraContext Context => _context;
        // CameraCore 요청 이벤트와 입력·Context·Module을 연결한다.
        public void Bind(
            CameraCore p_core,
            CameraViewModule p_viewModule,
            CameraObstructionModule p_obstructionModule,
            AlphaInputSystem p_input)
        {
            _core = p_core;
            _input = p_input;
            _viewModule = p_viewModule;
            _obstructionModule = p_obstructionModule;

            _context = p_core.Context;

            _core.OnViewRequested += HandleViewRequested;
        }

        // 외부 View 요청을 검증과 전환 준비 단계로 전달한다.
        private void HandleViewRequested(ECameraViewType p_viewType)
        {
            TryPrepareViewTransition(p_viewType);
        }

        // 전환·회전·줌 처리 후 Rig가 Target을 따라가도록 갱신한다.
        private void LateUpdate()
        {
            // 전환 중에는 회전·줌 입력을 잠그고 보간만 진행한다.
            if (_context.IsTransitioning)
            {
                UpdateViewTransition();
            }
            else
            {
                // Quarter View는 고정 회전을 사용하므로 Look 입력을 적용하지 않는다.
                if(_context.CurrentViewType != ECameraViewType.Quarter)
                    UpdateRotation();
                
                UpdateZoom();

            }

            // 전환 여부와 관계없이 항상 Target을 추적한다.
            _viewModule.UpdateRigFollow();

            // 최종 Rig 위치와 회전을 기준으로 장애물 안전 거리를 적용한다.
            UpdateObstruction();
        }

        // 전환 가능 여부 검증 및 설정
        private bool TryPrepareViewTransition(ECameraViewType p_targetViewType)
        {
            if (!CanChangeView(p_targetViewType))
            {
                Debug.LogWarning($"Cannot change view to {p_targetViewType}");
                return false;
            }

            // 재전환 중에는 기존 목표 View를 새로운 출발 타입으로 사용한다.
            ECameraViewType fromViewType = _context.EffectiveViewType;

            // Module에 시작값과 목표값 설정
            if (!_viewModule.TryBeginViewTransition(p_targetViewType))
            {
                return false;
            }

            _obstructionModule.ResetDistance();

            _context.BeginTransition(fromViewType, p_targetViewType);
            _core.NotifyViewTransitionStarted(fromViewType, p_targetViewType);

            return true;
        }

        // 중복 요청·Profile 존재 여부와 현재 View를 검사한다.
        private bool CanChangeView(ECameraViewType p_targetViewType)
        {
            if (!_viewModule.HasViewProfile(p_targetViewType))
                return false;

            // 같은 목표만 중복 차단하고 반대 방향 요청은 허용한다.
            if (_context.IsTransitioning)
                return _context.TargetViewType != p_targetViewType;

            // 최초 View 요청은 CurrentView가 없으므로 허용한다.
            if (_viewModule.CurrentView == null)
                return true;

            return _context.CurrentViewType != p_targetViewType;
        }

        // Module 전환 완료 시 Context의 View·회전·줌 상태를 확정한다.
        private void UpdateViewTransition()
        {
            bool isCompleted = _viewModule.TransionView();

            // 보간이 끝나기 전에는 현재 Context를 변경하지 않는다.
            if (!isCompleted) return;

            CameraViewSO currentView = _viewModule.CurrentView;
            ECameraViewType previousViewType = _context.CurrentViewType;

            // 완료 View와 전환 상태를 함께 확정한다.
            _context.CompleteTransition(currentView.ViewType);

            // Quarter View는 회전이 고정되어 있으므로, 이전 View가 Quarter이거나 현재 View가 Quarter이면 회전값을 유지한다.
            bool usesFixedRotation = previousViewType == ECameraViewType.Quarter || 
                                     currentView.ViewType == ECameraViewType.Quarter;
            if (usesFixedRotation)
            {
                _context.SetRotation(currentView.PivotEulerAngles.x, currentView.PivotEulerAngles.y);
            }

            _context.SetZoomDistance(currentView.ZoomDistance);
            _core.NotifyViewTransitionCompleted(currentView.ViewType);
        }

        // 전환 중이 아닐 때 Look 입력을 카메라 회전에 반영한다.
        private void UpdateRotation()
        {
            if (_context.IsTransitioning || _viewModule.CurrentView == null)
            {
                return;
            }
            _viewModule.UpdateRotation(_input.LookInput, _context);
        }

        // 전환 중이 아닐 때 휠 입력을 카메라 거리에 반영한다.
        private void UpdateZoom()
        {
            if (_context.IsTransitioning || _viewModule.CurrentView == null)
            {
                return;
            }

            _viewModule.UpdateZoom(_input.MouseScroll.y, _context);
        }

        // Shoulder에서 희망 Camera 위치까지 검사하고 실제 Zoom 거리를 보정한다.
        private void UpdateObstruction()
        {
            if (_viewModule.CurrentView == null)
                return;

            if (_obstructionModule.TryResolveDistance(
                    _viewModule.DesiredZoomDistance,
                    Time.deltaTime,
                    out float resolvedDistance))
            {
                _viewModule.ApplyResolvedZoomDistance(resolvedDistance);
            }
        }

        // 객체 해제 시 등록한 이벤트와 참조를 정리한다.
        private void OnDestroy()
        {
            if (_core != null)
            {
                _core.OnViewRequested -= HandleViewRequested;
            }
        }
    }
}
