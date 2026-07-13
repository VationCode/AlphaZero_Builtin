using UnityEngine;

public class TransitionViewState : ViewStateBase
{
    private ViewDataSO _targetData;
    private EViewType _prevType;
    public override void Enter()
    {
        _targetData = _Core.State.NextData;
        _prevType = _Core.State.PrevType;
    }

    public override void LateUpdate()
    {
        bool isTransitionCompleted = _Core.ViewTransitionModule.ViewTransition(_prevType, _targetData);

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
