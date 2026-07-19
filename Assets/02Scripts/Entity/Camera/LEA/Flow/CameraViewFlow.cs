using System.Collections.Generic;
using UnityEngine;
using Alpha.Mouse;

// 이벤트 구독, 현재 View 교체, 실행 시점 판단
namespace Alpha.AlphaCamera
{
    public class CameraViewFlow : MonoBehaviour
    {
        private MouseSystem _mouseSystem;

        [Header("Initial View")]
        [SerializeField] private ECameraViewType _initialViewType = ECameraViewType.ThirdPerson;

        [Header("View Profiles")]
        [SerializeField] private CameraViewSO _thirdPersonProfile;
        [SerializeField] private CameraViewSO _aimProfile;
        [SerializeField] private CameraViewSO _quarterProfile;

        private readonly Dictionary<ECameraViewType, ICameraView> _viewDict = new();

        private AlphaInputSystem _input;
        private CameraRigModule _rigModule;
        private CameraContext _context;
        private ICameraView _activeView;

        public ECameraViewType? CurrentViewType => _activeView?.Type;

        public CameraContext Context => _context;

        public void Bind(AlphaInputSystem p_input, CameraRigModule p_rigModule, MouseSystem p_mouseSystem)
        {
            _input = p_input;
            _rigModule = p_rigModule;
            _context = new CameraContext();
            _mouseSystem = p_mouseSystem;

            RegisterViews();
            InitializeView();
        }

        // 현재 View를 갱신하고 계산된 Pose를 Camera Rig에 반영한다.
        private void LateUpdate()
        {
            if (_activeView == null) return;
            
            float deltaTime = Time.deltaTime;

            _rigModule.FollowTarget(_activeView.Profile.FollowSpeed, deltaTime);

            if (_rigModule.IsTransitioning)
            {
                _rigModule.UpdateTransition(deltaTime);
                return;
            }

            _activeView.Update(_context, _input, deltaTime);

            CameraPose targetPose = _activeView.GetTargetPose(_context);

            _rigModule.ApplyPose(targetPose);
        }

        // 사용할 Camera View들을 생성하고 타입별로 등록한다.
        private void RegisterViews()
        {
            _viewDict.Clear();

            _viewDict.Add(ECameraViewType.ThirdPerson, new ThirdPersonCameraView(_thirdPersonProfile, _rigModule, _mouseSystem));
            _viewDict.Add(ECameraViewType.Aim, new AimCameraView(_aimProfile, _rigModule, _mouseSystem));
            _viewDict.Add(ECameraViewType.Quarter, new QuarterCameraView(_quarterProfile, _mouseSystem));
        }

        private void InitializeView()
        {
            _activeView = _viewDict[_initialViewType];

            _context.ChangeView(_activeView.Type);
            _activeView.Enter(_context);

            _rigModule.SnapToTarget();

            CameraPose initialPose = _activeView.GetTargetPose(_context);

            _rigModule.ApplyPose(initialPose);
        }

        // 지정한 Camera View로 전환을 요청한다.
        public void RequestView(ECameraViewType p_viewType)
        {
            if (_activeView == null || _activeView.Type == p_viewType)
            {
                return;
            }

            _activeView.Exit(_context);

            _activeView = _viewDict[p_viewType];

            _context.ChangeView(_activeView.Type);
            _activeView.Enter(_context);

            CameraPose targetPose = _activeView.GetTargetPose(_context);

            _rigModule.BeginIsTransition(targetPose, _activeView.Profile.TransitionDuration, _activeView.Profile.TransitionCurve);
        }
    }
}