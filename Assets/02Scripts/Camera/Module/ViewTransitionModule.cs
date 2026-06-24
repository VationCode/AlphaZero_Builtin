using UnityEngine;
using UnityEngine.Windows;

public class ViewTransitionModule : MonoBehaviour
{
    [Header("[Camera Config]")]
    [SerializeField] private Transform _cameraRigTr;            // 타겟 위치
    [SerializeField] private Transform _cameraPivotTr;          // 지정 높이
    //[SerializeField] private Transform _cameraTopviewTr;
    [SerializeField] private Transform _cameraShoulderTr;       // Pivot위치에서 좌우 거리
    [SerializeField] private Transform _cameraZoomHolderTr;     
    [SerializeField] private Camera _camera;
    [Space(10)]

    [Header("Target")]
    [SerializeField] private Transform _followTarget;        // Player

    [Header("Rotation")]
    [SerializeField] private float _sensitivity = 15;       // 마우스 감도
    [SerializeField] private float _clampAngle = 70;        // 회전 제한 (X축)

    [Header("[Speed]")]
    [SerializeField] private float[] _rigFollowSpeed;       // View에 따라서 달라지기에
    [SerializeField] private float _viewSmoothSpeed = 5f;   // 추후 뷰전환 상황에 따라도 SmoothSpeed 구분
    [SerializeField] private float _zoomScrollSpeed = 1f;
    [SerializeField] private float _zoomFollowSpeed = 5f;


    [Header("Collision")]
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private float _collisionPadding = 0.1f;

    // RunTime
    private float _currentMouseX;
    private float _currentMouseY;
    private float _currentZoomDis;

    private void Awake()
    {
        _cameraRigTr.position = Vector3.zero;
        _cameraPivotTr.position = Vector3.zero;
        _cameraPivotTr.rotation = Quaternion.identity;
        _cameraShoulderTr.position = Vector3.zero;
        _cameraZoomHolderTr.position = Vector3.zero;
    }

    public void Bind(CameraCore p_core)
    {
        
    }

    public bool ViewTransition(ViewDataSO p_viewInfo)
    {
        RigFollow(true);

        bool isPivotPosDone = TrasitionPivotPosition(p_viewInfo);
        
        bool isPivotRotDone = true;
        if (p_viewInfo.ViewType == EViewType.Quarter) isPivotRotDone = TrasitionPivotRotation(p_viewInfo);

        bool isShoulderPosDone = TrasitionShoulderPosition(p_viewInfo);
        bool isZoomPosDone = TrasitionZoom(p_viewInfo);
        bool isFOVDone = TrasitionFOV(p_viewInfo);

        return isPivotPosDone && isPivotRotDone && isShoulderPosDone && isZoomPosDone && isFOVDone;
    }

    private void RigFollow(bool isInstant = false, float p_rigFollowSpeed = 0)
    {
        if (isInstant) 
            _cameraRigTr.position = _followTarget.position;
        else
            _cameraRigTr.position = Vector3.Lerp(_cameraRigTr.position, _followTarget.position, Time.deltaTime * p_rigFollowSpeed);
    }

    // ==================== View 전환 시 사용
    private bool TrasitionPivotPosition(ViewDataSO p_viewInfo)
    {
        Vector3 targetPivot = 
            new Vector3(_cameraPivotTr.localPosition.x, p_viewInfo.PivotOffsetY, _cameraPivotTr.localPosition.z);

        _cameraPivotTr.localPosition =
                Vector3.Lerp(_cameraPivotTr.localPosition, targetPivot, Time.deltaTime * _viewSmoothSpeed);

        if(Vector3.Distance(_cameraPivotTr.localPosition, targetPivot) < 0.01f)
        {
            _cameraPivotTr.localPosition = targetPivot;
            return true;
        }
        return false;
    }

    // QuarterView만 사용
    private bool TrasitionPivotRotation(ViewDataSO p_viewInfo)
    {
        var angleData = p_viewInfo.Angle;
        Quaternion targetAngle = Quaternion.Euler(new Vector3(angleData, 0, 0));

        _cameraPivotTr.localRotation =
            Quaternion.Lerp(_cameraPivotTr.localRotation, targetAngle, Time.deltaTime * _viewSmoothSpeed);

        if(Quaternion.Angle(_cameraPivotTr.localRotation, targetAngle) < 0.1f)
        {
            _cameraPivotTr.localRotation = targetAngle;
            return true;
        }
        return false;
    }

    private bool TrasitionShoulderPosition(ViewDataSO p_viewInfo)
    {
        var targetShoulderPos = 
            new Vector3(p_viewInfo.ShoulderOffsetX, _cameraShoulderTr.localPosition.y, _cameraShoulderTr.localPosition.z);

        _cameraShoulderTr.localPosition =
                Vector3.Lerp(_cameraShoulderTr.localPosition, targetShoulderPos, Time.deltaTime * _viewSmoothSpeed);

        if(Vector3.Distance(_cameraShoulderTr.localPosition, targetShoulderPos) < 0.01f)
        {
            _cameraShoulderTr.localPosition = targetShoulderPos;
            return true;
        }
        return false;
    }
    private bool TrasitionFOV(ViewDataSO p_viewInfo)
    {
        var targetFOV = p_viewInfo.FOV;

        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, Time.deltaTime * _viewSmoothSpeed);

        if(Mathf.Abs(_camera.fieldOfView - targetFOV) < 0.1f)
        {
            _camera.fieldOfView = targetFOV;
            return true;
        }
        return false;
    }

    public bool TrasitionZoom(ViewDataSO p_viewInfo)
    {
        // 음수로 전환
        var targetZoomPos = 
            new Vector3(_cameraZoomHolderTr.localPosition.x, _cameraZoomHolderTr.localPosition.y, -p_viewInfo.ZoomMaxDistance);

        _cameraZoomHolderTr.localPosition =
                Vector3.Lerp(_cameraZoomHolderTr.localPosition, targetZoomPos, Time.deltaTime * _viewSmoothSpeed);

        if(Vector3.Distance(_cameraZoomHolderTr.localPosition, targetZoomPos) < 0.01f)
        {
            _cameraZoomHolderTr.localPosition = targetZoomPos;
            return true;
        }
        return false;
    }

    // ==================== RunTime
    public void RunTimeTPSView(Vector2 p_inputLookDir, float p_scrollInput, ViewDataSO p_viewData)
    {
        RigFollow(false, _rigFollowSpeed[(int)EViewType.ThirdPerson]);
        RunTimePivotRotation(false, p_inputLookDir);
        RunTimeZoomPosition(p_scrollInput, p_viewData.ZoomMinDistance, p_viewData.ZoomMaxDistance);
    }

    public void RunTimeAimView(Vector2 p_inputLookDir)
    {
        RigFollow(false, _rigFollowSpeed[(int)EViewType.Aim]);
        RunTimePivotRotation(false, p_inputLookDir);
    }

    public void RunTimeQuarterView()
    {
        RigFollow(false, _rigFollowSpeed[(int)EViewType.Quarter]);
    }
    public void RunTimePivotRotation(bool p_isFix, Vector2 p_inputLookDir)
    {
        if (p_isFix) return;

        _currentMouseX -= p_inputLookDir.y * _sensitivity * Time.deltaTime;
        _currentMouseY += p_inputLookDir.x * _sensitivity * Time.deltaTime;

        _currentMouseX = Mathf.Clamp(_currentMouseX, -_clampAngle, _clampAngle);

        Quaternion _rot = Quaternion.Euler(_currentMouseX, _currentMouseY, 0);
        _cameraPivotTr.localRotation = _rot;
    }

    public void SetCurrentZoomDistance(float dis)
    {
        _currentZoomDis = dis;
    }

    // TPSView에서만 호출;
    public void RunTimeZoomPosition(float scrollInput, float minDistance, float maxDistance)
    {
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            _currentZoomDis -= scrollInput * _zoomScrollSpeed;

            _currentZoomDis = Mathf.Clamp(_currentZoomDis, minDistance, maxDistance);
        }

        Vector3 desiredLocalPos =
        Vector3.back * _currentZoomDis;

        Vector3 desiredWorldPos = _cameraShoulderTr.TransformPoint(desiredLocalPos);

        Vector3 finalLocalPos = desiredLocalPos;

        // 플레이어와 카메라 사이에 사물 감지
        if (Physics.Linecast(_cameraShoulderTr.position, desiredWorldPos, out RaycastHit hit, _collisionMask))
        {
            float collisionDistance =
                Mathf.Clamp(hit.distance - _collisionPadding, minDistance, _currentZoomDis);

            finalLocalPos = Vector3.back * collisionDistance;
        }

        _cameraZoomHolderTr.localPosition =
            Vector3.Lerp(_cameraZoomHolderTr.localPosition, finalLocalPos, Time.deltaTime * _zoomFollowSpeed);
    }

    // QuarterView에서만 호출
    public void MouseWorldDirection()
    {

    }
}
