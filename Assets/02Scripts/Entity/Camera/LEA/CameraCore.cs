using System;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Camera 내부 기능을 조립하고 외부 진입점을 제공한다.
    public class CameraCore : MonoBehaviour
    {
        [SerializeField] private ThirdPersonViewSO _thirdPersonViewProfile;
        [SerializeField] private AimViewSO _aimViewProfile;
        [SerializeField] private QuarterViewSO _quarterViewProfile;
        [SerializeField] private bool _useViewInputTest = true;

        [Header("Transition")]
        [SerializeField] private CameraTransitionSettings _transitionSettings = new();

        private AlphaInputSystem _input;

        private ThirdPersonViewMode _thirdPersonViewMode;
        private AimViewMode _aimViewMode;
        private QuarterViewMode _quarterViewMode;

        public CameraContext Context { get; } = new();
        public CameraRigModule RigModule { get; private set; }
        public CameraViewFlow ViewFlow { get; private set; }
        public CameraObstructionModule ObstructionModule{get; private set;}

        public Camera RenderCamera => RigModule != null ? RigModule.RenderCamera : null;

        // 플레이어 이동과 회전의 기준으로 사용할 실제 렌더 카메라 Transform
        public Transform RenderCameraTransform
        {
            get
            {
                return RenderCamera != null? RenderCamera.transform : null;
            }
        }

        public bool IsBound { get; private set; }

        // 기본 View 변경을 외부 표현 시스템에 알린다.
        public event Action<ECameraViewType> OnBaseViewChanged;

        private void Awake()
        {
            ViewFlow = new CameraViewFlow();

            RigModule = GetComponent<CameraRigModule>();
            ObstructionModule = GetComponent<CameraObstructionModule>();

        }

        public bool Bind(AlphaInputSystem p_input, Transform p_followTarget)
        {
            if (IsBound)
                return true;

            if (p_input == null || p_followTarget == null || 
                RigModule == null || ObstructionModule == null || 
                _thirdPersonViewProfile == null || _aimViewProfile == null || _quarterViewProfile == null)
            {
                Debug.LogError($"{nameof(CameraCore)}의 의존성이 설정되지 않았습니다.", this);

                return false;
            }

            if (!RigModule.Bind(p_followTarget))
                return false;

            // Rig가 소유한 Shoulder를 장애물 검사의 시작점으로 연결한다.
            if (!ObstructionModule.Bind(RigModule.ObstructionOrigin))
                return false;
            

            _thirdPersonViewMode = new ThirdPersonViewMode(_thirdPersonViewProfile);
            _aimViewMode = new AimViewMode(_aimViewProfile);
            _quarterViewMode = new QuarterViewMode(_quarterViewProfile);

            // 시작 View는 ThirdPerson으로 설정한다.
            if (!ViewFlow.Bind(p_input, Context, RigModule, ObstructionModule,  _thirdPersonViewMode, _transitionSettings))
            {
                //Debug.LogError($"{nameof(CameraViewFlow)} 연결에 실패했습니다.", this);

                return false;
            }

            Context.BaseViewType = ECameraViewType.ThirdPerson;

            Context.SetCurrentView(ECameraViewType.ThirdPerson);

            _input = p_input;
            IsBound = true;
            return true;
        }

        private void LateUpdate()
        {
            if (!IsBound)
                return;

            if (_useViewInputTest && !TryApplyTestViewInput())
                return;

            ViewFlow.TryTick(Time.deltaTime);
        }

        // 테스트 입력으로 BaseView와 AimView를 독립적으로 전환한다.
        private bool TryApplyTestViewInput()
        {
            ECameraViewType baseViewType =
                _input.IsQuarter
                    ? ECameraViewType.Quarter
                    : ECameraViewType.ThirdPerson;

            // BaseView를 먼저 정해야 Aim 해제 시 돌아갈 View가 결정된다.
            if (!TrySetBaseView(baseViewType))
                return false;

            return TrySetAim(_input.IsAiming);
        }

        // ThirdPerson 또는 Quarter 기본 View를 변경한다.
        public bool TrySetBaseView(ECameraViewType p_viewType)
        {
            if (!IsBound || (p_viewType != ECameraViewType.ThirdPerson && p_viewType != ECameraViewType.Quarter))
            {
                return false;
            }

            if (Context.BaseViewType == p_viewType)
                return true;

            // Aim 중에는 화면을 유지하고 복귀할 View만 변경한다.
            if (Context.CurrentViewType == ECameraViewType.Aim)
            {
                Context.BaseViewType = p_viewType;
                OnBaseViewChanged?.Invoke(p_viewType);
                return true;
            }

            if (!TryApplyViewMode(p_viewType))
                return false;

            Context.BaseViewType = p_viewType;
            OnBaseViewChanged?.Invoke(p_viewType);

            return true;
        }

        public bool TrySetAim(bool p_isActive)
        {
            if (!IsBound) return false;

            // QuarterView에서는 Aim 입력이 활성화되어도
            // Camera는 QuarterView를 유지한다.
            bool canUseAimView = p_isActive && Context.BaseViewType != ECameraViewType.Quarter;

            ECameraViewType targetViewType =
                canUseAimView? ECameraViewType.Aim : Context.BaseViewType;

            if (Context.CurrentViewType == targetViewType)
                return true;

            return TryApplyViewMode(targetViewType);
        }

        // 실제 Mode 교체에 성공한 뒤 현재 View 상태를 확정한다.
        private bool TryApplyViewMode(ECameraViewType p_viewType)
        {
            ICameraViewMode targetViewMode =
                p_viewType switch
                {
                    ECameraViewType.ThirdPerson =>
                        _thirdPersonViewMode,

                    ECameraViewType.Aim =>
                        _aimViewMode,

                    ECameraViewType.Quarter =>
                        _quarterViewMode,

                    _ => null
                };

            if (targetViewMode == null || !ViewFlow.TrySetViewMode(targetViewMode))
            {
                return false;
            }

            Context.SetCurrentView(p_viewType);
            return true;
        }
    }
}
