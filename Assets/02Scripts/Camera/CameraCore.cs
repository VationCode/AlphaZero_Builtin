using UnityEngine;

[RequireComponent(typeof(ViewStateMachine))]
[RequireComponent(typeof(ViewTransitionModule))]
public class CameraCore : MonoBehaviour
{
    public UIModule UIModule;
    public InputBoundary Input;

    [Header("Ref")]
    // Flow
    public ViewStateMachine State;
    //Module
    public ViewTransitionModule ViewTransitionModule;

    //Domain
    [Header("[ViewData]")]
    public ViewDataSO TPSViewData;
    public ViewDataSO AimViewData;
    public ViewDataSO QuarterViewData;

    private void Awake()
    {
        ViewTransitionModule = GetComponent<ViewTransitionModule>();
        State = GetComponent<ViewStateMachine>();

        State.Bind(this);
    }

    public void TransitionView(EViewType p_viewType)
    {
        State.SetView(p_viewType);
    }
}
