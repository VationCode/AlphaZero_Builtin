using UnityEngine;

public class QuarterView : ViewStateBase
{
    public override void Enter()
    {
        _Core.Cursour(true);
    }

    public override void LateUpdate()
    {
        bool isAttack = _Core.Input.IsAttack;

        _Core.ViewTransitionModule.RunTimeQuarterView();
    }
    public override void Exit()
    {
        _Core.Cursour(false);
    }


    public override Vector3 GetLookDirection()
    {
        throw new System.NotImplementedException();
    }

}
