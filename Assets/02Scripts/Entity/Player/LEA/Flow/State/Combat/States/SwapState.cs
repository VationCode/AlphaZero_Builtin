using UnityEngine;

public class SwapState : BaseState
{
    private const float SwapDuration = 0.25f;

    private float _remainingTime;

    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsInCombat = true;

        _remainingTime = SwapDuration;
    }

    public override void Update()
    {
        _remainingTime -= Time.deltaTime;

        if (_remainingTime > 0f)
            return;

        _Core.StateMachineFlow.ChangeCombatState(ECombatType.Combat);
    }
    public override void Exit()
    {
        
    }
}
