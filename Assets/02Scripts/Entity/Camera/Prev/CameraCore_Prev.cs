using UnityEngine;

public class CameraCore_Prev : MonoBehaviour
{
    public UIManager UIModule;
    public AlphaInputSystem Input;
    public Camera RenderCamera { get; private set; }

    [Header("Ref")]
    // Flow
    public ViewStateMachine State;
    //Module
    public ViewTransitionModule ViewTransitionModule;
    public MouseUtility MouseUtility;

    //Domain
    [Header("[ViewData]")]
    public CameraViewDataSO TPSViewData;
    public CameraViewDataSO AimViewData;
    public CameraViewDataSO QuarterViewData;

    private void Awake()
    {
        RenderCamera = GetComponentInChildren<Camera>(true);

        ViewTransitionModule = GetComponent<ViewTransitionModule>();
        State = GetComponent<ViewStateMachine>();
        MouseUtility = GetComponent<MouseUtility>();

        State.Bind(this);
    }

    private void Start()
    {
        Cursour(false);
    }

    public void TransitionView(ECameraViewType p_viewType)
    {
        State.SetView(p_viewType);
    }

    public void Cursour(bool p_isActivate)
    {
        MouseUtility.ActivateCursor(p_isActivate);
    }
}
