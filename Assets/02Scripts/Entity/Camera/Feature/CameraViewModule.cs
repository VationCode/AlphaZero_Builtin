using System;
using System.Collections.Generic;
using UnityEngine;



namespace Alpha.AlphaCamera
{
    // View 전환 시작 시점의 Rig 위치·회전·카메라 값을 묶는다.
    [Serializable]
    public struct CameraTransitionPose
    {
        public Vector3 PivotPosition;
        public Quaternion PivotRotation;
        public Vector3 ShoulderPosition;
        public Vector3 ZoomPosition;

        public float FieldOfView;
        public float RigFollowSpeed;
    }

    // 카메라 Profile 조회와 Rig 추적·전환·회전·줌을 실제로 적용한다.
    public class CameraViewModule : MonoBehaviour
    {
        [Header("Rig Hierachy")]
        [SerializeField] private Transform _cameraRig;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _cameraShoulder;
        [SerializeField] private Transform _cameraZoom;
        public Camera RenderCamera => _renderCamera;
        [SerializeField] private Camera _renderCamera;

        [Header("Target")]
        [SerializeField] private Transform _rigFollowTarget;

        [Header("Profile")]
        [SerializeField] private CameraViewSO[] _viewProfiles;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float _defaultTransitionDuration = 0.2f;
        [SerializeField] private CameraViewTransitionSetting[] _transitionSettings;

        [Header("Rotation")]
        [SerializeField] private float _baseSensitivity = 0.06f;
        [SerializeField] private float _minPitch = -60f;
        [SerializeField] private float _maxPitch = 60f;


        // View 검색용
        private readonly Dictionary<ECameraViewType, CameraViewSO> _viewProfileDict = new();

        public CameraViewSO CurrentView => _currentView;
        private CameraViewSO _currentView;
        private CameraViewSO _targetView;  // 전환할 목표 View



        private float _currentRigFollowSpeed;

        private float _transitionElapsedTime;   // 경과 시간
        private float _transitionDuration;

        private CameraTransitionPose _transitionStart;
        private Quaternion _targetPivotRotation;

        public bool IsInitialized { get; private set; }

        // Rig 참조를 확인하고 ViewType별 Profile 검색 캐시를 구성한다.
        public bool Initialize()
        {
            // 이미 성공한 초기화는 다시 수행하지 않는다.
            if (IsInitialized) return true;

            if (_cameraRig == null || _cameraPivot == null ||
                _cameraShoulder == null || _cameraZoom == null ||
                _renderCamera == null || _rigFollowTarget == null ||
                _viewProfiles == null)
            {
                return false;
            }

            _viewProfileDict.Clear();

            // null 또는 중복 ViewType이 있으면 잘못된 설정으로 처리한다.
            foreach (CameraViewSO profile in _viewProfiles)
            {
                if (profile == null || _viewProfileDict.ContainsKey(profile.ViewType))
                {
                    return false;
                }

                _viewProfileDict.Add(profile.ViewType, profile);
            }

            IsInitialized = true;
            return true;
        }


        #region ============================== Rig Follow
        // 현재 View의 Follow 속도로 Rig를 Target 위치에 부드럽게 추적시킨다.
        public void UpdateRigFollow()
        {
            if (!IsInitialized || _rigFollowTarget == null || _currentView == null)
            {
                return;
            }

            // 프레임률에 비교적 독립적인 추적 보간값
            // FollowSpeed가 높을수록 followT가 빠르게 1에 가까워져 Target을 더 빠르게 따라갑니다.
            float followT = 1f - Mathf.Exp(-_currentRigFollowSpeed * Time.deltaTime);

            // Rig는 Target의 월드 위치만 추적한다.
            _cameraRig.position = Vector3.Lerp(_cameraRig.position, _rigFollowTarget.position, followT);
        }
        #endregion ============================== /Rig Follow

        #region ============================== Transition View
        // ViewType이 존재하는지 확인
        public bool HasViewProfile(ECameraViewType p_viewType)
        {
            return IsInitialized && _viewProfileDict.ContainsKey(p_viewType);
        }

        // 현재 실제 Transform을 시작값으로 저장하고 목표 Profile을 준비한다.
        public bool TryBeginViewTransition(ECameraViewType p_viewType)
        {
            if (!IsInitialized || !_viewProfileDict.TryGetValue(p_viewType, out CameraViewSO profile))
            {
                return false;
            }

            // 연속 전환에서도 튀지 않도록 현재 실제 값을 시작 Pose로 저장한다.
            _transitionStart.PivotPosition = _cameraPivot.localPosition;
            _transitionStart.PivotRotation = _cameraPivot.localRotation;
            _transitionStart.ShoulderPosition = _cameraShoulder.localPosition;
            _transitionStart.ZoomPosition = _cameraZoom.localPosition;
            _transitionStart.FieldOfView = _renderCamera.fieldOfView;
            _transitionStart.RigFollowSpeed = _currentRigFollowSpeed;

            bool preserveDirection = _currentView != null && _currentView.ViewType != ECameraViewType.Quarter &&
                                     profile.ViewType != ECameraViewType.Quarter;

            // TPS ↔ Aim에서는 현재 바라보는 방향을 유지한다.
            _targetPivotRotation = 
                preserveDirection? _transitionStart.PivotRotation : Quaternion.Euler(profile.PivotEulerAngles);

            // 재전환 전에 기존 목표를 기준으로 타입별 시간을 결정한다.
            _transitionDuration = ResolveTransitionDuration(p_viewType);
            _targetView = profile;
            _transitionElapsedTime = 0f;

            return true;
        }

        // 현재 View와 목표 View 조합에 맞는 전환 시간을 선택한다.
        private float ResolveTransitionDuration(ECameraViewType p_targetViewType)
        {
            CameraViewSO fromView = _targetView != null
                ? _targetView
                : _currentView;

            if (fromView == null)
                return 0f;

            ECameraViewType fromViewType = fromView.ViewType;

            // 명시적인 From → To 설정을 우선한다.
            foreach (CameraViewTransitionSetting setting in _transitionSettings)
            {
                if (setting.IsDirectMatch(fromViewType, p_targetViewType))
                    return setting.Duration;
            }

            // 양방향 설정이면 To → From도 허용한다.
            foreach (CameraViewTransitionSetting setting in _transitionSettings)
            {
                if (setting.IsReverseMatch(fromViewType, p_targetViewType))
                    return setting.Duration;
            }

            return _defaultTransitionDuration;
        }

        // 경과 시간에 따라 View를 보간하고 완료 여부를 반환한다.
        public bool TransionView()
        {
            if (_targetView == null) return false;

            // 최초 View이거나 전환 시간이 0이면 즉시 적용한다.
            if (_currentView == null || _transitionDuration <= 0f)
            {
                CompleteViewTransition();
                return true;
            }

            _transitionElapsedTime += Time.deltaTime;

            float linearT = Mathf.Clamp01(_transitionElapsedTime / _transitionDuration);

            // SmoothStep 곡선으로 전환 시작과 끝의 속도를 완화한다.
            float smoothT = linearT * linearT * (3f - 2f * linearT);

            ApplyView(_targetView, smoothT);

            if (linearT < 1f) return false;

            CompleteViewTransition();

            return true;
        }

        // 시작 Pose에서 목표 Profile까지 Rig 구성 요소를 같은 비율로 보간한다.
        private void ApplyView(CameraViewSO p_target, float p_t)
        {
            // 현재의 Pivot, Shoulder, Zoom, FOV를 목표 View로 보간한다.
            _cameraPivot.localPosition = Vector3.Lerp(_transitionStart.PivotPosition, p_target.PivotLocalPosition, p_t);

            _cameraPivot.localRotation = Quaternion.Slerp(_transitionStart.PivotRotation,_targetPivotRotation, p_t);

            _cameraShoulder.localPosition = Vector3.Lerp(_transitionStart.ShoulderPosition, p_target.ShoulderLocalPosition, p_t);

            Vector3 targetZoom = new Vector3(0f, 0f, -p_target.ZoomDistance);

            _cameraZoom.localPosition = Vector3.Lerp(_transitionStart.ZoomPosition, targetZoom, p_t);

            _renderCamera.fieldOfView = Mathf.Lerp(_transitionStart.FieldOfView, p_target.FieldOfView, p_t);

            _currentRigFollowSpeed = Mathf.Lerp(_transitionStart.RigFollowSpeed, p_target.RigFollowSpeed, p_t);
        }

        // 전환 완료 후 즉시 적용
        private void CompleteViewTransition()
        {
            ApplyView(_targetView, 1f);

            _currentView = _targetView;
            _targetView = null;
        }

        // 타겟View로 즉시 전환
        private void ApplyViewImmediately(CameraViewSO p_profile)
        {
            if (p_profile == null)
                return;

            _cameraPivot.localPosition = p_profile.PivotLocalPosition;

            _cameraPivot.localRotation = Quaternion.Euler(p_profile.PivotEulerAngles);

            _cameraShoulder.localPosition = p_profile.ShoulderLocalPosition;

            // 카메라는 Zoom 기준점의 뒤쪽인 로컬 -Z에 배치한다.
            _cameraZoom.localPosition = new Vector3(0f, 0f, -p_profile.ZoomDistance);

            _renderCamera.fieldOfView = p_profile.FieldOfView;

            _currentRigFollowSpeed = p_profile.RigFollowSpeed;
        }
        #endregion ============================== /Transition View

        #region ============================== Rotation Camera

        // Look 입력을 누적 Yaw·Pitch로 변환하고 Pitch 범위를 제한한다.
        public void UpdateRotation(Vector2 p_lookInput, CameraContext p_context)
        {
            if (_currentView == null)
                return;

            float sensitivity = _baseSensitivity * _currentView.LookSensitivityMultiplier;

            float yaw = p_context.Yaw + p_lookInput.x * sensitivity;
            float pitch = p_context.Pitch - (p_lookInput.y * sensitivity);

            pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);

            p_context.SetRotation(pitch, yaw);

            _cameraPivot.localRotation =
                p_context.PivotRotation;
        }
        #endregion ============================== /Rotation Camera

        #region ============================== Zoom
        // 휠 방향을 Profile의 한 단계 거리로 변환해 Zoom 범위 안에 적용한다.
        public void UpdateZoom(float p_scrollY, CameraContext p_context)
        {
            if (_currentView == null || Mathf.Approximately(p_scrollY, 0f))
            {
                return;
            }

            // 한 번의 휠 입력을 한 단계로 처리한다.
            float scrollDirection = Mathf.Sign(p_scrollY);

            float nextDistance = Mathf.Clamp(p_context.ZoomDistance - (scrollDirection * _currentView.ZoomStep),
                                            _currentView.MinZoomDistance, _currentView.MaxZoomDistance);

            p_context.SetZoomDistance(nextDistance);

            // Pivot으로부터 로컬 -Z 방향으로 거리를 적용한다.
            _cameraZoom.localPosition = new Vector3(0f, 0f, -nextDistance);
        }
        #endregion ============================== /Zoom
    }
}
