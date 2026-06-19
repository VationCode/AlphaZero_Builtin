using UnityEngine;

namespace alpha.camera
{
    public enum EViewType
    {
        BackView,
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
        private GameObject _cameraRig;
        [Tooltip("타겟으로부터의 거리와 회전 담당") ,SerializeField]
        private Transform _cameraPivot;
        [SerializeField]
        private Camera _camera;


        [SerializeField]
        private EViewType _currentViewType;

        [Header("[ BackView ]")]
        [SerializeField]
        private Transform _backViewTarget;      // CameraRig가 쫒을 대상
        [SerializeField]
        private Vector3 _backViewOffset;
        [SerializeField]
        private float _minDistance = 0.5f;
        [SerializeField]
        private float _maxDistance = 4.0f;
        [SerializeField]
        private float _zoomSeed = 1f;
        [SerializeField]
        private float _baseFOV = 60f;
        [SerializeField]
        private float _combatFOV = 40f;
        [SerializeField]
        private float _smoothFOVSpeed = 10f;

        [SerializeField]
        private LayerMask _collisionMask;
        [SerializeField]
        private float _collisionPadding = 0.1f;

        [Tooltip("감도"), SerializeField]
        private float _sensitivity = 15;
        [Tooltip("각도 제한"), SerializeField]
        private float _clampAngle = 70;

        private float _smoothness = 10f;
        [Space(10)]

        [Header("[ TopDownView ]")]
        [SerializeField]
        private Transform _topDownViewTarget;
        [SerializeField]
        private Vector3 _topviewOffset;
        [SerializeField]
        private Vector3 _topviewAngleOffset;

        [SerializeField]
        private float _topViewFOV = 70f;

        [Header("[ Public ]")]
        [SerializeField]
        private float _smoothSpeed = 5f;

        [SerializeField]
        private float _followSpeed = 100;

        [Space(10)]
        #endregion

        #region Runtime
        private Transform _currentTarget;

        private float _currentX;
        private float _currentY;
        private float _currentDistance;
        private float _currentFOV;
        #endregion

        private void Awake()
        {
            _cameraRig = this.gameObject;
        }

        private void Start()
        {
            _camera = GetComponentInChildren<Camera>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _currentTarget = _currentViewType == EViewType.BackView ? _backViewTarget : _topDownViewTarget;

            if (_currentTarget == null)
            {
                Debug.LogError("CameraMovementModule : Target is not set.");
                return;
            }

            InitializeCamera();

            SetViewTypeFOV(_currentViewType);
        }

        private void InitializeCamera()
        {
            _cameraRig.transform.position = _currentTarget.position;
            _cameraPivot.localPosition = Vector3.zero;

            _currentX = _cameraRig.transform.localRotation.eulerAngles.x;
            _currentY = _cameraRig.transform.localRotation.eulerAngles.y;
        }

        private void Update()
        {
            SetViewTypeFOV(_currentViewType);
            Rotation();
        }

        private void LateUpdate()
        {
            Follow();
        }
        public void SetViewType(EViewType p_type)
        {
            switch (p_type)
            {
                case EViewType.BackView:
                    _currentViewType = p_type;
                    break;
                case EViewType.TopDownView:
                    _currentViewType = p_type;
                    break;
            }
        }

        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }

        public void SetViewTypeFOV(EViewType p_viewType = EViewType.BackView)
        {
            if(_currentViewType == EViewType.TopDownView)
            {
                _camera.fieldOfView = Mathf.Lerp(_currentFOV, _topViewFOV, Time.deltaTime);
            }
            else if(_currentViewType == EViewType.BackView)
            {
                _camera.fieldOfView = Mathf.Lerp(_currentFOV, _baseFOV, Time.deltaTime);
            }
            _currentFOV = _camera.fieldOfView;
        }

        public void SetBackViewFOV(bool p_isCombat = false)
        {
            if (p_isCombat)
                _camera.fieldOfView = Mathf.Lerp(_currentFOV, _combatFOV, Time.deltaTime * _smoothFOVSpeed);
            else
                _camera.fieldOfView = Mathf.Lerp(_currentFOV, _baseFOV, Time.deltaTime * _smoothFOVSpeed);
        }

        public void Follow()
        {
           /* _cameraRig.transform.position = Vector3.MoveTowards(_cameraRig.transform.position, _currentTarget.position, _followSpeed * Time.deltaTime);

            if (Mathf.Abs(_input.MouseScroll.y) > 0.01f)
            {
                float distance = Mathf.Abs(_backViewOffset.z);

                distance -= _input.MouseScroll.y * _zoomSeed;

                distance = Mathf.Clamp(distance, _minDistance, _maxDistance);

                _backViewOffset.z = -distance;
            }

            Vector3 maxLocalPos = _backViewOffset;

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

            _cameraPivot.localPosition = Vector3.Lerp(_cameraPivot.localPosition, finalTargetLocalPos, Time.deltaTime * _smoothness);*/
        }
        public void Rotation()
        {
            _currentX -= _input.LookInput.y * _sensitivity * Time.deltaTime;
            _currentY += _input.LookInput.x * _sensitivity * Time.deltaTime;

            _currentX = Mathf.Clamp(_currentX, -_clampAngle, _clampAngle);

            Quaternion _rot = Quaternion.Euler(_currentX, _currentY, 0);
            transform.rotation = _rot;
        }


    }
}