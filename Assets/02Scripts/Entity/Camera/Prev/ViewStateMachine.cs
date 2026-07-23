using System.Collections.Generic;
using UnityEngine;

// Flow : CurrentViewState -> TransitionViewState -> NextViewState

public class ViewStateMachine : MonoBehaviour
{
    private UIManager _uiModule;

    // ViewType으로 정의하기보단 별도로 관리
    private TransitionViewState _transitionState;

    private ViewStateBase _currentViewState;
    public ViewStateBase NextViewState => _nextViewState;
    private ViewStateBase _nextViewState;

    // TransitionViewState에서 전달하기 위한 데이터 저장
    public CameraViewDataSO NextData => _nextData;
    private CameraViewDataSO _nextData;

    public CameraViewDataSO CurrentData => _currentData;
    private CameraViewDataSO _currentData;

    public ECameraViewType CurrentViewType => _currentType;
    private ECameraViewType _currentType;

    public ECameraViewType PrevType => _previousType;
    private ECameraViewType _previousType;

    // 데이터 목록 저장후 타입으로 Value 호출
    private Dictionary<ECameraViewType, ViewStateBase> _viewDic;
    private Dictionary<ECameraViewType, CameraViewDataSO> _viewDataDic;

    private void Awake()
    {
        _viewDic = new()
        {
            {ECameraViewType.ThirdPerson, new ThirdPersonView() },
            {ECameraViewType.Aim, new AimView() },
            {ECameraViewType.Quarter, new QuarterView() },
        };

        _transitionState = new TransitionViewState();
    }

    public void Bind(CameraCore_Prev p_core)
    {
        foreach (var state in _viewDic.Values)
        {
            state.Initialize(p_core);
        }
        _uiModule = p_core.UIModule;

        _viewDataDic = new()
        {
            { ECameraViewType.ThirdPerson, p_core.TPSViewData },
            { ECameraViewType.Aim, p_core.AimViewData },
            { ECameraViewType.Quarter, p_core.QuarterViewData }
        };

        _transitionState.Initialize(p_core);
    }
    private void Start()
    {
        SetView(ECameraViewType.ThirdPerson);
    }

    private void LateUpdate()
    {
        /*StateUI state = _uiModule.Get<StateUI>();
        state.ChangeViewText(($"{_currentViewState}"));*/
        _currentViewState?.LateUpdate();
    }

    public Vector3 GetLookDirection()
    {
        return _currentViewState?.GetLookDirection()?? transform.forward;
    }

    public void SetView(ECameraViewType type)
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