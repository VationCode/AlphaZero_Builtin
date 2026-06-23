using UnityEngine;

public class LandState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsLand = true;
    }
    public override void Update()
    {
    }

    public override void Exit()
    {
        _Core.Context.IsLand = false;
    }
}
