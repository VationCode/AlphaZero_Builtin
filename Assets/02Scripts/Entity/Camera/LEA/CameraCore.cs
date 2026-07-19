using Alpha.Mouse;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    public class CameraCore : MonoBehaviour
    {
        private CameraRigModule _rigModule;
        private CameraViewFlow _viewFlow;
        private MouseSystem _mouseSystem;

        public ECameraViewType? CurrentViewType => _viewFlow.CurrentViewType;

        public UnityEngine.Camera RenderCamera => _rigModule.RenderCamera;

        private void Awake()
        {
            _rigModule = GetComponent<CameraRigModule>();

            _viewFlow = GetComponent<CameraViewFlow>();
        }

        public void Bind(AlphaInputSystem p_input, Transform p_followTarget, MouseSystem p_mouseSystem)
        {
            _rigModule.BindTarget(p_followTarget);
            _viewFlow.Bind(p_input, _rigModule, p_mouseSystem);
        }

        public void RequestView(ECameraViewType p_viewType)
        {
            _viewFlow.RequestView(p_viewType);
        }

        // 화면 중앙을 향하는 Ray를 반환한다.
        public Ray GetCenterRay()
        {
            return _rigModule.GetCenterRay();
        }
    }
}
