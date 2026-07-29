using UnityEngine;
namespace Alpha.AlphaCamera
{
    // Quarter ViewMode의 고정 구도와 Zoom 설정을 보관한다.
    [CreateAssetMenu(fileName = "QuarterView", menuName = "Alpha/Camera/Quarter Profile")]
    public class QuarterViewSO : ScriptableObject
    {
        [SerializeField]
        private CameraViewSettings _viewSettings = new();

        [SerializeField]
        private Vector3 _pivotLocalEulerAngles = new(60f, 0f, 0f);
        public float PitchAngle => _pivotLocalEulerAngles.x;
        public CameraViewSettings ViewSettings => _viewSettings;

        private void OnValidate()
        {
            _viewSettings ??= new CameraViewSettings();
            _viewSettings.Validate();

            // QuarterView는 월드 +Z 방향을 화면 위쪽으로 고정한다.
            _pivotLocalEulerAngles.x = Mathf.Clamp(_pivotLocalEulerAngles.x, 1f, 90f);

            _pivotLocalEulerAngles.y = 0f;
            _pivotLocalEulerAngles.z = 0f;
        }
    }
}
