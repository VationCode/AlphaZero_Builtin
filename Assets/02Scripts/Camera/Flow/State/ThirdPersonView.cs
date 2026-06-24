using UnityEngine;

public class ThirdPersonView : ViewStateBase
{
    private ViewDataSO _viewData;
    public override void Enter()
    {
        _viewData = _Core.TPSViewData;

        _Core.ViewTransitionModule.SetCurrentZoomDistance(_viewData.ZoomMaxDistance);
    }

    public override void LateUpdate()
    {
        var lookDir = _Core.Input.LookInput;
        var scrollInput = _Core.Input.MouseScroll.y;

        _Core.ViewTransitionModule.RunTimeTPSView(lookDir, scrollInput, _viewData);
    }
    public override void Exit()
    {

    }


    public override Vector3 GetLookDirection()
    {
        throw new System.NotImplementedException();
    }
}