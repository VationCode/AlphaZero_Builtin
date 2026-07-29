using UnityEngine;

namespace Alpha.AlphaCamera
{
    // ThirdPerson ViewMode의 구도와 조작 설정을 보관한다.
    [CreateAssetMenu(fileName = "ThirdPersonView", menuName = "Alpha/Camera/Third Person Profile")]
    public class ThirdPersonViewSO : ScriptableObject
    {
        [SerializeField]
        private CameraViewSettings _viewSettings = new();

        [SerializeField]
        private CameraOrbitSettings _orbitSettings = new();

        [SerializeField]
        private float _initialPitch;

        public CameraViewSettings ViewSettings => _viewSettings;

        public CameraOrbitSettings OrbitSettings => _orbitSettings;

        public float InitialPitch => _initialPitch;

        private void OnValidate()
        {
            _viewSettings ??= new CameraViewSettings();
            _orbitSettings ??= new CameraOrbitSettings();

            _viewSettings.Validate();
            _orbitSettings.Validate();

            _initialPitch = _orbitSettings.ClampPitch(_initialPitch);
        }
    }
}
