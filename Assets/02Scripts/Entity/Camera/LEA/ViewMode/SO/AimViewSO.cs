using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Aim ViewMode의 구도와 조작 설정을 보관한다.
    [CreateAssetMenu(fileName = "AimView", menuName = "Alpha/Camera/Aim Profile")]
    public class AimViewSO : ScriptableObject
    {
        [SerializeField]
        private CameraViewSettings _viewSettings = new();

        [SerializeField]
        private CameraOrbitSettings _orbitSettings = new();

        public CameraViewSettings ViewSettings => _viewSettings;

        public CameraOrbitSettings OrbitSettings => _orbitSettings;

        private void OnValidate()
        {
            _viewSettings ??= new CameraViewSettings();
            _orbitSettings ??= new CameraOrbitSettings();

            _viewSettings.Validate();
            _orbitSettings.Validate();
        }
    }
}
