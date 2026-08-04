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

        public CameraContext Context { get; } = new();
        public Camera RenderCamera => ViewModule.RenderCamera;



        public event Action<ECameraViewType, float> OnViewRequested;

        private void Awake()
        {
            ViewFlow = GetComponent<CameraViewFlow>();
            ViewModule = GetComponent<CameraViewModule>();
        }


        public bool Bind(AlphaInputSystem p_input)
        {
            if (ViewFlow == null || ViewModule == null)
                return false;

            if (!ViewModule.Initialize())
                return false;

            ViewFlow.Bind(this, ViewModule, p_input);

            InputSystem = p_input;

            return true;
        }

        public void RequestView(ECameraViewType p_viewType, float p_transitionDuration)
        {
            OnViewRequested?.Invoke(p_viewType, p_transitionDuration);
        }


#if UNITY_EDITOR

        [ContextMenu("Camera Test/ThirdPerson")]
        private void TestThirdPerson()
        {
            RequestView(ECameraViewType.ThirdPerson, 0.6f);
        }

        [ContextMenu("Camera Test/Aim")]
        private void TestAim()
        {
            RequestView(ECameraViewType.Aim, 0.6f);
        }

        [ContextMenu("Camera Test/Quarter")]
        private void TestQuarter()
        {
            RequestView(ECameraViewType.Quarter, 1f);
        }
#endif
    }
}
