using UnityEngine;

public class NormalState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsInCombat = false;
    }

    public override void Update()
    {
        bool isAim = _Core.InputBoundary.IsAim;
        bool isAttack = _Core.InputBoundary.IsAttack;

        if (isAim || isAttack)
        {
            _Core.StateMachine.ChangeCombatState(ECombatType.Combat);
        }
    }

    public override void Exit()
    {
        
    }
}
