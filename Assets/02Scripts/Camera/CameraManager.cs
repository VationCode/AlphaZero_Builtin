using System;
using UnityEngine;
using UnityEngine.InputSystem.HID;

namespace alpha.camera
{
    public enum EViewType
    {
        BackView,
        ShoulderView,
        TopDownView
    }

    public class CameraManager : MonoBehaviour
    {
        #region Ref Component
        [SerializeField]
        private InputBoundary _input;
        #endregion

        #region ==================== Config 
        [Header("[Targets]")]
        [Tooltip("타겟과 동일한 위치 추적만 담당")]
        [SerializeField] private Transform _cameraRigTr;
        [Tooltip("View에 따른 높이와 회전 담당")]
        [SerializeField] private Transform _cameraPivotTr;
        [Tooltip("TopView일때만 회전담당")]
        [SerializeField] private Transform _cameraTopviewTr;
        [SerializeField] private Transform _cameraShoulderTr;
        [SerializeField] private Transform _cameraZoomHolderTr;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _followTarget;        // Player

        [Header("[View Data]")]
        [SerializeField] private ViewDataSO _backViewData;
        [SerializeField] private ViewDataSO _shoulderViewData;
        [SerializeField] private ViewDataSO _topDownViewData;

        [Header("Rotation")]
        [SerializeField] private float _sensitivity = 15;       // 마우스 감도
        [SerializeField] private float _clampAngle = 70;        // 회전 제한 (X축)

        [Header("[Follw Speed]")]
        [SerializeField] private float _viewSmoothSpeed = 5f;
        [SerializeField] private float _rigFollowSpeed = 25f;
        [SerializeField] private float _zoomSmoothSpeed = 5f;
        [SerializeField] private float _zoomFollowSeed = 1f;

        [Header("Collision")]
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField] private float _collisionPadding = 0.1f;
        #endregion ==================== /Config 

        #region ==================== Runtime
        [Header("[View Type]")]
        [SerializeField] private EViewType _currentViewType;
        private ViewDataSO _currentViewData
        {
            get
            {
                return _currentViewType switch
                {
                    EViewType.BackView => _backViewData,
                    EViewType.ShoulderView => _shoulderViewData,
                    EViewType.TopDownView => _topDownViewData,
                    _ => _backViewData
                };
            }
        }
        private float _currentZoomMin => _currentViewData.ZoomMinDistance;
        private float _currentZoomMax => _currentViewData.ZoomMaxDistance;

        private float _currentX;
        private float _currentY;
        private float _currentZoomDis;

        private float _targetPivotY;
        private float _targetRollX;
        private float _targetFOV;
        private bool _isViewTransition;
        #endregion ==================== /Runtime

        private void Awake()
        {
            _cameraRigTr = this.transform;
        }

        private void Start()
        {
            _camera = GetComponentInChildren<Camera>();

            if (_followTarget == null)
            {
                Debug.LogError("CameraMovementModule : Target is not set.");
                return;
            }
            _cameraRigTr.position = Vector3.zero;
            _cameraPivotTr.localPosition = Vector3.zero;
            _cameraPivotTr.localRotation = Quaternion.identity;
            _cameraShoulderTr.localPosition = Vector3.zero;
            _cameraZoomHolderTr.position = Vector3.zero;

            _currentZoomDis = _backViewData.ZoomMaxDistance;
            _currentViewType = EViewType.TopDownView;
            SetView(EViewType.BackView);
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Alpha8))
            {
                SetView(EViewType.BackView);
            }
            else if(Input.GetKeyDown(KeyCode.Alpha9))
            {
                SetView(EViewType.ShoulderView);
            }
            else if(Input.GetKeyDown(KeyCode.Alpha0))
            {
                SetView(EViewType.TopDownView);
            }

            if(!_isViewTransition)
                Rotation();
        }

        private void LateUpdate()
        {
            if (_isViewTransition)
                SwitchView();

            if (_currentViewType == EViewType.TopDownView)
                TopViewFollow();
            else
                TPSFollow();
        }

        public void SetView(EViewType p_viewType)
        {
            if (_currentViewType == p_viewType) return;
            _currentViewType = p_viewType;

            _isViewTransition = true;

            _targetPivotY = _currentViewData.PivotOffsetY;
            _targetRollX = _currentViewData.ShoulderOffsetX;
            _targetFOV = _currentViewData.FOV;
            _currentZoomDis = _currentViewData.ZoomMaxDistance;

            // 회전값을 복구시켜놔야 데이터값으로 전환시 화면구도 계산이 제대로 나옴
            if (_currentViewType == EViewType.TopDownView)
            {
                _currentX = 0f;
                _currentY = 0f;
            }
        }

        private void SwitchView()
        {
            Vector3 zoomTarget = Vector3.back * _currentZoomDis;        //음수로 전환
            Vector3 pivotTargetPos = new Vector3(0, _targetPivotY, 0);
            Vector3 shoulderTargetPos = new Vector3(_targetRollX, 0, 0);

            _cameraPivotTr.localPosition =
                Vector3.Lerp(_cameraPivotTr.localPosition, pivotTargetPos, Time.deltaTime * _viewSmoothSpeed);

            _cameraShoulderTr.localPosition = 
                Vector3.Lerp(_cameraShoulderTr.localPosition, shoulderTargetPos, Time.deltaTime * _viewSmoothSpeed);

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFOV, Time.deltaTime * _viewSmoothSpeed);

            _cameraZoomHolderTr.localPosition = 
                Vector3.Lerp(_cameraZoomHolderTr.localPosition, zoomTarget, Time.deltaTime * _viewSmoothSpeed);

            // 탑뷰 전환시 _cameraPivotTr의 회전값을 정상으로 만들고 _cameraTopviewTr로만 회전시킴
            if (_currentViewType == EViewType.TopDownView)
            {
                _cameraPivotTr.localRotation =
                    Quaternion.Lerp(_cameraPivotTr.localRotation, Quaternion.identity, Time.deltaTime * _viewSmoothSpeed);

                Quaternion angle = Quaternion.Euler(new Vector3(_topDownViewData.Angle, 0, 0));
                _cameraTopviewTr.localRotation = 
                    Quaternion.Lerp(_cameraTopviewTr.localRotation, angle, Time.deltaTime * _viewSmoothSpeed);
            }
            else
            {
                _cameraTopviewTr.localRotation =
                    Quaternion.Lerp(_cameraTopviewTr.localRotation, Quaternion.identity, Time.deltaTime * _viewSmoothSpeed);
            }

            bool pivotPosDone = Vector3.Distance(_cameraPivotTr.localPosition, pivotTargetPos) < 0.01f;
            bool rollPosDone = Vector3.Distance(_cameraShoulderTr.localPosition, shoulderTargetPos) < 0.01f;
            bool zoomDone = Vector3.Distance(_cameraZoomHolderTr.localPosition, zoomTarget) < 0.01f;

            bool fovDone = Mathf.Abs(_camera.fieldOfView - _targetFOV) < 0.1f;

            _isViewTransition = !(pivotPosDone && rollPosDone && fovDone && zoomDone);
        }


        public void RigFollw()
        {
            float speed = _isViewTransition? _viewSmoothSpeed : _rigFollowSpeed;

            if (_isViewTransition)
            {
                _cameraRigTr.position = _followTarget.position;
                return;
            }


            if (_currentViewType == EViewType.BackView)
                _cameraRigTr.position = Vector3.Lerp(_cameraRigTr.position, _followTarget.position, Time.deltaTime * speed);
            else
                _cameraRigTr.position = _followTarget.position;
        }


        public void TopViewFollow()
        {
            RigFollw();
        }

        public void TPSFollow()
        {
            RigFollw();

            if (_isViewTransition) return;

            ZoomFollow();
        }

        public void ZoomFollow()
        {
            if (_currentViewType != EViewType.BackView) return;

            if (Mathf.Abs(_input.MouseScroll.y) > 0.01f)
            {
                _currentZoomDis -= _input.MouseScroll.y * _zoomFollowSeed;

                _currentZoomDis = Mathf.Clamp(_currentZoomDis, _currentZoomMin, _currentZoomMax);
            }

            Vector3 desiredLocalPos = Vector3.back * _currentZoomDis;

            Vector3 desiredWorldPos = _cameraShoulderTr.TransformPoint(desiredLocalPos);
            Vector3 finalLocalPos = desiredLocalPos;

            // 카메라와 타겟 사이에 장애물이 있는지 체크
            RaycastHit hit;

            if (Physics.Linecast(_cameraShoulderTr.position, desiredWorldPos, out hit, _collisionMask))
            {
                float collisionDistance = Mathf.Clamp(hit.distance - _collisionPadding, _currentZoomMin, _currentZoomDis);
                finalLocalPos = Vector3.back * collisionDistance;
            }

            _cameraZoomHolderTr.localPosition =
                Vector3.Lerp(_cameraZoomHolderTr.localPosition, finalLocalPos, Time.deltaTime * _zoomSmoothSpeed);
        }

        public void Rotation()
        {
            if (_currentViewType == EViewType.TopDownView)
                return;

            _currentX -= _input.LookInput.y * _sensitivity * Time.deltaTime;
            _currentY += _input.LookInput.x * _sensitivity * Time.deltaTime;

            _currentX = Mathf.Clamp(_currentX, -_clampAngle, _clampAngle);

            Quaternion _rot = Quaternion.Euler(_currentX, _currentY, 0);
            _cameraPivotTr.localRotation = _rot;
        }
    }
}