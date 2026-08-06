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

        private bool _isTransitionRequested;
        public CameraContext Context => _context;
        // CameraCore 요청 이벤트와 입력·Context·Module을 연결한다.
        public void Bind(CameraCore p_core, CameraViewModule p_viewModule, AlphaInputSystem p_input)
        {
            _core = p_core;
            _input = p_input;
            _viewModule = p_viewModule;

            _context = p_core.Context;

            _core.OnViewRequested += HandleViewRequested;
        }

        // 외부 View 요청을 검증과 전환 준비 단계로 전달한다.
        private void HandleViewRequested(ECameraViewType p_viewType, float p_transitionDuration)
        {
            TryPrepareViewTransition(p_viewType, p_transitionDuration);
        }

        // 전환·회전·줌 처리 후 Rig가 Target을 따라가도록 갱신한다.
        private void LateUpdate()
        {
            // 전환 중에는 회전·줌 입력을 잠그고 보간만 진행한다.
            if (_isTransitionRequested)
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
        }

        // 전환 가능 여부 검증 및 설정
        private bool TryPrepareViewTransition(ECameraViewType p_targetViewType, float p_requestedDuration)
        {
            // 실제 전환 시간 결정
            float transitionDuration = ResolveTransitionDuration(p_targetViewType, p_requestedDuration);

            if (!CanChangeView(p_targetViewType, transitionDuration))
            {
                Debug.LogWarning($"Cannot change view to {p_targetViewType} with duration {transitionDuration}");
                return false;
            }

            // Module에 시작값과 목표값 설정
            if (!_viewModule.TryBeginViewTransition(p_targetViewType, transitionDuration))
            {
                return false;
            }

            // LateUpdate 실행 상태 활성화
            _isTransitionRequested = true;

            return true;
        }

        // 시간·중복 요청·Profile 존재 여부와 현재 View를 검사한다.
        private bool CanChangeView(ECameraViewType p_targetViewType, float p_transitionDuration)
        {
            if (p_transitionDuration < 0f)
                return false;

            if (_isTransitionRequested)
                return false; ;

            if (!_viewModule.HasViewProfile(p_targetViewType))
                return false;

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

            // 전환 완료 후 현재 Context를 갱신한다.
            _context.SetViewType(currentView.ViewType);

            // Quarter View는 회전이 고정되어 있으므로, 이전 View가 Quarter이거나 현재 View가 Quarter이면 회전값을 유지한다.
            bool usesFixedRotation = previousViewType == ECameraViewType.Quarter || 
                                     currentView.ViewType == ECameraViewType.Quarter;
            if (usesFixedRotation)
            {
                _context.SetRotation(currentView.PivotEulerAngles.x, currentView.PivotEulerAngles.y);
            }

            _context.SetZoomDistance(currentView.ZoomDistance);

            _isTransitionRequested = false;
        }

        // 전환 중이 아닐 때 Look 입력을 카메라 회전에 반영한다.
        private void UpdateRotation()
        {
            if (_isTransitionRequested || _viewModule.CurrentView == null)
            {
                return;
            }
            _viewModule.UpdateRotation(_input.LookInput, _context);
        }

        // 전환 중이 아닐 때 휠 입력을 카메라 거리에 반영한다.
        private void UpdateZoom()
        {
            if (_isTransitionRequested || _viewModule.CurrentView == null)
            {
                return;
            }

            _viewModule.UpdateZoom(_input.MouseScroll.y, _context);
        }

        // 요청 시간과 현재 View를 기준으로 실제 전환 시간을 결정
        private float ResolveTransitionDuration(ECameraViewType p_targetViewType, float p_requestedDuration)
        {
            bool isFromQuarter = _viewModule.CurrentView != null &&
                                 _viewModule.CurrentView.ViewType == ECameraViewType.Quarter;

            bool isToQuarter = p_targetViewType == ECameraViewType.Quarter;

            // Quarter가 출발점 또는 목표라면 고정된 시간을 사용한다.
            if (isFromQuarter || isToQuarter)
            {
                return _viewModule.QuarterTransitionDuration;
            }

            return p_requestedDuration;
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
