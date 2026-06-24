using UnityEngine;

public class QuarterView : ViewStateBase
{
    public override void Enter()
    {
        
    }

    public override void LateUpdate()
    {
        _Core.ViewTransitionModule.RunTimeQuarterView();
    }
    public override void Exit()
    {

    }


    public override Vector3 GetLookDirection()
    {
        throw new System.NotImplementedException();
    }

}
