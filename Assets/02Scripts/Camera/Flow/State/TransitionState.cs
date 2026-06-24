using UnityEngine;

public class TransitionState : ViewStateBase
{
    private ViewDataSO _targetData;
    public override void Enter()
    {
        _targetData = _Core.State.NextData;
    }

    public override void LateUpdate()
    {
        bool isTransitionCompleted = _Core.ViewTransitionModule.ViewTransition(_targetData);

        if (isTransitionCompleted) _Core.State.EndTransition();
    }

    public override void Exit()
    {
        
    }

    public override Vector3 GetLookDirection()
    {
        throw new System.NotImplementedException();
    }


}
