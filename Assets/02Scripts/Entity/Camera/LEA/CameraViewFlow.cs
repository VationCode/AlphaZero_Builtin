using UnityEngine;

namespace Alpha.AlphaCamera
{
    public class CameraViewFlow
    {
        // Camera 입력과 ViewMode, Rig 실행 순서를 제어한다.
        private AlphaInputSystem _input;
        private CameraContext _context;
        private CameraRigModule _rigModule;
        private CameraObstructionModule _obstructionModule;
        private ICameraViewMode _viewMode;
        private CameraTransitionSettings _transitionSettings;
        private CameraPose _transitionStartPose;

        private float _transitionElapsed;
        private float _transitionDuration;

        private bool _isTransitioning;

        public bool IsBound { get; private set; }

        public bool Bind(AlphaInputSystem p_input, CameraContext p_context,
                         CameraRigModule p_rigModule, CameraObstructionModule p_obstructionModule,
                         ICameraViewMode p_initialViewMode, CameraTransitionSettings p_transitionSettings)
        {
            if (IsBound)
                return true;

            if (p_input == null || p_context == null ||
                p_rigModule == null || !p_rigModule.IsBound ||
                p_obstructionModule == null || !p_obstructionModule.IsBound ||
                p_initialViewMode == null || p_transitionSettings == null)
            {
                return false;
            }

            if (!p_initialViewMode.TryInitialize(p_context))
                return false;

            if (!p_initialViewMode.TryCreatePose(p_context, out CameraPose pose))
            {
                return false;
            }

            _input = p_input;
            _context = p_context;
            _rigModule = p_rigModule;
            _obstructionModule = p_obstructionModule;
            _viewMode = p_initialViewMode;
            _transitionSettings = p_transitionSettings;


            _obstructionModule.ResetDistance();

            if (!TryApplyPose(p_initialViewMode, pose, 0f))
            {
                ClearBindings();
                return false;
            }

            IsBound = true;
            return true;
        }

        public bool TrySetViewMode(ICameraViewMode p_viewMode)
        {
            if (!IsBound || p_viewMode == null)
                return false;

            if (ReferenceEquals(_viewMode, p_viewMode))
                return true;

            if (!p_viewMode.TryInitialize(_context))
                return false;

            if (!p_viewMode.TryCreatePose(_context, out CameraPose targetPose))
            {
                return false;
            }

            // 장애물 보정과 진행 중인 전환이 반영된 실제 Pose를 시작점으로 사용한다.
            if (!_rigModule.TryGetCurrentPose(out CameraPose currentPose))
            {
                return false;
            }

            float duration = _transitionSettings.ResolveDuration(_viewMode.ViewType, p_viewMode.ViewType);

            _obstructionModule.ResetDistance();

            if (duration <= Mathf.Epsilon)
            {
                if (!TryApplyPose(p_viewMode, targetPose, 0f))
                    return false;

                _viewMode = p_viewMode;
                ResetTransition();

                return true;
            }

            _transitionStartPose = currentPose;
            _transitionElapsed = 0f;
            _transitionDuration = duration;
            _isTransitioning = true;


            // 전환 중에도 목표 View의 입력과 Pose가 갱신되도록 즉시 교체한다.
            _viewMode = p_viewMode;

            return true;
        }

        public bool TryTick(float p_deltaTime)
        {
            if (!IsBound || _viewMode == null)
                return false;

            float deltaTime = Mathf.Max(0f, p_deltaTime);

            if (!_viewMode.TryUpdateContext(_context, _input.LookInput, _input.MouseScroll.y, deltaTime))
            {
                return false;
            }

            if (!_viewMode.TryCreatePose(_context, out CameraPose targetPose))
            {
                return false;
            }

            if (!_rigModule.TryFollowTarget(_viewMode.FollowSpeed, p_deltaTime))
            {
                return false;
            }

            if (!_isTransitioning)
                return TryApplyPose(_viewMode, targetPose, deltaTime);

            float nextElapsed = _transitionElapsed + deltaTime;

            float ratio = Mathf.Clamp01(nextElapsed / _transitionDuration);

            // 시작과 종료 시점의 속도를 완만하게 만든다.
            float smoothRatio = ratio * ratio * (3f - (2f * ratio));

            CameraPose transitionPose = CameraPose.Lerp(_transitionStartPose, targetPose, smoothRatio);

            if (!TryApplyPose(_viewMode, transitionPose, deltaTime))
            {
                return false;
            }

            _transitionElapsed = nextElapsed;

            if (ratio >= 1f)
                ResetTransition();

            return true;
        }

        private bool TryApplyPose(ICameraViewMode p_viewMode, CameraPose p_pose, float p_deltaTime)
        {
            // 먼저 Pivot과 Shoulder를 갱신해 Cast 기준점을 확정한다.
            if (!_rigModule.TryApplyPose(p_pose))
                return false;

            if (!p_viewMode.UsesObstruction)
                return true;

            float desiredDistance = Mathf.Max(0f, -p_pose.CameraLocalPosition.z);

            if (!_obstructionModule.TryResolveDistance(desiredDistance, p_deltaTime, out float resolvedDistance))
            {
                return false;
            }

            return _rigModule.TryApplyCameraDistance(resolvedDistance);
        }

        // 전환 상태 초기화
        private void ResetTransition()
        {
            _transitionStartPose = default;
            _transitionElapsed = 0f;
            _transitionDuration = 0f;
            _isTransitioning = false;
        }

        private void ClearBindings()
        {
            _input = null;
            _context = null;
            _rigModule = null;
            _obstructionModule = null;
            _viewMode = null;
            _transitionSettings = null;

            ResetTransition();

            IsBound = false;
        }
    }
}
