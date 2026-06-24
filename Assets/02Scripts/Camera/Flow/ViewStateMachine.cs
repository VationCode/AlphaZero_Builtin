using alpha.camera;
using System.Collections.Generic;
using UnityEngine;

public enum EViewType
{
    ThirdPerson,
    Aim,
    Quarter,
}

public class ViewStateMachine : MonoBehaviour
{
    private UIModule _uiModule;

    private TransitionState _transitionState;

    private ViewStateBase _currentView;
    public ViewStateBase NextView => _nextView;
    private ViewStateBase _nextView;

    public ViewDataSO NextData => _nextData;
    private ViewDataSO _nextData;

    // 데이터 목록 저장후 타입으로 Value 호출
    private Dictionary<EViewType, ViewStateBase> _viewDic;
    private Dictionary<EViewType, ViewDataSO> _viewDataDic;

    private void Awake()
    {
        _viewDic = new()
        {
            {EViewType.ThirdPerson, new ThirdPersonView() },
            {EViewType.Aim, new AimView() },
            {EViewType.Quarter, new QuarterView() },
        };

        _transitionState = new TransitionState();
    }

    public void Bind(CameraCore p_core)
    {
        foreach (var state in _viewDic.Values)
        {
            state.Initialize(p_core);
        }
        _uiModule = p_core.UIModule;

        _viewDataDic = new()
        {
            { EViewType.ThirdPerson, p_core.TPSViewData },
            { EViewType.Aim, p_core.AimViewData },
            { EViewType.Quarter, p_core.QuarterViewData }
        };

        _transitionState.Initialize(p_core);
    }
    private void Start()
    {
        SetView(EViewType.ThirdPerson);
    }

    private void LateUpdate()
    {
        _uiModule.ChangeViewText(($"{_currentView}"));
        _currentView?.LateUpdate();
    }

    public Vector3 GetLookDirection()
    {
        return _currentView?.GetLookDirection()?? transform.forward;
    }

    public void SetView(EViewType type)
    {
        _nextData = _viewDataDic[type];

        _nextView = _viewDic[type];

        _currentView?.Exit();

        _currentView = _transitionState;

        _currentView.Enter();
    }


    public void EndTransition()
    {
        _currentView?.Exit();

        _currentView = _nextView;

        _currentView.Enter();
    }
}