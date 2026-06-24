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
    public MouseUtility MouseUtility;

    //Domain
    [Header("[ViewData]")]
    public ViewDataSO TPSViewData;
    public ViewDataSO AimViewData;
    public ViewDataSO QuarterViewData;

    private void Awake()
    {
        ViewTransitionModule = GetComponent<ViewTransitionModule>();
        State = GetComponent<ViewStateMachine>();
        MouseUtility = GetComponent<MouseUtility>();

        State.Bind(this);
    }

    private void Start()
    {
        Cursour(false);
    }

    public void TransitionView(EViewType p_viewType)
    {
        State.SetView(p_viewType);
    }

    public void Cursour(bool p_isActivate)
    {
        MouseUtility.ActivateCursor(p_isActivate);
    }
}
