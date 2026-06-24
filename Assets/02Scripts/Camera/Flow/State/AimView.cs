using UnityEngine;

public class AimView : ViewStateBase
{
    public override void Enter()
    {
        
    }

    public override void LateUpdate()
    {
        var lookDir = _Core.Input.LookInput;
        _Core.ViewTransitionModule.RunTimeAimView(lookDir);
    }
    public override void Exit()
    {

    }


    public override Vector3 GetLookDirection()
    {
        throw new System.NotImplementedException();
    }
}
