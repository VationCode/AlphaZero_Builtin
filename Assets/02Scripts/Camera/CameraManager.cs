using System;
using UnityEngine;

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

        #region Config 
        [Tooltip("타겟과 동일한 위치 추적만 담당"), SerializeField]
        private Transform _cameraRigTr;
        [Tooltip("타겟으로부터의 거리와 회전 담당") ,SerializeField]
        private Transform _cameraPivotTr;
        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private Transform _followTarget;        // Player


        #region ==================== BackView
        [Header("[ BackView ]")]
        [SerializeField]
        private ViewDataSO _backViewData;
        [SerializeField]
        private float _minDistance = 0.5f;
        [SerializeField]
        private float _maxDistance = 3.0f;
        [SerializeField]
        private float _zoomSeed = 1f;
        [SerializeField]
        private float _smoothFollow = 10f;


        [Tooltip("감도"), SerializeField]
        private float _sensitivity = 15;
        [Tooltip("각도 제한"), SerializeField]
        private float _clampAngle = 70;

        private float _currentPivotMaxDis;
        [Space(10)]
        #endregion

        #region ==================== ShouldView (Aim)
        [Header("[ ShoulderView ]")]
        [SerializeField]
        private ViewDataSO _shoulderViewData;


        #endregion

        #region ==================== TopDownView
        [Header("[ TopDownView ]")]
        [SerializeField]
        private ViewDataSO _topDownViewData;
        #endregion


        [SerializeField]
        private EViewType _currentViewType;

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

        [Header("[ Public ]")]
        [SerializeField]
        private float _smoothSpeed = 5f;

        [SerializeField]
        private LayerMask _collisionMask;
        [SerializeField]
        private float _collisionPadding = 0.1f;

        [Space(10)]
        #endregion

        #region Runtime


        private float _currentX;
        private float _currentY;
        private float _currentDistance;

        private float _targetRigY;
        private Vector3 _targetPivotPos;
        private Quaternion _targetRot;
        private float _targetFOV;
        #endregion

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
            _cameraPivotTr.position = Vector3.zero;
            _cameraPivotTr.rotation = Quaternion.identity;

            _currentPivotMaxDis = _maxDistance;

            SetView(_currentViewType);
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

            Rotation();
        }

        private void LateUpdate()
        {
            SwitchView();

            if (_currentViewType == EViewType.TopDownView)
            {
                RigFollw();
                return; 
            }
            else TPSFollow();
        }

        public void SetView(EViewType p_viewType)
        {
            _currentViewType = p_viewType;

            switch (p_viewType)
            {
                case EViewType.BackView:

                    _currentPivotMaxDis = _backViewData.PivotOffset.z;
                    break;
                case EViewType.ShoulderView:

                    _currentPivotMaxDis = _shoulderViewData.PivotOffset.z;
                    break;

            }

            _targetRigY = _currentViewData.RigOffsetY;
            _targetPivotPos = _currentViewData.PivotOffset;
            _targetRot = Quaternion.Euler(_currentViewData.Angle);
            _targetFOV = _currentViewData.FOV;

        }

        private void SwitchView()
        {
           // _cameraRigTr.localPosition = Vector3.Lerp(_cameraRigTr.localPosition, new Vector3(_cameraRigTr.localPosition.x, _targetRigY, _cameraRigTr.localPosition.z), Time.deltaTime * _smoothSpeed);

            _cameraPivotTr.localPosition =
                Vector3.Lerp(_cameraPivotTr.localPosition, _targetPivotPos, Time.deltaTime * _smoothSpeed);

            _cameraPivotTr.localRotation = 
                Quaternion.Slerp(_cameraPivotTr.localRotation, _targetRot, Time.deltaTime * _smoothSpeed);

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFOV, Time.deltaTime * _smoothSpeed);
        }

        public void RigFollw()
        {
            Vector3 rigPos = _cameraRigTr.transform.position;

            rigPos = Vector3.Lerp(rigPos, _followTarget.position, Time.deltaTime * _smoothSpeed);

            rigPos.y = _targetRigY;

            _cameraRigTr.position = rigPos;
        }

        public void TPSFollow()
        {
            RigFollw();

            if (_currentViewType == EViewType.BackView)
            {
                if (Mathf.Abs(_input.MouseScroll.y) > 0.01f)
                {
                    float distance = Mathf.Abs(_currentPivotMaxDis);

                    distance -= _input.MouseScroll.y * _zoomSeed;

                    distance = Mathf.Clamp(distance, _minDistance, _maxDistance);

                    _currentPivotMaxDis = -distance;
                }
            }

            Vector3 maxLocalPos = Vector3.zero;

            maxLocalPos.z = _currentPivotMaxDis;

            Vector3 targetWorldPos = transform.TransformPoint(maxLocalPos);

            // 카메라와 타겟 사이에 장애물이 있는지 체크
            RaycastHit _hit;
            Vector3 finalTargetLocalPos;
            if (Physics.Linecast(transform.position, targetWorldPos, out _hit, _collisionMask))
            {
                _currentDistance = Mathf.Clamp(_hit.distance - _collisionPadding, _minDistance, maxLocalPos.magnitude);

                Vector3 dirToTarget = (maxLocalPos).normalized;
                finalTargetLocalPos = dirToTarget * _currentDistance;
            }
            else
            {
                finalTargetLocalPos = maxLocalPos;
            }

            _cameraPivotTr.localPosition = Vector3.Lerp(_cameraPivotTr.localPosition, finalTargetLocalPos, Time.deltaTime * _smoothFollow);
        }
        public void Rotation()
        {
            if (_currentViewType == EViewType.TopDownView)
            {
                _cameraPivotTr.LookAt(_followTarget);
                return;
            }

            _currentX -= _input.LookInput.y * _sensitivity * Time.deltaTime;
            _currentY += _input.LookInput.x * _sensitivity * Time.deltaTime;

            _currentX = Mathf.Clamp(_currentX, -_clampAngle, _clampAngle);

            Quaternion _rot = Quaternion.Euler(_currentX, _currentY, 0);
            transform.rotation = _rot;
        }
    }
}