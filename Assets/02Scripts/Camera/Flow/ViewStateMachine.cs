using alpha.camera;
using System.Collections.Generic;
using UnityEngine;

// Flow : CurrentViewState -> TransitionViewState -> NextViewState

public enum EViewType
{
    ThirdPerson,
    Aim,
    Quarter,
}

public class ViewStateMachine : MonoBehaviour
{
    private UIManager _uiModule;

    // ViewType으로 정의하기보단 별도로 관리
    private TransitionViewState _transitionState;

    private ViewStateBase _currentViewState;
    public ViewStateBase NextViewState => _nextViewState;
    private ViewStateBase _nextViewState;

    // TransitionViewState에서 전달하기 위한 데이터 저장
    public ViewDataSO NextData => _nextData;
    private ViewDataSO _nextData;

    public ViewDataSO CurrentData => _currentData;
    private ViewDataSO _currentData;

    public EViewType CurrentViewType => _currentType;
    private EViewType _currentType;

    public EViewType PrevType => _previousType;
    private EViewType _previousType;

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

        _transitionState = new TransitionViewState();
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
        StateUI state = _uiModule.Get<StateUI>();
        state.ChangeViewText(($"{_currentViewState}"));
        _currentViewState?.LateUpdate();
    }

    public Vector3 GetLookDirection()
    {
        return _currentViewState?.GetLookDirection()?? transform.forward;
    }

    public void SetView(EViewType type)
    {
        _previousType = _currentType;

        _nextData = _viewDataDic[type];
        _nextViewState = _viewDic[type];

        _currentViewState?.Exit();

        _currentViewState = _transitionState;
        _currentViewState?.Enter();
    }


    public void EndTransition()
    {
        _currentViewState?.Exit();

        _currentViewState = _nextViewState;
        _currentData = _nextData;
        _currentType = _nextData.ViewType;

        _currentViewState.Enter();
    }
}