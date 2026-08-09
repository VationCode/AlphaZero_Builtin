using System;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Camera 내부 기능을 조립하고 외부 진입점을 제공한다.
    public class CameraCore : MonoBehaviour
    {
        public AlphaInputSystem InputSystem { get; private set; }
        public CameraViewFlow ViewFlow { get; private set; }

        public CameraViewModule ViewModule { get; private set; }
        public CameraShakeModule ShakeModule { get; private set; }

        public CameraContext Context { get; } = new();
        public Camera RenderCamera => ViewModule.RenderCamera;



        public event Action<ECameraViewType> OnViewRequested;
        public event Action<ECameraViewType, ECameraViewType> OnViewTransitionStarted;
        public event Action<ECameraViewType> OnViewTransitionCompleted;

        // 같은 GameObject의 Camera View 구성 요소를 캐시한다.
        private void Awake()
        {
            ViewFlow = GetComponent<CameraViewFlow>();
            ViewModule = GetComponent<CameraViewModule>();
            ShakeModule = GetComponent<CameraShakeModule>();
        }


        // Camera View를 초기화하고 입력과 View Flow의 의존성을 연결한다.
        public bool Bind(AlphaInputSystem p_input)
        {
            if (ViewFlow == null ||
                ViewModule == null ||
                ShakeModule == null)
                return false;

            if (!ViewModule.Initialize() ||
                !ShakeModule.Initialize())
                return false;

            ViewFlow.Bind(this, ViewModule, p_input);

            InputSystem = p_input;

            return true;
        }

        // RequestView 요청을 받아 담당 흐름으로 전달한다.
        public void RequestView(ECameraViewType p_viewType)
        {
            OnViewRequested?.Invoke(p_viewType);
        }

        // 외부 View가 전달한 표현 설정으로 Camera Shake를 요청한다.
        public void RequestShake(
            in CameraShakeSetting p_setting)
        {
            ShakeModule?.Play(p_setting);
        }

        // CameraViewFlow만 전환 생명주기 이벤트를 확정할 수 있다.
        internal void NotifyViewTransitionStarted(
            ECameraViewType p_fromViewType,
            ECameraViewType p_targetViewType)
        {
            OnViewTransitionStarted?.Invoke(p_fromViewType, p_targetViewType);
        }

        internal void NotifyViewTransitionCompleted(ECameraViewType p_viewType)
        {
            OnViewTransitionCompleted?.Invoke(p_viewType);
        }


#if UNITY_EDITOR

        // Editor Context Menu에서 3인칭 View 전환을 시험한다.
        [ContextMenu("Camera Test/ThirdPerson")]
        private void TestThirdPerson()
        {
            RequestView(ECameraViewType.ThirdPerson);
        }

        // Editor Context Menu에서 조준 View 전환을 시험한다.
        [ContextMenu("Camera Test/Aim")]
        private void TestAim()
        {
            RequestView(ECameraViewType.Aim);
        }

        // Editor Context Menu에서 쿼터 View 전환을 시험한다.
        [ContextMenu("Camera Test/Quarter")]
        private void TestQuarter()
        {
            RequestView(ECameraViewType.Quarter);
        }
#endif
    }
}
